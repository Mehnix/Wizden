using Content.Shared._BPL.Botany.Items.Components;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Items.Components;
using Content.Shared.Botany.Items.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Swab;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._BPL.Botany.Items.Systems;

public sealed partial class SwabApplicatorSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private EntityQuery<PlantComponent> _plantQuery;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Ensure the Swab Applicator is primed to swab a plant, will only be able to do so if it has swab data provided to it by its contained swab.
    /// </summary>
    [SubscribeLocalEvent(before: [typeof(BotanySwabSystem)])]
    private void OnAfterInteract(Entity<SwabApplicatorComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach || !_plantQuery.HasComp(args.Target))
            return;

        if (TryComp<BotanySwabComponent>(ent, out var swabComp) && swabComp.PlantData != null && swabComp.PlantProtoId != null)
            return;

        _popup.PopupEntity(Loc.GetString("swab-applicator-unusable"), ent.Owner, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Updates the swab applicator's Plant data based on its stored swab to prevent contamination
    /// </summary>
    [SubscribeLocalEvent(after: [typeof(BotanySwabSystem)])]
    private void OnDoAfter(Entity<SwabApplicatorComponent> ent, ref BotanySwabDoAfterEvent args)
    {
        if (!TryComp<BotanySwabComponent>(ent, out var applicatorSwabComp) || ent.Comp.SwabContainer == null || !_container.TryGetContainer(ent, ent.Comp.SwabContainer, out var swabContainer))
            return;

        foreach (var swab in swabContainer.ContainedEntities)
        {
            if (TryComp<BotanySwabComponent>(swab, out var swabComp))
            {
                applicatorSwabComp.PlantData = swabComp.PlantData;
                applicatorSwabComp.PlantProtoId = swabComp.PlantProtoId;
                Dirty(ent);
                return;
            }
        }
    }

    ///<summary>
    /// On swab insert check swab has pollen, cancel if it doesn't.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInsertAttempt(Entity<BotanySwabComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        //does the container have the botanySwab component (should always be the case)
        if (!HasComp<SwabApplicatorComponent>(args.Container.Owner))
            return;

        //does the swab have plantdata (aka, is not null)
        if (ent.Comp.PlantData != null || ent.Comp.PlantProtoId != null)
            return;

        //if these are not true, cancel, clean swabs aren't allowed.
        _popup.PopupEntity(Loc.GetString("swab-applicator-needs-pollen"), ent.Owner);
        args.Cancel();
        return;
    }

    ///<summary>
    /// On swab successfully inserted transfer its PlantData to the applicator's own botany swab component
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInsert(Entity<BotanySwabComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (!TryComp<BotanySwabComponent>(args.Container.Owner, out var swabComp))
            return;

        swabComp.PlantData = ent.Comp.PlantData;
        swabComp.PlantProtoId = ent.Comp.PlantProtoId;
        Dirty(args.Container.Owner, swabComp);
    }

    ///<summary>
    /// Swab Applicator, on removing swab, set Applicator's botany swab component back to null
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRemove(Entity<BotanySwabComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (!TryComp<BotanySwabComponent>(args.Container.Owner, out var swabComp))
            return;

        swabComp.PlantData = null;
        swabComp.PlantProtoId = null;
        Dirty(args.Container.Owner, swabComp);
    }

    ///<summary>
    /// Remove a swab's PlantData
    /// </summary>
    [SubscribeLocalEvent]
    private void OnClean(Entity<BPLSwabComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !ent.Comp.Cleanable)
            return;

        if (!TryComp<BotanySwabComponent>(ent, out var swabComp))
            return;

        swabComp.PlantData = null;
        swabComp.PlantProtoId = null;
        Dirty(ent);
        _popup.PopupEntity(Loc.GetString("botany-swab-clean"), ent.Owner, args.User);
        _audio.PlayPredicted(ent.Comp.CleanSound, ent.Owner, args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Updates the swab applicator's Plant data based on its stored swab to prevent contamination
    /// </summary>
    [SubscribeLocalEvent(after: [typeof(BotanySwabSystem)])]
    private void OnDoAfter(Entity<BPLSwabComponent> ent, ref BotanySwabDoAfterEvent args)
    {
        if (args.Cancelled || !_plantQuery.HasComp(args.Args.Target))
            return;

        _audio.PlayPredicted(ent.Comp.SwabSound, ent.Owner, args.User);
    }
}
