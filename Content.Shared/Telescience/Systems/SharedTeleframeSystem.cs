using Content.Shared.Administration.Logs;
using Content.Shared.ChargeRecharge.Components;
using Content.Shared.ChargeRecharge.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.Emag.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Ui;
using Content.Shared.Telescience.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;
using Content.Shared.ChargeRecharge.Events;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    [Dependency] protected readonly SharedTransformSystem Xform = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvs = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private readonly SharedChargeRechargeSystem _chargeRecharge = default!;
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
        SubscribeLocalEvent<TeleframeConsoleComponent, TeleframeActivateMessage>(OnTeleportActivate);
        SubscribeLocalEvent<TeleframeComponent, EndChargingEvent>(OnEndTeleportCharge);
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
        //confirmation of client, ideally these should never return false as the client-side UI should block teleportation if these aren't satisfied.
        if (ent.Comp.LinkedTeleframe is not { } teleEnt || !TryComp<TeleframeComponent>(teleEnt, out var teleComp))
            return; //if null, nonexistent, or lacking teleframe component, return

        if (!teleComp.ReadyToTeleport)
            return; //if the teleframe isn't ready, return

        if (ent.Comp.MaxRange != null && args.Coords.Position.Length() > ent.Comp.MaxRange + Xform.GetMapCoordinates(ent).Position.Length())
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
        if (ent.Comp.ActiveTeleportInfo != null || ent.Comp.ReadyToTeleport != true || HasComp<ChargingComponent>(ent) || HasComp<RechargingComponent>(ent)) //nuh uh
            return false;

        var sourceEffect = ent.Comp.TeleportModeEffects.GetValueOrDefault(args.Mode); //Get the effect associated with the teleportation mode at the source of teleportation (EG: Send -> From)
        var targetEffect = ent.Comp.TeleportModeEffects.GetValueOrDefault(args.Mode.GetOpposite()); //Get the other effect for the target

        var sourcePortal = PredictedSpawnNextToOrDrop(sourceEffect, ent.Owner); //put source teleportal at teleportation source (The Teleframe)
        var targetPortal = EntityUid.Invalid;
        if (GetEntity(args.TargetEnt) != EntityUid.Invalid) //if there's a known entity associated with the target, use that instead of just coordinates
            targetPortal = PredictedSpawnNextToOrDrop(targetEffect, GetEntity(args.TargetEnt)); //put target portal on target Coords.
        else
            targetPortal = EntityManager.PredictedSpawn(targetEffect, args.Coords); //put target teleportal on target Coords.

        Transform(sourcePortal).Coordinates.SnapToGrid(EntityManager); //ensure grid alignment so not teleportals half stuck in a wall
        Transform(targetPortal).Coordinates.SnapToGrid(EntityManager); //This may mean coordinate setting is slightly off, needs testing

        var sourceComp = EnsureComp<TeleframeTeleportalComponent>(sourcePortal); //make sure teleportal component is here to track interactions made with them
        sourceComp.Teleframe = ent.Owner;
        var targetComp = EnsureComp<TeleframeTeleportalComponent>(targetPortal);
        targetComp.Teleframe = ent.Owner;

        ent.Comp.ActiveTeleportInfo = args.Mode switch //store teleportal info into the teleframe for safe keeping
        {
            TeleframeActivationMode.Send => new TeleframeActiveTeleportInfo(args.Mode, GetNetEntity(targetPortal), GetNetEntity(sourcePortal)),
            TeleframeActivationMode.Receive => new TeleframeActiveTeleportInfo(args.Mode, GetNetEntity(sourcePortal), GetNetEntity(targetPortal)),
            _ => throw new NotImplementedException()
        };

        if (ent.Comp.TeleportBeginEffect != null) //create start effects at teleportals
        {
            foreach (var effect in ent.Comp.TeleportBeginEffect)
            {
                PredictedSpawnNextToOrDrop(effect, sourcePortal); //flash start effect
                PredictedSpawnNextToOrDrop(effect, targetPortal);
            }
        }


        _chargeRecharge.StartCharge(ent.Owner); //begin charging!

        Log.Debug("Initiated 1");
        return true;
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

        Log.Debug("Teleporting Start");

        var tpFrom = GetEntity(teleInfo.From);
        var tpTo = GetEntity(teleInfo.To);

        var entities = _lookup.GetEntitiesInRange(tpFrom, ent.Comp.TeleportRadius, RangeFlags); //get everything in teleport radius range that isn't in a container
        //getting from inside a container would result in teleporting organs outside of the body, or machine parts outside of machines, this is not good.
        var tpToCoords = Xform.ToMapCoordinates(Transform(tpTo).Coordinates); //have to use map coordinates as these entities will be deleted after teleportation concludes
        var tpFromCoords = Xform.ToMapCoordinates(Transform(tpFrom).Coordinates);

        List<EntityUid> teleported = new(entities.Count);
        foreach (var tp in entities) //for each entity in list of detected entities
        {
            var tpEnt = Transform(tp); //get transform

            var rand = new RobustRandom(); //generate a new RobustRandom object with its own seed the Client and Server can agree on
            rand.SetSeed(SharedRandomExtensions.HashCodeCombine((int)Timing.CurTick.Value, GetNetEntity(tp).Id));

            if (tpEnt.Anchored) //if it's anchored, skip it. We don't want to be teleporting the Teleframe itself. Or the station's walls.
                continue;

            if (_whitelistSystem.IsWhitelistPass(ent.Comp.Blacklist, tp)) //if it's on the blacklist, skip it. Don't teleport things like the singularity.
                continue;

            Xform.DropNextTo(tp, tpTo); //bit scuffed but because the map the target will be on won't neccisarily be the same as the Teleframe's we first drop them next to the target THEN scatter.
            var scatterpos = new Vector2( //create scatter coordinates as teleported entities' X and Y values +/- scatter range.
                Xform.ToMapCoordinates(tpEnt.Coordinates).X + rand.NextFloat() * ent.Comp.TeleportScatterRange - ent.Comp.TeleportScatterRange,
                Xform.ToMapCoordinates(tpEnt.Coordinates).Y + rand.NextFloat() * ent.Comp.TeleportScatterRange - ent.Comp.TeleportScatterRange);

            Xform.SetWorldPosition(tp, scatterpos); //set final position after scatter

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

        var frameFinishEv = new TeleframeTeleportedAllEvent(teleported, tpToCoords, tpFromCoords); //all done event
        RaiseLocalEvent(ent.Owner, ref frameFinishEv);

        Log.Debug("Teleporting End");

        //clean up
        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(ent.Owner)} has teleported {teleported.Count} entities from {tpTo} to {tpFrom}.");
        Dirty(ent);
    }

    #endregion
    public abstract (bool, string?) CheckTeleportation(Entity<TeleframeComponent> ent);

    /// <summary>
    /// When Teleport Charge completes, check whether Teleportation is allowed
    /// </summary>
    public void OnEndTeleportCharge(Entity<TeleframeComponent> ent, ref EndChargingEvent args)
    {
        _chargeRecharge.StartRecharge(ent.Owner);

        Log.Debug("Teleporting starting 1");
        if (args.Success == false) //if anything caused a fail, cleanup
        {
            TeleportCleanup(ent, args.FailReason);
        }
        else
        {
            var (teleportSuccess, failReason) = CheckTeleportation(ent);
            if (teleportSuccess == false) //start of charge wellness check on the teleframe
            {
                TeleportCleanup(ent, failReason);
            }
            else
            {
                OnTeleport(ent); //if all good, teleport
                TeleportCleanup(ent, null);
            }
        }
    }
    #region Teleport Fail Cleanup/Checking

    ///<summary>
    /// Teleportation has concluded, clean up teleportation entities
    /// also if we failed raise an event and summon some l̶i̶g̶h̶t̶n̶i̶n̶g̶  smoke, for fun.
    /// </summary>
    protected void TeleportCleanup(Entity<TeleframeComponent> ent, string? failReason = null)
    {
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

            PredictedQueueDel(teleFrom);
            PredictedQueueDel(teleTo);
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

    /// <summary>
    /// If the teleframe is deleted, make sure charging/recharging shuts down
    /// </summary>
    public void OnDeletion(Entity<TeleframeComponent> ent, ref EntityTerminatingEvent args)
    {
        if (HasComp<ChargeRechargeComponent>(ent))
            _chargeRecharge.DisableCharge(ent.Owner, "teleport-fail-boom");
    }
    #endregion

}

