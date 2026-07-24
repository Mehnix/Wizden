using Content.Shared.Flash;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger on being flashed by any source
/// In an ideal world we could have a generic trigger for any event defined in by a component...
/// </summary>
public sealed partial class XATFlashSystem : BaseXATSystem<XATFlashComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        XATSubscribeDirectEvent<FlashAttemptEvent>(OnFlashed);
    }

    private void OnFlashed(Entity<XenoArtifactComponent> artifact, Entity<XATFlashComponent, XenoArtifactNodeComponent> node, ref FlashAttemptEvent args)
    {
        Trigger(artifact, node);
    }
}
