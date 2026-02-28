using Content.Shared.Coordinates;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Content.Shared.Examine;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTelechargeSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    private const LookupFlags RangeFlags = LookupFlags.Approximate | LookupFlags.Dynamic | LookupFlags.Sundries | LookupFlags.Static;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelechargeComponent, TeleframeUserTeleportedEvent>(OnTeleported);
        SubscribeLocalEvent<TelechargeComponent, TeleframeUserIncidentEvent>(OnIncident);
        SubscribeLocalEvent<TelechargeComponent, TelechargeAddScienceEvent>(OnScience);

        SubscribeLocalEvent<TelechargeComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<TelechargeRechargingComponent, ComponentStartup>(OnRechargeStart);
        SubscribeLocalEvent<TelechargeRechargingComponent, ComponentShutdown>(OnRechargeEnd);
    }

    /// <summary>
    /// Scan for whitelisted things in range.
    /// If none in range do nothing
    /// If some in range but not scan range say how far away the closest is
    /// If some in scan range send an event to each of them and say that that has happened.
    /// </summary>
    private void OnTeleported(Entity<TelechargeComponent> ent, ref TeleframeUserTeleportedEvent args)
    {
        if (GetWhitelistTargets(ent.Owner, ent.Comp.Range, ent.Comp.Blacklist).Count > 0) //if detecting anything on the blacklist in range, stop immediately.
            return;

        var inRange = GetWhitelistTargets(ent.Owner, ent.Comp.Range, ent.Comp.Whitelist);
        if (inRange.Count <= 0) //if no targets in range, say so. Gotta go out and look for signs.
        {
            var message = Loc.GetString("telecharge-range-none", ("range", ent.Comp.Range));
            SendRadioMessage(ent, message);
            return;
        }
        var inScanRange = GetWhitelistTargets(ent.Owner, ent.Comp.ScanRange, ent.Comp.Whitelist);

        var telechargePos = _transform.ToMapCoordinates(Transform(ent).Coordinates);

        var (closeEnt, closeDistance) = GetClosest(inRange!, telechargePos); //the user will only know the teleported target destination coords, but distance is calculated from the true, scattered location. Introducing uncertainty.
        if (inScanRange.Count <= 0) //if targets in range, but none in scan range, say how far away closest is
        {
            //telecharge-range-detect = [num] distortions detected, closest [distance] metres away, adjust coordinates to bring within [range] metre scanning range.
            var message = Loc.GetString("telecharge-range-detect", ("num", inRange.Count), ("distance", closeDistance!.Value), ("range", ent.Comp.ScanRange));
            SendRadioMessage(ent, message);
            return;
        }
        else //if at least one in range, scan it
        {
            var rechargeComp = AddComp<TelechargeRechargingComponent>(ent); //scanned, so recharge
            rechargeComp.Duration = ent.Comp.RechargeDuration;
            rechargeComp.EndTime = ent.Comp.RechargeDuration + Timing.CurTime;
            Dirty(ent.Owner, rechargeComp);

            var incidentMult = 0f; //initialise incident mult. Only stays zero if teleframe isn't capable of incidents (admeme only)
            if (TryComp<TeleframeIncidentLiableComponent>(args.Teleframe, out var teleComp)) //if the teleframe that sent the telecharge is incident liable, update incident multiplier
                incidentMult = teleComp.IncidentMultiplier;

            foreach (var target in inScanRange) //for each scanned target
            {
                var distance = (float)GetMapCoordinatesDistance(telechargePos, _transform.ToMapCoordinates(Transform(target).Coordinates)); //distance between telecharge and target
                //Log.Debug($"{target.ToString()} {ent.Owner} {distance} {incidentMult}");
                var scanEv = new TelechargeScanEvent(ent.Owner, distance, incidentMult);
                RaiseLocalEvent(target, ref scanEv); //send a scan event, they can then send back how much science the telecharge gets.
            }
            //telecharge-range-scan = [num] Distortion reactions detected within [range] metres. Data collected for retrieval.
            var message = Loc.GetString("telecharge-range-scan", ("num", inScanRange.Count), ("range", ent.Comp.ScanRange)); //send a message
            SendRadioMessage(ent, message);
            Dirty(ent);
            return;
        }

    }
    /// <summary>
    /// On a teleport incident send a custom event to whitelisted elements in scanning range so they can do something.
    /// </summary>
    private void OnIncident(Entity<TelechargeComponent> ent, ref TeleframeUserIncidentEvent args)
    {
        var inScanRange = GetWhitelistTargets(ent.Owner, ent.Comp.ScanRange, ent.Comp.Whitelist);
        if (inScanRange != null)
        {
            foreach (var target in inScanRange) //for each scanned target
            {
                var scanEv = new TelechargeScanIncidentEvent(args.Score, args.IncidentMult);
                RaiseLocalEvent(target, ref scanEv); //send an incident event, just a copy of the TeleframeUserIncidentEvent
            }
        }
    }

    /// <summary>
    /// Event sent to the telecharge to give it science
    /// </summary>
    private void OnScience(Entity<TelechargeComponent> ent, ref TelechargeAddScienceEvent args)
    {
        Log.Debug("Science");
        ent.Comp.Science += args.Science;
        Dirty(ent);
    }

    /// <summary>
    /// Telecharge will display its science points if it has any
    /// </summary>
    private void OnExamined(Entity<TelechargeComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Science > 0)
        {
            args.PushMarkup(Loc.GetString("telecharge-examine", ("points", ent.Comp.Science)));
        }
    }

    /// <summary>
    /// find which target is the closest to the telecharge
    /// </summary>
    /// <param name="targets">Hashset of all targets</param>
    /// <param name="source">MapCoordinates of Telecharge</param>
    /// <returns>Closest entity and its MapCoordinates</returns>
    private (EntityUid, double?) GetClosest(HashSet<EntityUid> targets, MapCoordinates source)
    {
        double? closest = null;
        var closestEnt = new EntityUid();
        foreach (var target in targets)
        {
            var distance = GetMapCoordinatesDistance(source, _transform.ToMapCoordinates(Transform(target).Coordinates));
            if (closest == null || distance < closest)
            {
                closest = distance;
                closestEnt = target;
            }
        }
        return (closestEnt, closest);
    }

    /// <summary>
    /// Check within telecharge's range to pick out whitelisted targets
    /// </summary>
    /// <param name="ent">The Telecharge Entity</param>
    /// <param name="range">Range to scan in</param>
    /// <param name="whitelist">Accepted Whitelist</param>
    /// <returns>Hashset of whitelisted targets</returns> <
    private HashSet<EntityUid> GetWhitelistTargets(EntityUid ent, float range, EntityWhitelist? whitelist = null)
    {
        var entities = _lookup.GetEntitiesInRange(ent, range, RangeFlags); //get all entities within range
        var whitelistEntities = new HashSet<EntityUid>();
        foreach (var target in entities) //for each entity
        {
            if (_whitelistSystem.IsWhitelistPass(whitelist, target)) //check if matching whitelist
            {
                whitelistEntities.Add(target); //if yes, add to list
                //Log.Debug(target.ToString());
            }
        }
        return whitelistEntities; //return whitelisted entities in range
    }

    /// <summary>
    /// Get distance between two map coordinates with the power of primary school maths.
    /// </summary>
    /// <param name="a">Source</param>
    /// <param name="b">Target</param>
    /// <returns>distance between points</returns>
    private double GetMapCoordinatesDistance(MapCoordinates a, MapCoordinates b)
    {   // a^2 + b^2 = c^2
        return Math.Sqrt(Math.Pow(Math.Abs(a.X - b.X), 2) + Math.Pow(Math.Abs(a.Y - b.Y), 2));
    }

    protected void UpdateAppearance(Entity<TelechargeComponent> ent)
    {
        TelechargeVisualState state;
        if (ent.Comp.Science > 0)
            state = TelechargeVisualState.Full;
        else
            state = TelechargeVisualState.Empty;

        if (HasComp<TelechargeRechargingComponent>(ent))
            state = TelechargeVisualState.Recharging;

        _appearance.SetData(ent.Owner, TelechargeVisuals.VisualState, state); //Dirties itself
        Dirty(ent);
    }

    private void OnRechargeStart(Entity<TelechargeRechargingComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<TelechargeComponent>(ent, out var telecomp))
            UpdateAppearance((ent.Owner, telecomp));
    }

    private void OnRechargeEnd(Entity<TelechargeRechargingComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<TelechargeComponent>(ent, out var telecomp))
            UpdateAppearance((ent.Owner, telecomp));
    }

    // See server-side
    protected virtual string GetVagueLocation(MapCoordinates coords)
    {
        return "[Unknown]";
    }

    // See server-side
    protected virtual void SendRadioMessage(Entity<TelechargeComponent> ent, string message)
    {
    }


}
