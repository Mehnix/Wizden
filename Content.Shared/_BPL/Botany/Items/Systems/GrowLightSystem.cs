
using Content.Shared._BPL.Botany.Items.Components;
using Content.Shared._BPL.Botany.Items.Events;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Botany;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Botany.Events;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._BPL.Botany.Items.Systems;

public sealed partial class GrowLightSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private PlantTraySystem _plantTray = default!;
    [Dependency] private PlantHolderSystem _plantHolder = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<PlantTrayComponent> _trayQuery;
    public override void Initialize()
    {
        base.Initialize();
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<GrowLightComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<PlantTrayComponent>(args.Target) || HasComp<GrowLightComponent>(args.Target))
            return;

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ApplyTime, new GrowLightDoAfterEvent(), ent.Owner, target: args.Target, used: ent.Owner)
        {
            Broadcast = true,
            BreakOnMove = true,
            NeedHand = true,
        });
    }

    /// <summary>
    /// Apply the grow light components values to the tray
    /// </summary>
    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<GrowLightComponent> ent, ref GrowLightDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted || args.Cancelled || args.Handled || args.Target == null || !TryComp<PlantTrayComponent>(args.Args.Target, out var tray))
            return;

        args.Handled = true;
        var newLight = new GrowLightComponent();
        newLight.BoostCount = ent.Comp.BoostCount;
        AddComp(args.Target.Value, newLight);
        //SpawnAttachedTo("HydroponicsGrowLightUpgrade", Transform(args.Args.Target.Value).Coordinates);
        //QueueDel(ent.Owner);
        var xform = Transform(args.Args.Target.Value);
        _transform.SetLocalPosition(ent.Owner, xform.LocalPosition, xform);
        _transform.SetParent(ent.Owner, xform, args.Args.Target.Value);
    }

    [SubscribeLocalEvent]
    private void OnTrayUpdate(Entity<GrowLightComponent> ent, ref TrayUpdateEvent args)
    {
        if (!_trayQuery.TryComp(ent.Owner, out var trayComp)) //need to consider update for trays is 3 seconds and update for plant is 15
            return;

        if (!_plantTray.TryGetPlant((ent.Owner, trayComp), out var plant) || plant == null)
            return;

        var ev = new PlantGrowEvent(ent.Owner);
        RaiseLocalEvent(plant.Value, ref ev);
    }
}
