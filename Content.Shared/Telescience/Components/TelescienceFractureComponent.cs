using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared.Telescience.Components;

/// <summary>
/// Main component for Telescience Distortions
/// Usually invisible entities that produce minor effects that could be construed as space madness. Teleport a telecharge next to them to get science.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TelescienceFractureComponent : Component
{
    /// <summary>
    /// Maximum amount of HP (science points) that can rolled for
    /// </summary>
    [DataField]
    public int MaxHealthRoll = 30000;

    /// <summary>
    /// Minimum amount of HP (science points) that can be rolled for
    /// </summary>
    [DataField]
    public int MinHealthRoll = 15000;

    /// <summary>
    /// Amount of HP regenerated per second if not at max HP.
    /// </summary>
    [DataField]
    public int HealthRegen = 10;

    /// <summary>
    /// science points gradiant, damage = MaxHealth * x/distance+x
    /// </summary>
    [DataField]
    public float Gradiant = 2f;

    //##################################

    /// <summary>
    /// The distortion's rolled max HP
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int MaxHealth = 1000;

    /// <summary>
    /// The distortion's current HP
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public int Health = 1000;
}
