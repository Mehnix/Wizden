using Content.Shared.StatusEffectNew;
using Content.Shared.Xenoarchaeology.Artifact.XAE.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE;

/// <summary>
/// System for applying status effects when artifact effect is activated.
/// </summary>
public sealed partial class XAEApplyStatusEffectSystem : BaseXAESystem<XAEApplyStatusEffectComponent>
{
    private const LookupFlags RangeFlags = LookupFlags.Approximate | LookupFlags.Dynamic;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private StatusEffectsSystem _status = default!;

    protected override void OnActivated(Entity<XAEApplyStatusEffectComponent> ent, ref XenoArtifactNodeActivatedEvent args)
    {
        Log.Debug("run");
        AddStatus(ent.Comp.Effects, args.Artifact, ent.Comp.ArtifactDuration); //status effect system cancels zero time duration for us. No need to do it here.

        if (ent.Comp.Range > 0)
        {
            var entities = _lookup.GetEntitiesInRange(ent.Owner, ent.Comp.Range, RangeFlags); // will not look for static entities or anything inside a container.
            entities.Remove(args.Artifact); //don't effect the artifact again.
            foreach (var entity in entities)
                AddStatus(ent.Comp.Effects, entity, ent.Comp.TargetDuration); //apply to all found entities. Status effect system will sort out which are valid.
        }
        else
        {
            if (args.Target != null)
            {
                AddStatus(ent.Comp.Effects, args.Target.Value, ent.Comp.TargetDuration);
                return; // end early if there's a target, lets you target a specific person and not effect yourself.
            }

            if (args.User != null)
                AddStatus(ent.Comp.Effects, args.User.Value, ent.Comp.TargetDuration);
        }

    }

    private void AddStatus(List<EntProtoId> effects, EntityUid target, TimeSpan duration)
    {
        Log.Debug(ToPrettyString(target));
        foreach (var effect in effects)
            _status.TryAddStatusEffectDuration(target, effect, duration);
    }
}
