using Content.Shared.Chat;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger that requires a specific emote from any mob near artifact.
/// </summary>
public sealed partial class XATEmoteSystem : BaseXATSystem<XATEmoteComponent>
{
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<XenoArtifactComponent> _xenoArtifactQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BeforeEmoteEvent>(OnEmote); //Listen for all emote attempts
    }   //This doesn't care about being muted. Acting out a scream is good enough.

    private void OnEmote(ref BeforeEmoteEvent args)
    {
        if (args.Cancelled == true)
            return;

        var targetCoords = Transform(args.Source).Coordinates;

        var query = EntityQueryEnumerator<XATEmoteComponent, XenoArtifactNodeComponent>();
        while (query.MoveNext(out var uid, out var comp, out var node)) // Find all artifact nodes with this component
        {
            if (node.Attached == null)
                continue;

            if (!comp.Emotes.Contains(args.Emote)) // Does the emote match our list
                continue;

            var artifact = _xenoArtifactQuery.Get(node.Attached.Value);

            if (!CanTrigger(artifact, (uid, node)))
                continue;

            var artifactCoords = Transform(artifact).Coordinates;

            if (_transform.InRange(targetCoords, artifactCoords, comp.Range))
                Trigger(artifact, (uid, comp, node));
        }
    }
}
