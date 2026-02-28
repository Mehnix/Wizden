using Robust.Shared.Prototypes;
namespace Content.Shared.Telescience.Components;

/// <summary>
/// Component added to teleportal entities to keep track of special interactions they have
/// </summary>
[RegisterComponent]
public sealed partial class TeleframeTeleportalComponent : Component
{
    /// <summary>
    /// Effect produced if this teleportal is destroyed
    /// </summary>
    [DataField]
    public EntProtoId? TeleportalDestructionEffect = null;

    [DataField, ViewVariables]
    public EntityUid? Teleframe;
}
