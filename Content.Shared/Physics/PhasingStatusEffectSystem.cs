using Content.Shared.StatusEffectNew;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Physics;

/// <summary>
/// Makes the user phase through walls.
/// </summary>
public sealed partial class PhasingStatusEffectSystem : EntitySystem
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    public static readonly EntProtoId PhasingStatusEffect = "StatusEffectPhasing";

    [SubscribeLocalEvent]
    private void OnPhasingStatusApplied(Entity<PhasingStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        Log.Debug("effected");
        if (!TryComp<FixturesComponent>(args.Target, out var fixtures))
            return;

        Log.Debug("Bazinga");
        foreach (var fixture in fixtures.Fixtures.Values)
            _physics.SetHard(args.Target, fixture, false, fixtures);
    }

    [SubscribeLocalEvent]
    private void OnPhasingStatusRemoved(Entity<PhasingStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (!TryComp<FixturesComponent>(args.Target, out var fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures.Values)
            _physics.SetHard(args.Target, fixture, true, fixtures);
    }

    [SubscribeLocalEvent]
    private void OnRefreshPhasingStatus(Entity<PhasingStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CollisionLayerChangeEvent> args)
    {
        if (!TryComp<FixturesComponent>(args.AppliedTo, out var fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures.Values)
            _physics.SetHard(args.AppliedTo, fixture, false, fixtures);
    }
}
