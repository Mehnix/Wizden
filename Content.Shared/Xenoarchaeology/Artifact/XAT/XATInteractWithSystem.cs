using Content.Shared.Interaction;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires some way of 'using' (with default action) an artifact entity.
/// </summary>
public sealed partial class XATInteractWithSystem : BaseXATSystem<XATInteractWithComponent>
{
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<AttackedEvent>(OnAttacked);
        XATSubscribeDirectEvent<InteractUsingEvent>(OnInteractHand);
        XATSubscribeDirectEvent<StartCollideEvent>(OnStartCollide);
    }

    /// <summary>
    /// Trigger the node if the entity used to attack matches the whitelist
    /// </summary>
    private void OnAttacked(Entity<XenoArtifactComponent> artifact, Entity<XATInteractWithComponent, XenoArtifactNodeComponent> node, ref AttackedEvent args)
    {
        Log.Debug("Attacked");
        if (CheckEntity(artifact.Owner, args.Used, node.Comp1))
            Trigger(artifact, node);
    }

    /// <summary>
    /// Trigger the node if the entity used in interaction matches the whitelist
    /// </summary>
    private void OnInteractHand(Entity<XenoArtifactComponent> artifact, Entity<XATInteractWithComponent, XenoArtifactNodeComponent> node, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Log.Debug("Interacted");

        if (CheckEntity(artifact.Owner, args.Used, node.Comp1))
            Trigger(artifact, node);
    }

    /// <summary>
    /// Trigger the node if the colliding entity matches the whitelist
    /// </summary>
    private void OnStartCollide(Entity<XenoArtifactComponent> artifact, Entity<XATInteractWithComponent, XenoArtifactNodeComponent> node, ref StartCollideEvent args)
    {
        Log.Debug("Collided");

        if (CheckEntity(artifact.Owner, args.OtherEntity, node.Comp1))
            Trigger(artifact, node);
    }

    /// <summary>
    /// Check whitelist match and delete entity if appropriate
    /// </summary>
    private bool CheckEntity(EntityUid interacted, EntityUid interacter, XATInteractWithComponent comp)
    {
        if (_whitelistSystem.IsWhitelistPassOrNull(comp.Whitelist, interacter))
        {
            if (comp.TriggerSound != null)
                _audio.PlayPredicted(comp.TriggerSound, interacted, interacted); //play on the artifact as the interacter may be deleted

            if (comp.DestroyAfter == true)
                PredictedQueueDel(interacter);

            return true;
        }
        else
        {
            return false;
        }

    }
}
