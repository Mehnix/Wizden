using Content.Shared.Destructible.Thresholds;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for an artifact trigger that activates when an entity interacts with the artifact
/// EG: A user clicks on the artifact whilst holding a specific object. Or, a specific projectile strikes the artifact.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATInteractAttackSystem))]
public sealed partial class XATInteractAttackComponent : Component
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
    /// Number of interactions required to trigger
    /// Interacting with a stack counts a number of interactions equal to the stack count
    /// </summary>
    [DataField]
    public MinMax InteractionCount = new(1, 3);

    public int MaxCount = 0;

    /// <summary>
    /// Number of interactions to go
    /// </summary>
    public int Count = 0;


}

