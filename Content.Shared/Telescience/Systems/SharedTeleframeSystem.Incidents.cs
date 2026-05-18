using System.Diagnostics.CodeAnalysis;
using Content.Shared.Emag.Systems;
using Content.Shared.Random.Helpers;
using Content.Shared.Telescience.Events;
using Robust.Shared.Random;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    protected virtual void InitializeIncidents()
    {
        SubscribeLocalEvent<TeleframeIncidentLiableComponent, TeleframeTeleportedAllEvent>(OnIncidentTeleported);
        SubscribeLocalEvent<TeleframeIncidentLiableComponent, TeleframeTeleportFailedEvent>(OnIncidentFailed);

        SubscribeLocalEvent<TeleframeIncidentLiableComponent, GotEmaggedEvent>(OnIncidentEmagged);
    }
    /// <summary>
    /// Once the teleframe finishes teleportation, roll for incident
    /// </summary>

    private void OnIncidentTeleported(Entity<TeleframeIncidentLiableComponent> ent, ref TeleframeTeleportedAllEvent args)
    {
        if (!TryRollForIncident(ent, out var severity))
            return;

        if (severity != null)
            return;

        var rand = new RobustRandom(); //generate a new RobustRandom object with its own seed the Client and Server can agree on
        rand.SetSeed(SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent.Owner).Id));

        if (rand.NextFloat() < ent.Comp.IncidentTarget) //choose whether incident occurs at target or source
        {
            var target = GetTeleportalTarget(args.TeleportInfo);
            if (target != EntityUid.Invalid)
                DoIncident(target, severity!.Value);
        }
        else
        {
            var source = GetTeleportalSource(args.TeleportInfo);
            if (source != EntityUid.Invalid)
                DoIncident(source, severity!.Value);
        }
    }

    /// <summary>
    /// Teleport failiures can also result in incidents, but only at the source
    /// </summary>
    private void OnIncidentFailed(Entity<TeleframeIncidentLiableComponent> ent, ref TeleframeTeleportFailedEvent args)
    {
        if (!TryRollForIncident(ent, out var severity))
            return;

        if (args.TeleportInfo == null || args.TeleportInfo is not { } teleInfo || severity != null)
            return;

        var target = GetTeleportalSource(teleInfo); //teleportation failed, blame the teleframe
        if (target != EntityUid.Invalid)
            DoIncident(target, severity!.Value);

    }

    private void DoIncident(EntityUid target, float severity)
    {
        //Something Something just a week away
    }

    /// <summary>
    /// Adds the emag flag to the Teleframe, makes the Teleframe more dangerous, cumulative with any other effect that does that.
    /// </summary>
    private void OnIncidentEmagged(Entity<TeleframeIncidentLiableComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    private bool TryRollForIncident(Entity<TeleframeIncidentLiableComponent> ent, [NotNullWhen(true)] out float? severity)
    {
        var rand = new RobustRandom(); //generate a new RobustRandom object with its own seed the Client and Server can agree on
        rand.SetSeed(SharedRandomExtensions.HashCodeCombine((int)_timing.CurTick.Value, GetNetEntity(ent.Owner).Id));
        var roll = rand.NextFloat();

        var chance = _emag.CheckFlag(ent.Owner, EmagType.Interaction) ? ent.Comp.EmagIncidentChance + ent.Comp.IncidentChance : ent.Comp.IncidentChance;
        var multiplier = _emag.CheckFlag(ent.Owner, EmagType.Interaction) ? ent.Comp.EmagIncidentMultiplier + ent.Comp.IncidentMultiplier : ent.Comp.IncidentMultiplier;

        if (roll < chance)
        {
            severity = rand.NextFloat() * multiplier;
            return true;
        }
        else
        {
            severity = null;
            return false;
        }
    }

}
