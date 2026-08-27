using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Applies list of status effects to everything in range when effect is activated.
/// </summary>
[RegisterComponent, Access(typeof(XAEApplyStatusEffectSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XAEApplyStatusEffectComponent : Component
{
    /// <summary>
    /// List of status effects to apply
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> Effects = new();

    /// <summary>
    /// Range within which targets will be effected by status effect.
    /// If zero, limited to only the artifact and whoever activated it or whoever it is activated on (clicking on another person with an item arti).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 2f;

    /// <summary>
    /// Amount of time the artifact will be effected by the status effect for. Applied independently of area of effect.
    /// If zero, only the targets will be effected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ArtifactDuration = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Amount of time surrounding targets will be effected by the status effect for.
    /// If zero, only the artifact will be effected.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan TargetDuration = TimeSpan.FromSeconds(10);
}
