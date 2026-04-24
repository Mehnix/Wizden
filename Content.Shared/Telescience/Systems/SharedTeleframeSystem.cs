using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Physics;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Ui;
using Content.Shared.Telescience.Events;
using Content.Shared.Trigger;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvs = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    private const LookupFlags RangeFlags = LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Sundries;
    public override void Initialize()
    {
        base.Initialize();

        InitializeIncidents(); //TeleframeIncidentLiable stuff
        InitializeRelay(); //Talk between console and teleframe
        InitializeRadio(); //The console saying things over radio, mostly server.
        InitializePower(); //turning the teleframe on and off, and TeleframeStructurePower in Server
        InitializeConsole(); //Teleframe Console specific stuff dealing with new links and PVS
        InitializeTeleportal(); //Teleport Entity (Teleportal) effects and additional ways teleport charging can fail

        SubscribeLocalEvent<TeleframeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TeleframeComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeActivateMessage>(OnTeleportActivate);

        SubscribeLocalEvent<TeleframeChargingComponent, ComponentStartup>(OnChargeStart);
        SubscribeLocalEvent<TeleframeRechargingComponent, ComponentStartup>(OnRechargeStart);
        SubscribeLocalEvent<TeleframeRechargingComponent, ComponentRemove>(OnRechargeEnd);

    }

    /// <summary>
    /// If Teleframe and Console were linked during map creation, add that link at the start of the round
    /// </summary>
    private void OnMapInit(Entity<TeleframeComponent> ent, ref MapInitEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        if (!TryComp<DeviceLinkSinkComponent>(ent, out var sink))
            return;

        foreach (var source in sink.LinkedSources)
        {
            if (!TryComp<TeleframeConsoleComponent>(source, out var console))
                continue;

            console.LinkedTeleframe = ent.Owner;
            ent.Comp.LinkedConsole = source;
            Dirty(source, console);
            Dirty(ent);
            break;
        }
    }

    #region Teleportation
    /// <summary>
    /// The initial setup function for teleporting
    /// No need to inform player of fails here as client has the same blockers that do so
    /// </summary>
    private void OnTeleportActivate(Entity<TeleframeConsoleComponent> ent, ref TeleframeActivateMessage args)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        //confirmation of client, ideally these should never return false as the client-side UI should block teleportation if these aren't satisfied.
        if (ent.Comp.LinkedTeleframe is not { } teleEnt || !TryComp<TeleframeComponent>(teleEnt, out var teleComp))
            return; //if null, nonexistent, or lacking teleframe component, return

        if (!teleComp.IsPowered || !teleComp.ReadyToTeleport)
            return; //if the teleframe isn't powered or ready, return

        if (ent.Comp.MaxRange != null && args.Coords.Position.Length() > ent.Comp.MaxRange + _transform.GetMapCoordinates(ent).Position.Length())
            return; //if the teleframe's target is outside the maximum range, return

        if (!StartTeleport((teleEnt, teleComp), args))
            return;

        Dirty(teleEnt, teleComp);
    }

    /// <summary>
    /// The spawning of the teleportals
    /// Predictively spawn teleportals, if a target entity is known, spawn next to that
    /// Make sure it's on a grid, if not cancel
    /// </summary>
    /// <param name="ent">Teleframe Entity</param>
    /// <param name="args">Activation message containing coordinates, teleportation mode (send/receive), and optionally a targetable entity</param>
    /// <returns>true if succeeeding, false if failing</returns>
    private bool StartTeleport(Entity<TeleframeComponent> ent, TeleframeActivateMessage args)
    {
        if (ent.Comp.ActiveTeleportInfo != null || ent.Comp.ReadyToTeleport != true || HasComp<TeleframeChargingComponent>(ent) || HasComp<TeleframeRechargingComponent>(ent)) //nuh uh, we recharging
            return false;

        ent.Comp.ReadyToTeleport = false;
        var chargeComp = AddComp<TeleframeChargingComponent>(ent);

        var ev = new TeleframeStartChargeEvent(ent, args.Coords);
        RaiseLocalEvent(ent, ref ev);

        var tp = Transform(ent); //get transform of the Teleframe

        var sourceEffect = ent.Comp.TeleportModeEffects.GetValueOrDefault(args.Mode);
        var targetEffect = ent.Comp.TeleportModeEffects.GetValueOrDefault(args.Mode.GetOpposite());

        var sourcePortal = EntityManager.PredictedSpawnAtPosition(sourceEffect, tp.Coordinates); //put source portal on Teleframe

        var targetPortal = EntityUid.Invalid;
        if (GetEntity(args.TargetEnt) != EntityUid.Invalid) //if there's a known entity associated with the target, use that instead of just coordinates
            targetPortal = EntityManager.PredictedSpawnNextToOrDrop(targetEffect, GetEntity(args.TargetEnt)); //put target portal on target Coords.
        else
            targetPortal = EntityManager.PredictedSpawn(targetEffect, args.Coords); //put target portal on target Coords.

        if (ent.Comp.TeleportBeginEffect != null) //create effects at source teleportal
        {
            foreach (var effect in ent.Comp.TeleportBeginEffect)
            {
                PredictedSpawnNextToOrDrop(effect, sourcePortal); //flash start effect
                PredictedSpawnNextToOrDrop(effect, targetPortal); //flash start effect
            }
        }

        ent.Comp.ActiveTeleportInfo = args.Mode switch
        {
            TeleframeActivationMode.Send => new TeleframeActiveTeleportInfo(args.Mode, GetNetEntity(targetPortal), GetNetEntity(sourcePortal)),
            TeleframeActivationMode.Receive => new TeleframeActiveTeleportInfo(args.Mode, GetNetEntity(sourcePortal), GetNetEntity(targetPortal)),
            _ => throw new NotImplementedException()
        };

        switch (args.Mode)
        {
            case TeleframeActivationMode.Send: //prevent sending into empty space or a wall
                (chargeComp.TeleportSuccess, chargeComp.FailReason) = CheckTeleportal(targetPortal);
                break;
            case TeleframeActivationMode.Receive: //prevent receiving into empty space or a wall
                (chargeComp.TeleportSuccess, chargeComp.FailReason) = CheckTeleportal(sourcePortal);
                break;
            default:
                throw new NotImplementedException();
        }

        var sourceComp = EnsureComp<TeleframeTeleportalComponent>(sourcePortal); //make sure teleportal component is here to track interactions made with them
        sourceComp.Teleframe = ent.Owner;
        var targetComp = EnsureComp<TeleframeTeleportalComponent>(targetPortal);
        targetComp.Teleframe = ent.Owner;


        Dirty(ent, chargeComp);
        return true;
    }
    /// <summary>
    /// Prevent teleportation if receive teleportal is not on a grid or inside a wall, send teleportal is allowed to be off grid so you can teleport from empty space but not to.
    /// </summary>
    /// <param name="teleportal">teleportal entity</param>
    /// <returns></returns>
    private (bool, string) CheckTeleportal(EntityUid teleportal)
    {
        if (_transform.GetGrid(teleportal) == null)
            return (false, "teleport-fail-nogrid");

        if (_physics.GetEntitiesIntersectingBody(teleportal, (int)CollisionGroup.Impassable).Count > 0)
            return (false, "teleport-fail-collision");

        return (true, "teleport-fail-unknown");
    }

    /// <summary>
    /// Function that handles actual teleportation:
    /// Get all entities in range, for each entity
    /// If it doesn't have physics, skip
    /// If it's anchored, skip
    /// If it's on blacklist, skip
    /// otherwise, teleport to target location, then scatter slightly
    /// also adminlog
    /// </summary>
    /// <param name="ent">TeleframeComponent Entity</param>
    private void OnTeleport(Entity<TeleframeComponent> ent)
    {
        if (ent.Comp.ActiveTeleportInfo is not { } teleInfo)
            return;

        var tpFrom = GetEntity(teleInfo.From);
        var tpTo = GetEntity(teleInfo.To);

        var entities = _lookup.GetEntitiesInRange(tpFrom, ent.Comp.TeleportRadius, RangeFlags); //get everything in teleport radius range that isn't in a container
        //getting from inside a container would result in teleporting organs outside of the body, or machine parts outside of machines, this is not good.
        var tpToCoords = _transform.ToMapCoordinates(Transform(tpTo).Coordinates); //have to use map coordinates as these entities will be deleted after teleportation concludes
        var tpFromCoords = _transform.ToMapCoordinates(Transform(tpFrom).Coordinates);

        List<EntityUid> teleported = new(entities.Count);
        foreach (var tp in entities) //for each entity in list of detected entities
        {
            var tpEnt = Transform(tp); //get transform

            if (tpEnt.Anchored) //if it's anchored, skip it. We don't want to be teleporting the Teleframe itself. Or the station's walls.
                continue;

            if (_whitelistSystem.IsWhitelistPass(ent.Comp.Blacklist, tp)) //if it's on the blacklist, skip it. Don't teleport things like the singularity.
                continue;

            _transform.DropNextTo(tp, tpTo); //bit scuffed but because the map the target will be on won't neccisarily be the same as the Teleframe's we first drop them next to the target THEN scatter.
            var scatterpos = new Vector2( //create scatter coordinates as teleported entities' X and Y values +/- scatter range.
                _transform.ToMapCoordinates(tpEnt.Coordinates).X + Random.NextFloat(-ent.Comp.TeleportScatterRange, ent.Comp.TeleportScatterRange),
                _transform.ToMapCoordinates(tpEnt.Coordinates).Y + Random.NextFloat(-ent.Comp.TeleportScatterRange, ent.Comp.TeleportScatterRange));

            _transform.SetWorldPosition(tp, scatterpos); //set final position after scatter

            var tpEv = new TeleframeUserTeleportedEvent(ent.Owner, tpToCoords, tpFromCoords); //raise teleport event on teleported entity so it knows it was just teleported
            RaiseLocalEvent(tp, ref tpEv);

            var frameEv = new TeleframeTeleportedEvent(tp, tpToCoords, tpFromCoords); //raise teleport event on teleframe so it knows what it just teleported
            RaiseLocalEvent(ent.Owner, ref frameEv);

            teleported.Add(tp);
        }

        if (ent.Comp.TeleportFinishEffect != null)
        {
            foreach (var effect in ent.Comp.TeleportFinishEffect)
            {
                PredictedSpawnNextToOrDrop(effect, tpTo); //finish effects
                PredictedSpawnNextToOrDrop(effect, tpFrom);
            }
        }

        var trig = new TriggerEvent(tpTo); //send a trigger to the teleportals in case they have any last actions
        RaiseLocalEvent(tpTo, ref trig);
        RaiseLocalEvent(tpFrom, ref trig);

        var frameFinishEv = new TeleframeTeleportedAllEvent(teleported, tpToCoords, tpFromCoords); //all done event
        RaiseLocalEvent(ent.Owner, ref frameFinishEv);

        //clean up
        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent.Owner)} has teleported {teleported.Count} entities from {tpTo} to {tpFrom}.");
        TeleportCleanup(ent, null);
        Dirty(ent);
    }

    #endregion
    #region Charge/Recharge
    /// <summary>
    /// update charge appearance
    /// </summary>
    private void OnChargeStart(Entity<TeleframeChargingComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<TeleframeComponent>(ent, out var teleComp)) //when charging starts, update appearance to charge animation
            return;

        ent.Comp.Duration = teleComp.ChargeDuration;
        ent.Comp.EndTime = teleComp.ChargeDuration + Timing.CurTime;

        var chargingEv = new ChargingEvent(); //raise event to indicate charging starting successfully
        RaiseLocalEvent(ent, ref chargingEv);
        UpdateAppearance((ent.Owner, teleComp));
    }

    private void OnRechargeStart(Entity<TeleframeRechargingComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<TeleframeComponent>(ent, out var teleComp)) //when charging starts, update appearance to charge animation
            return;

        ent.Comp.Duration = teleComp.RechargeDuration;
        ent.Comp.EndTime = teleComp.RechargeDuration + Timing.CurTime;

        var rechargingEv = new RechargingEvent(); //raise event to indicate recharging starting successfully
        RaiseLocalEvent(ent, ref rechargingEv);
        UpdateAppearance((ent.Owner, teleComp));
    }

    private void OnRechargeEnd(Entity<TeleframeRechargingComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp<TeleframeComponent>(ent, out var teleComp)) //when recharging ends, update appearance to on animation
            return;

        var rechargeEv = new TeleframeReadyEvent(ent.Owner); //raise event to indicate recharge complete
        RaiseLocalEvent(ent, ref rechargeEv);
        UpdateAppearance((ent.Owner, teleComp));            //recharge component isn't removed if teleframe is depowered
    }

    /// <summary>
    /// When Teleport Charge completes, check whether Teleportation is allowed
    /// </summary>
    public void EndTeleportCharge(Entity<TeleframeComponent, TeleframeChargingComponent> ent)
    {
        var failReason = ent.Comp2.FailReason;

        if (ent.Comp1.ActiveTeleportInfo == null || ent.Comp1.ActiveTeleportInfo is not { } teleInfo || !Exists(GetEntity(teleInfo.From)) || !Exists(GetEntity(teleInfo.To)))
        { //is active teleport info null, is the teleport info empty, do either teleport entity not exist
            ent.Comp2.TeleportSuccess = false; //if either teleport entity doesn't exist obvs you can't teleport
            failReason = Loc.GetString("teleport-fail-nolink");
        }

        RemCompDeferred<TeleframeChargingComponent>(ent); //stop charging

        if (ent.Comp2.TeleportSuccess) //if teleport is still good to go, engage
            OnTeleport(ent); //teleport
        else
            TeleportCleanup(ent, failReason); //if not, say why

        if (!HasComp<TeleframeRechargingComponent>(ent))
        {
            var rechargeComp = AddComp<TeleframeRechargingComponent>(ent); //start recharging
            Dirty(ent, rechargeComp);
        }

        UpdateAppearance(ent);
    }

    /// <summary>
    /// Recharge is done, indicate this to player at console and reset power draw levels
    /// </summary>
    public void EndTeleportRecharge(Entity<TeleframeComponent> ent, TeleframeRechargingComponent recharge)
    {
        ent.Comp.ReadyToTeleport = true;
        if (ent.Comp.LinkedConsole != null)
        {
            if (TryComp<TeleframeConsoleComponent>(ent.Comp.LinkedConsole, out var consoleComp))
            {
                Audio.PlayPvs(consoleComp.TeleportRechargedSound, ent.Comp.LinkedConsole!.Value);
            }
        }
        RemCompDeferred<TeleframeRechargingComponent>(ent);
        UpdateAppearance(ent);
    }

    #endregion
    #region Teleport Fail Cleanup

    ///<summary>
    /// Teleportation has concluded, clean up teleportation entities
    /// also if we failed raise an event and summon some l̶i̶g̶h̶t̶n̶i̶n̶g̶  smoke, for fun.
    /// </summary>
    protected void TeleportCleanup(Entity<TeleframeComponent> ent, string? failReason = null)
    {
        if (ent.Comp.ActiveTeleportInfo is { } teleInfo)
        {
            PredictedQueueDel(GetEntity(teleInfo.From));
            PredictedQueueDel(GetEntity(teleInfo.To));
        }
        ent.Comp.ActiveTeleportInfo = null; //clean up our teleport info

        if (failReason != null) //fail if we have a reason for it
        {
            if (ent.Comp.TeleportFailEffect != null)
            {
                foreach (var effect in ent.Comp.TeleportFailEffect)
                    PredictedSpawnNextToOrDrop(effect, ent.Owner); //fail effects
            }

            var reasonWrapped = Loc.GetString("teleport-fail", ("reason", Loc.GetString(failReason)));

            var ev = new TeleframeTeleportFailedEvent(reasonWrapped);
            RaiseLocalEvent(ent.Owner, ref ev);
        }
    }

    #endregion
    #region Appearance
    /// <summary>
    /// update teleframe appearance between on, off, charging, and recharging
    /// also enables/disables lights
    /// </summary>
    /// <param name="ent"></param>

    protected void UpdateAppearance(Entity<TeleframeComponent> ent)
    {
        TeleframeVisualState state;
        if (ent.Comp.IsPowered == true) //check if powered, set to on state
        {
            state = TeleframeVisualState.On;
            if (HasComp<TeleframeChargingComponent>(ent)) //override if charged
            {
                state = TeleframeVisualState.Charging;
            }

            if (HasComp<TeleframeRechargingComponent>(ent)) //override if recharged, this state takes highest priority
            {
                state = TeleframeVisualState.Recharging;
            }
        }
        else
        {
            state = TeleframeVisualState.Off;
        }

        if (_lights.TryGetLight(ent.Owner, out var light)) //set light whilst here
        {
            _lights.SetEnabled(ent.Owner, ent.Comp.IsPowered);
            Dirty(ent.Owner, light);
        }

        _appearance.SetData(ent.Owner, TeleframeVisuals.VisualState, state); //Dirties itself
        Dirty(ent);
    }

    /// <summary>
    /// tell user power status and charge level
    /// </summary>
    private void OnExamined(Entity<TeleframeComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.IsPowered == true) //manually apply power level descriptions
        {
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main", ("stateText", Loc.GetString("power-receiver-component-on-examine-powered"))));
            if (HasComp<TeleframeChargingComponent>(ent))
            {
                args.PushMarkup(Loc.GetString("teleporter-examine-charging"));
            }

            if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp))
            {
                if (rechargeComp.Pause == false)
                    args.PushMarkup(Loc.GetString("teleporter-examine-recharging"));
                else
                    args.PushMarkup(Loc.GetString("teleporter-examine-recharging-paused"));
            }
        }
        else
        {
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main", ("stateText", Loc.GetString("power-receiver-component-on-examine-unpowered"))));
        }
    }
    #endregion
}
