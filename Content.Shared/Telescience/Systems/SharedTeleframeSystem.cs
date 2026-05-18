using Content.Shared.Administration.Logs;
using Content.Shared.ChargeRecharge.Components;
using Content.Shared.ChargeRecharge.Events;
using Content.Shared.ChargeRecharge.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.Emag.Systems;
using Content.Shared.Physics;
using Content.Shared.Random.Helpers;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;
using Content.Shared.Telescience.Ui;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using System.Numerics;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    [Dependency] protected readonly SharedTransformSystem Xform = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChargeRechargeSystem _chargeRecharge = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvs = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    private const LookupFlags RangeFlags = LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Sundries;
    public override void Initialize()
    {
        base.Initialize();

        InitializeIncidents(); //TeleframeIncidentLiable stuff
        InitializeRelay(); //Talk between console and teleframe
        InitializeRadio(); //The console saying things over radio, mostly server.
        InitializeConsole(); //Teleframe Console specific stuff dealing with new links and PVS
        InitializeTeleportal(); //Teleport Entity (Teleportal) effects and additional ways teleport charging can fail

        SubscribeLocalEvent<TeleframeComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeActivateMessage>(OnTeleportActivate);
        SubscribeLocalEvent<TeleframeComponent, TeleframeInitiateEvent>(OnInitiate);
        SubscribeLocalEvent<TeleframeComponent, TeleframeTeleportBeginEvent>(OnTeleport);
        SubscribeLocalEvent<TeleframeComponent, StartChargingEvent>(OnStartTeleportCharge);
        SubscribeLocalEvent<TeleframeComponent, EndChargingEvent>(OnEndTeleportCharge);
        SubscribeLocalEvent<TeleframeComponent, EndRechargingEvent>(OnEndTeleportRecharge);
        SubscribeLocalEvent<TeleframeComponent, EntityTerminatingEvent>(OnDeletion);
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
        //if (!_timing.IsFirstTimePredicted)
        //    return;

        Log.Debug($"activate");
        //confirmation of client, ideally these should never return false as the client-side UI should block teleportation if these aren't satisfied.
        if (ent.Comp.LinkedTeleframe is not { } teleEnt || !TryComp<TeleframeComponent>(teleEnt, out var teleComp))
            return; //if null, nonexistent, or lacking teleframe component, return

        if (!teleComp.ReadyToTeleport)
            return; //if the teleframe isn't ready, return

        if (ent.Comp.MaxRange != null && args.Coords.Position.Length() > ent.Comp.MaxRange + Xform.GetMapCoordinates(ent).Position.Length())
            return; //if the teleframe's target is outside the maximum range, return

        var ev = new TeleframeInitiateEvent(args);
        RaiseLocalEvent(teleEnt, ref ev);
    }


    /// <summary>
    /// The spawning of the teleportals
    /// Predictively spawn teleportals, if a target entity is known, spawn next to that
    /// Make sure it's on a grid, if not cancel
    /// </summary>
    /// <param name="ent">Teleframe Entity</param>
    /// <param name="args">Initiate event containing Activation message, containing coordinates, teleportation mode (send/receive), and optionally a targetable entity</param>
    /// <returns>true if succeeeding, false if failing</returns>
    private void OnInitiate(Entity<TeleframeComponent> ent, ref TeleframeInitiateEvent args)
    {
        Log.Debug($"initiate");

        if (ent.Comp.ActiveTeleportInfo != null || ent.Comp.ReadyToTeleport != true || HasComp<ChargingComponent>(ent) || HasComp<RechargingComponent>(ent)) //nuh uh
            return;

        var sourceEffect = ent.Comp.TeleportModeEffects.GetValueOrDefault(args.Msg.Mode); //Get the effect associated with the teleportation mode at the source of teleportation (EG: Send -> From)
        var targetEffect = ent.Comp.TeleportModeEffects.GetValueOrDefault(args.Msg.Mode.GetOpposite()); //Get the other effect for the target

        var sourcePortal = EntityUid.Invalid;
        var targetPortal = EntityUid.Invalid;

        sourcePortal = PredictedSpawnNextToOrDrop(sourceEffect, ent.Owner); //put source teleportal at teleportation source (The Teleframe)
        if (GetEntity(args.Msg.TargetEnt) != EntityUid.Invalid) //if there's a known entity associated with the target, use that instead of just coordinates
            targetPortal = PredictedSpawnNextToOrDrop(targetEffect, GetEntity(args.Msg.TargetEnt)); //put target portal on target Coords.
        else
            targetPortal = EntityManager.PredictedSpawn(targetEffect, args.Msg.Coords); //put target teleportal on target Coords.

        if (ent.Comp.TeleportBeginEffect != null) //create start effects at teleportals
        {
            foreach (var effect in ent.Comp.TeleportBeginEffect)
            {
                PredictedSpawnNextToOrDrop(effect, sourcePortal); //flash start effect
                PredictedSpawnNextToOrDrop(effect, targetPortal);
            }
        }

        ent.Comp.ActiveTeleportInfo = args.Msg.Mode switch //store teleportal info into the teleframe for safe keeping
        {
            TeleframeActivationMode.Send => new TeleframeActiveTeleportInfo(args.Msg.Mode, GetNetEntity(targetPortal), GetNetEntity(sourcePortal), args.Msg.User),
            TeleframeActivationMode.Receive => new TeleframeActiveTeleportInfo(args.Msg.Mode, GetNetEntity(sourcePortal), GetNetEntity(targetPortal), args.Msg.User),
            _ => throw new NotImplementedException()
        };
        Dirty(ent);

        var sourceComp = EnsureComp<TeleframeTeleportalComponent>(sourcePortal); //make sure teleportal component is here to track interactions made with them
        sourceComp.Teleframe = ent.Owner;
        var targetComp = EnsureComp<TeleframeTeleportalComponent>(targetPortal);
        targetComp.Teleframe = ent.Owner;

        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(GetEntity(args.Msg.User))} initiated {ToPrettyString(ent.Owner)} from {ToPrettyString(targetPortal)} ({Xform.ToMapCoordinates(Transform(sourcePortal).Coordinates)}) targeting {ToPrettyString(targetPortal)} ({Xform.ToMapCoordinates(Transform(targetPortal).Coordinates)})");

        _chargeRecharge.StartCharge(ent.Owner, null, args.Msg.User); //begin charging!


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
    private void OnTeleport(Entity<TeleframeComponent> ent, ref TeleframeTeleportBeginEvent args)
    {
        Log.Debug($"teleport");
        var tpFrom = GetEntity(args.TeleportInfo.From);
        var tpTo = GetEntity(args.TeleportInfo.To);

        var entities = _lookup.GetEntitiesInRange(tpFrom, ent.Comp.TeleportRadius, RangeFlags); //get everything in teleport radius range that isn't in a container
        //getting from inside a container would result in teleporting organs outside of the body, or machine parts outside of machines, this is not good.

        List<EntityUid> teleported = new(entities.Count);
        foreach (var tp in entities) //for each entity in list of detected entities
        {
            var tpEnt = Transform(tp); //get transform
            var tpToEnt = Transform(tpTo);

            var rand = new RobustRandom(); //generate a new RobustRandom object with its own seed the Client and Server can agree on
            rand.SetSeed(SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(tp).Id));

            if (tpEnt.Anchored) //if it's anchored, skip it. We don't want to be teleporting the Teleframe itself. Or the station's walls.
                continue;

            if (_whitelistSystem.IsWhitelistPass(ent.Comp.Blacklist, tp)) //if it's on the blacklist, skip it. Don't teleport things like the singularity.
                continue;

            var scatterpos = new EntityCoordinates(tpTo, //make coords at target, scattered by X and Y values +/- scatter range.
                2 * rand.NextFloat() * ent.Comp.TeleportScatterRange - ent.Comp.TeleportScatterRange,
                2 * rand.NextFloat() * ent.Comp.TeleportScatterRange - ent.Comp.TeleportScatterRange);

            Xform.SetCoordinates(tp, scatterpos); //set final position after scatter
            Xform.AttachToGridOrMap(tp, tpEnt);

            var tpEv = new TeleframeUserTeleportedEvent(ent.Owner, args.TeleportInfo); //raise teleport event on teleported entity so it knows it was just teleported
            RaiseLocalEvent(tp, ref tpEv);

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

        var frameFinishEv = new TeleframeTeleportedAllEvent(teleported, args.TeleportInfo); //all done event
        RaiseLocalEvent(ent.Owner, ref frameFinishEv);

        //clean up
        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent.Owner)} has teleported {teleported.Count} entities to {ToPrettyString(tpTo)} ({Xform.ToMapCoordinates(Transform(tpTo).Coordinates)}) to {ToPrettyString(tpFrom)} ({Xform.ToMapCoordinates(Transform(tpFrom).Coordinates)}).");
        TeleportCleanup(ent);
        Dirty(ent);
    }

    #endregion
    #region Teleport Fail Cleanup/Checking

    ///<summary>
    /// Teleportation has concluded, clean up teleportation entities
    /// </summary>
    protected void TeleportCleanup(Entity<TeleframeComponent> ent)
    {
        Log.Debug($"cleanup");
        if (ent.Comp.ActiveTeleportInfo is { } teleInfo)
        {
            var teleFrom = GetEntity(teleInfo.From);
            var teleTo = GetEntity(teleInfo.To);
            if (TryComp<TeleframeTeleportalComponent>(teleFrom, out var teleFromComp))
            {
                teleFromComp.Complete = true;
                Dirty(teleFrom, teleFromComp);
            }
            if (TryComp<TeleframeTeleportalComponent>(teleTo, out var teleToComp))
            {
                teleToComp.Complete = true;
                Dirty(teleTo, teleToComp);
            }

            PredictedQueueDel(teleFrom); //deliberately unpredicted so that the teleport entity dissapears when everyone other than the client is moved rather than after the client is
            PredictedQueueDel(teleTo);
        }

        ent.Comp.ActiveTeleportInfo = null; //clean up our teleport info
    }

    /// <summary>
    /// Indicate teleportation has failed, raise an event, then clean up the teleportals
    /// </summary>
    protected void TeleportFail(Entity<TeleframeComponent> ent, string? failReason = null)
    {
        Log.Debug($"fail");
        if (ent.Comp.TeleportFailEffect != null && !_net.IsClient)
        {
            foreach (var effect in ent.Comp.TeleportFailEffect)
                PredictedSpawnNextToOrDrop(effect, ent.Owner); //fail effects
        }

        var reasonWrapped = Loc.GetString("teleport-fail", ("reason", Loc.GetString(failReason ?? "teleport-fail-unknown")));

        var ev = new TeleframeTeleportFailedEvent(reasonWrapped, ent.Comp.ActiveTeleportInfo);
        RaiseLocalEvent(ent.Owner, ref ev);

        TeleportCleanup(ent);
    }

    /// <summary>
    /// If the teleframe is deleted, make sure charging/recharging shuts down
    /// </summary>
    public void OnDeletion(Entity<TeleframeComponent> ent, ref EntityTerminatingEvent args)
    {
        if (HasComp<ChargeRechargeComponent>(ent))
            _chargeRecharge.DisableCharge(ent.Owner, "teleport-fail-boom");
    }
    #endregion

    #region Charge/Recharge
    // Functions handling whether teleportation is successful are unreliable to predict, as teleportation can go anywhere.
    // While the base functions that call these checks could be in shared and call an overridden abstract function, it results in teleport failiure mispredictions that feel weird.
    /// <summary>
    /// When Teleport Charge starts, check teleframe and teleportals have initialised correctly
    /// </summary>
    public void OnStartTeleportCharge(Entity<TeleframeComponent> ent, ref StartChargingEvent args)
    {
        Log.Debug($"charge start");
        var (teleportSuccess, failReason) = CheckTeleportation(ent);
        if (teleportSuccess == false) //start of charge wellness check on the teleframe, if not good, just end the charge immediately
        {
            _chargeRecharge.EndCharge(ent.Owner, false, failReason);
        }
        else
        {
            ent.Comp.ReadyToTeleport = false;
            Dirty(ent);

            var ev = new TeleframeInitiatedEvent(ent.Owner, ent.Comp.ActiveTeleportInfo!.Value);
            RaiseLocalEvent(ent, ref ev);
        }
    }

    /// <summary>
    /// When Teleport Charge completes, check whether Teleportation is allowed
    /// </summary>
    public void OnEndTeleportCharge(Entity<TeleframeComponent> ent, ref EndChargingEvent args)
    {
        Log.Debug($"charge end");
        if (args.Success == false) //if anything caused a fail during charging, cleanup
        {
            TeleportFail(ent, args.FailReason);
        }
        else
        {
            var (teleportSuccess, failReason) = CheckTeleportation(ent);
            if (teleportSuccess == false) //end of charge wellness check on the teleframe
            {
                TeleportFail(ent, failReason);
            }
            else
            {
                var ev = new TeleframeTeleportBeginEvent(ent.Owner, ent.Comp.ActiveTeleportInfo!.Value);
                RaiseLocalEvent(ent, ref ev);
            }
        }
    }

    /// <summary>
    /// Recharge is done, indicate this to player at console
    /// </summary>
    public void OnEndTeleportRecharge(Entity<TeleframeComponent> ent, ref EndRechargingEvent args)
    {
        Log.Debug($"recharge end");
        ent.Comp.ReadyToTeleport = true;

        var ev = new TeleframeReadyEvent(ent.Owner, args.User);
        RaiseLocalEvent(ent, ref ev);

        Dirty(ent);
    }
    #endregion


    #region Other Helpers
    /// <summary>
    /// Gets the Teleportal at the teleframe's target
    /// </summary>
    public EntityUid GetTeleportalTarget(TeleframeActiveTeleportInfo teleInfo)
    {
        if (!Exists(GetEntity(teleInfo.From)) || !Exists(GetEntity(teleInfo.To))) //is active teleport info null, is the teleport info empty, do either teleport entity not exist
            return EntityUid.Invalid;

        switch (teleInfo.Mode)
        {
            case TeleframeActivationMode.Send:
                return GetEntity(teleInfo.To);
            case TeleframeActivationMode.Receive:
                return GetEntity(teleInfo.From);
            default:
                return EntityUid.Invalid;
        }
    }

    /// <summary>
    /// Gets the Teleportal at the teleframe's source (usually directly above itself unless the teleframe is an item).
    /// </summary>
    public EntityUid GetTeleportalSource(TeleframeActiveTeleportInfo teleInfo)
    {
        if (!Exists(GetEntity(teleInfo.From)) || !Exists(GetEntity(teleInfo.To))) //is active teleport info null, is the teleport info empty, do either teleport entity not exist
            return EntityUid.Invalid;

        switch (teleInfo.Mode)
        {
            case TeleframeActivationMode.Send:
                return GetEntity(teleInfo.To);
            case TeleframeActivationMode.Receive:
                return GetEntity(teleInfo.From);
            default:
                return EntityUid.Invalid;
        }
    }

    // these checks require information the client potentially doesn't know.
    /// <summary>
    /// Prevent teleportation if receive teleportal is not on a grid or inside a wall, send teleportal is allowed to be off grid so you can teleport from empty space but not to.
    /// </summary>
    /// <param name="teleportal">teleportal entity</param>
    /// <returns></returns>
    public (bool, string?) CheckTeleportal(EntityUid teleportal, bool allowCollision = false, bool allowGridless = false)
    {
        if (!Exists(teleportal) || Transform(teleportal).MapID == MapId.Nullspace) //does this entity exist and is not in nullspace
            return (false, "teleport-fail-nolink");

        if (!HasComp<TeleframeTeleportalComponent>(teleportal)) //does the teleportal have its tracking component
            return (false, "teleport-fail-nolink");

        if (Xform.GetGrid(teleportal) == null && allowGridless == false) //prevent portals off grids unless permitted
            return (false, "teleport-fail-nogrid");

        if (_physics.GetEntitiesIntersectingBody(teleportal, (int)CollisionGroup.Impassable).Count > 0 && allowCollision == false) //prevent collision with impassible objects unless permitted
            return (false, "teleport-fail-collision");

        return (true, null);
    }

    /// <summary>
    /// Check teleframe has done its book-keeping and that it knows where it wants to go
    /// Then check teleportals to see if they're valid
    /// </summary>
    /// <param name="ent">The teleframe</param>
    /// <returns>validity , fail reason if there is one</returns>
    public (bool, string?) CheckTeleportation(Entity<TeleframeComponent> ent)
    {
        if (_net.IsClient) //can't trust the client to know this, entities could be anywhere. Client can simply assume truth and be told otherwise by the server.
            return (true, null);

        if (ent.Comp.ActiveTeleportInfo == null || ent.Comp.ActiveTeleportInfo is not { } teleInfo || !Exists(GetEntity(teleInfo.From)) || !Exists(GetEntity(teleInfo.To))) //is active teleport info null, is the teleport info empty, do either teleport entity not exist
            return (false, "teleport-fail-nolink");
        //check From teleportal
        var (teleportSuccess, failReason) = CheckTeleportal(GetEntity(teleInfo.From), ent.Comp.AllowCollision, ent.Comp.AllowGridless ?? true);
        if (teleportSuccess == false)
            return (teleportSuccess, failReason);
        //check To teleportal
        (teleportSuccess, failReason) = CheckTeleportal(GetEntity(teleInfo.To), ent.Comp.AllowCollision, ent.Comp.AllowGridless ?? false);
        if (teleportSuccess == false)
            return (teleportSuccess, failReason);

        return (true, null);
    }
    #endregion
}

