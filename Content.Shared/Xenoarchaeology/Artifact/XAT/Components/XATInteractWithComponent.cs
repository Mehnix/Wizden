using Robust.Shared.GameStates;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact trigger that activates when an entity interacts with the artifact
/// EG: A user clicks on the artifact whilst holding a specific object. Or, a specific projectile strikes the artifact.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATInteractWithSystem))]
public sealed partial class XATInteractWithComponent : Component
{
    /// <summary>
    /// Whether to destroy the interacting entity afterwards
    /// EG: feed the artifact a pizza slice, it eats it
    /// </summary>
    [DataField]
    public bool DestroyAfter = false;

    /// <summary>
    /// Whitelist of allowed interacting entities
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Additional Sound played on successful item interaction
    /// </summary>
    [DataField]
    public SoundSpecifier? TriggerSound;
}
