using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
namespace Content.Shared.Telescience.Components;

/// <summary>
/// Component added to teleportal entities to keep track of special interactions they have
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TeleframeTeleportalComponent : Component
{
    /// <summary>
    /// Effect produced if this teleportal is destroyed
    /// </summary>
    [DataField]
    public EntProtoId? TeleportalDestructionEffect = null;

    /// <summary>
    /// Teleframe related to this Teleportal
    /// </summary>
    [DataField, ViewVariables]
    public EntityUid? Teleframe;

    /// <summary>
    /// Is teleportation complete? if so, deleting this shouldn't cause any issues
    /// </summary>
    public bool Complete = false;
}
