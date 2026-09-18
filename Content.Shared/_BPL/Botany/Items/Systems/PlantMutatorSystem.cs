using Content.Shared.Botany.Components;
using Content.Shared.Interaction;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Content.Shared._BPL.Botany.Items.Components;

namespace Content.Shared._BPL.Botany.Items.Systems;

public sealed partial class PlantMutatorSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _entityEffects = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<PlantMutatorComponent> ent, ref AfterInteractEvent args)
    {
        //only fire off on plant holders with a plant.
        if (!args.Target.HasValue || !TryComp<PlantHolderComponent>(args.Target, out var holder))
            return;

        if (!_interactionSystem.InRangeUnobstructed(args.User, (EntityUid)args.Target))
            return;

        if (holder.Dead)
        {
            _popup.PopupEntity(Loc.GetString("mutator-dead"), ent.Owner, args.User);
            return;
        }

        //TODO: May need to add some mutations to the plant's mutation list to persist.
        _popup.PopupEntity(Loc.GetString("mutator-applied"), ent.Owner, args.User);
        //destroy plant mutator once used.
        if (_entityEffects.TryApplyEffect(args.Target.Value, ent.Comp.EntityEffect))
            QueueDel(ent.Owner);
        args.Handled = true;
    }
}
