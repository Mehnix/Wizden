using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires user access
/// This just handles the trigger and emag interactions, <see cref="AccessReaderComponent"/> handles accesses
/// </summary>
public sealed partial class XATAccessSystem : BaseXATSystem<XATAccessComponent>
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedXenoArtifactSystem _xenoarch = default!;
    public override void Initialize()
    {
        base.Initialize();
        XATSubscribeDirectEvent<InteractUsingEvent>(OnInteractUsing);
        XATSubscribeDirectEvent<ActivateInWorldEvent>(OnActivateWorld);
        XATSubscribeDirectEvent<GotEmaggedEvent>(OnNodeEmagged);
        XATSubscribeDirectEvent<ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<XenoArtifactComponent, GotEmaggedEvent>(OnEmagged);
    }

    private void OnActivateWorld(Entity<XenoArtifactComponent> artifact, Entity<XATAccessComponent, XenoArtifactNodeComponent> node, ref ActivateInWorldEvent args)
    {     //this will only work if there are no active nodes as activating the artifact overrides this, just here for completeness
        if (CheckAccess(args.User, node.Owner))
        {
            Trigger(artifact, node);
            _audio.PlayPredicted(node.Comp1.AccessSound, artifact.Owner, args.User);
        }
        else
            _audio.PlayPredicted(node.Comp1.DeniedSound, artifact.Owner, args.User);
    }
    private void OnInteractUsing(Entity<XenoArtifactComponent> artifact, Entity<XATAccessComponent, XenoArtifactNodeComponent> node, ref InteractUsingEvent args)
    {
        if (CheckAccess(args.User, node.Owner))
        {
            Trigger(artifact, node);
            _audio.PlayPredicted(node.Comp1.AccessSound, artifact.Owner, args.User);
        }
        else
            _audio.PlayPredicted(node.Comp1.DeniedSound, artifact.Owner, args.User);
    }
    private void OnNodeEmagged(Entity<XenoArtifactComponent> artifact, Entity<XATAccessComponent, XenoArtifactNodeComponent> node, ref GotEmaggedEvent args)
    {
        Trigger(artifact, node);
    }

    /// <summary>
    /// Read access from interaction
    /// </summary>
    /// <returns> true if appropriate access </returns>
    private bool CheckAccess(EntityUid user, EntityUid node)
    {
        if (!TryComp<AccessReaderComponent>(node, out var accessComp)) //the access trigger should have an AccessReaderComponent alongside it
            return false;

        if (_access.IsAllowed(user, node, accessComp))
            return true;

        return false;

    }

    /// <summary>
    /// Check whether there is a node requiring access, if there is then handle the zapping of the emag.
    /// We also raise the GotEmaggedEvent here onto other nodes because otherwise we're subscribing twice
    /// Which is why this isn't in the list of relayed events
    /// </summary>
    private void OnEmagged(Entity<XenoArtifactComponent> ent, ref GotEmaggedEvent args)
    {
        var nodes = _xenoarch.GetAllNodes(ent);
        var ev = new XenoArchNodeRelayedEvent<GotEmaggedEvent>(ent, args);

        foreach (var node in nodes) // here we make sure all nodes get the event
            RaiseLocalEvent(node, ref ev);

        foreach (var node in nodes) //but here we only need to care about zapping once, so here we just stop once it's found
        {
            if (HasComp<XATAccessComponent>(node.Owner))
            {
                args.Handled = true;
                args.Repeatable = true;
                return;
            }
        }
    }
    private void OnExamine(Entity<XenoArtifactComponent> artifact, Entity<XATAccessComponent, XenoArtifactNodeComponent> node, ref ExaminedEvent args)
    {
        RaiseLocalEvent(node, args); //raise on the node and let AccessReaderSystem handle it
    }

}
