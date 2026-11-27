using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     Acts the same as the WhitelistChameleon Tag in allowing entities to be recognised as chameleonable
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedChameleonClothingSystem))]
public sealed partial class WhitelistChameleonComponent : Component
{
    /// <summary>
    /// Whether this entity can be chameleoned.
    /// </summary>
    [DataField]
    public bool Valid = true;

    /// <summary>
    /// List of supplementary tags to specify what chameleon lists this entity should appear in.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public HashSet<ProtoId<TagPrototype>> Tags = new();
}
