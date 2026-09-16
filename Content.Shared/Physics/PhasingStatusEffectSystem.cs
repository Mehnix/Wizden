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
    [Dependency] private OccluderSystem _occluder = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    public static readonly EntProtoId PhasingStatusEffect = "StatusEffectPhasing";

    [SubscribeLocalEvent]
    private void OnPhasingStatusApplied(Entity<PhasingStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (!TryComp<FixturesComponent>(args.Target, out var fixtures))
            return;

        ent.Comp.Fixtures = fixtures.Fixtures; //store old fixtures to be reapplied later

        foreach (var fixture in fixtures.Fixtures.Values) //set to non collide
        {
            _physics.SetHard(args.Target, fixture, false, fixtures);
        }

        if (TryComp<OccluderComponent>(args.Target, out var occluderComp))
        {
            ent.Comp.Occluded = occluderComp.Enabled;
            _occluder.SetEnabled(args.Target, false, occluderComp);
        }

        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnPhasingStatusRemoved(Entity<PhasingStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (!TryComp<FixturesComponent>(args.Target, out var fixtures))
            return;

        var meta = MetaData(args.Target).EntityPrototype;
        if (meta == null || !meta.TryComp<FixturesComponent>(out var protoFixturesComp, EntityManager.ComponentFactory))
            return;

        foreach (var fixture in fixtures.Fixtures) //restore old fixtures
        {
            if (!protoFixturesComp.Fixtures.TryGetValue(fixture.Key, out var protoFixturesValue))
                continue;

            _physics.SetHard(args.Target, fixture.Value, protoFixturesValue.Hard, fixtures);
        }

        if (TryComp<OccluderComponent>(args.Target, out var occluderComp))
            _occluder.SetEnabled(args.Target, ent.Comp.Occluded, occluderComp); //restore old occlusion
    }

    [SubscribeLocalEvent]
    private void OnRefreshPhasingStatus(Entity<PhasingStatusEffectComponent> ent, ref StatusEffectRelayedEvent<CollisionLayerChangeEvent> args)
    {
        if (!TryComp<FixturesComponent>(args.AppliedTo, out var fixtures))
            return;

        foreach (var fixture in fixtures.Fixtures.Values)
            _physics.SetHard(args.AppliedTo, fixture, false, fixtures);

        if (TryComp<OccluderComponent>(args.AppliedTo, out var occluderComp))
            _occluder.SetEnabled(args.AppliedTo, false, occluderComp);

        Dirty(ent);
    }
}
