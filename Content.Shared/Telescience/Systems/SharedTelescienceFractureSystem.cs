using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;
using Robust.Shared.Random;
using Robust.Shared.Toolshed;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTelescienceFractureSystem : EntitySystem
{
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly SharedAnomalySystem _anomaly = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelescienceFractureComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TelescienceFractureComponent, TelechargeScanEvent>(OnScanned);
    }

    private void OnStartup(Entity<TelescienceFractureComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.MaxScience = Random.Next(ent.Comp.MinScienceRoll, ent.Comp.MaxScienceRoll);
        ent.Comp.CurrentScience = ent.Comp.MaxScience;
        Dirty(ent);
    }

    /// <summary>
    /// Scan the Reality Fracture, increasing its severity and recovering science
    /// </summary>
    /// <param name="ent"></param>
    /// <param name="args"></param>
    private void OnScanned(Entity<TelescienceFractureComponent> ent, ref TelechargeScanEvent args)
    {
        Log.Debug("Scanned");
        var damage = DoScanDamage(ent, args.Distance, args.IncidentMult);
        if (HasComp<TelechargeComponent>(args.Telecharge))
        {
            var scienceDamage = (damage * ent.Comp.MaxScience);
            ent.Comp.CurrentScience -= (int)scienceDamage;
            if (args.IncidentMult > 0) //high incident multipler, more science! (and more danger!)
                scienceDamage *= args.IncidentMult;
            Dirty(ent);
            var status = (float)(ent.Comp.CurrentScience / ent.Comp.MaxScience); //can be negative
            var scienceEv = new TelechargeAddScienceEvent((int)scienceDamage, status);

            RaiseLocalEvent(args.Telecharge, ref scienceEv); //send a science event back to the telecharge
        }
    }
    /// <summary>
    /// deal damage, increase severity and pulse if not going supercritical
    /// </summary>
    /// <param name="ent">The Fracture</param>
    /// <param name="distance">Distance from the fracture</param>
    /// <param name="multiplier">Incident Multiplier</param>
    /// <returns></returns>
    private float DoScanDamage(Entity<TelescienceFractureComponent> ent, float distance, float multiplier)
    {
        var damage = ent.Comp.Gradiant / (ent.Comp.Gradiant + distance);
        if (TryComp<AnomalyComponent>(ent, out var anomComp))
        {
            Log.Debug($"Dealt {damage} severity damage to {ToPrettyString(ent.Owner)}");
            _anomaly.ChangeAnomalySeverity(ent.Owner, damage, anomComp); //update severity from scanning, will blow up if over 1.
            if (anomComp.Severity < 1)
                _anomaly.DoAnomalyPulse(ent.Owner, anomComp); //if we aren't going to blow up, then pulse in response to being scanned instead
        }
        return damage;
    }
}

