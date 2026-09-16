using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics;

namespace Content.Shared.Physics;

/// <summary>
/// This is used for a status effect that lets you ignore gravity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PhasingStatusEffectComponent : Component
{
    /// <summary>
    /// Datafield for keeping track of the target's fixtures prior to being phased
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("fixtures", customTypeSerializer: typeof(FixtureSerializer))]
    public Dictionary<string, Fixture> Fixtures = new();

    /// <summary>
    /// Datafield for keeping track of if the target was occluded prior to the status effect being applied
    /// </summary>
    [DataField]
    public bool Occluded = false;
}
