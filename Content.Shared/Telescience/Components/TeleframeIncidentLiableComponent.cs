using Robust.Shared.GameStates;

namespace Content.Shared.Telescience;

[RegisterComponent, NetworkedComponent]
public sealed partial class TeleframeIncidentLiableComponent : Component
{
    /// <summary>
    /// Chance of an Anomalous Incident occurring from a Teleportation event. Chance is per Teleported entity.
    /// </summary>
    [DataField]
    public float IncidentChance = 0.00f;

    /// <summary>
    /// Severity Multiplier of Anomalous incidents. High Severity increases the likelyhood of very significant events.
    /// </summary>
    [DataField]
    public float IncidentMultiplier = 1f;

    /// <summary>
    /// Chance an incident will occur at the target, inverse of this is chance of occurring at the source
    /// </summary>
    [DataField]
    public float IncidentTarget = 0.5f;

    /// <summary>
    /// Minimum Severity level required for an incident of Minor, Moderate, Major, or Malefic occuring.
    /// </summary>
    [DataField]
    public List<float> IncidentSeverityMinimum = new List<float> { 0, 1, 2, 3 };

    //potentially with upgrades, emagging could be considered an invisible "upgrade" that can't be gotten rid of and moved to there.
    /// <summary>
    /// Effect of an emag on Teleframe Incident Chance
    /// </summary>
    [DataField]
    public float EmagIncidentChance = 1f;

    /// <summary>
    /// Effect of an emag on Teleframe Incident Multiplier
    /// </summary>
    [DataField]
    public float EmagIncidentMultiplier = 1f;

}
