using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.Gravity;

/// <summary>
/// Makes the user float
/// </summary>
public sealed partial class FloatingStatusEffectSystem : EntitySystem
{
    [Dependency] private SharedGravitySystem _gravity = default!;
    public static readonly EntProtoId FloatingStatusEffect = "StatusEffectFloating";

    [SubscribeLocalEvent]
    private void OnFloatingStatusApplied(Entity<FloatingStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _gravity.RefreshWeightless(args.Target, true);
    }

    [SubscribeLocalEvent]
    private void OnFloatingStatusRemoved(Entity<FloatingStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _gravity.RefreshWeightless(args.Target, false);
    }

    [SubscribeLocalEvent]
    private void OnRefreshFloatingStatus(Entity<FloatingStatusEffectComponent> ent, ref StatusEffectRelayedEvent<GravityChangedEvent> args)
    {
        _gravity.RefreshWeightless(args.AppliedTo, true);
    }
}
