using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ChargeRecharge.Components;

/// <summary>
/// Component that holds times that something charges or recharges for, as well as the visuals related to doing so.
/// This component is not required for the <see cref="SharedChargeRechargeSystem"/> to work, it just lets you provide generic charging and recharging times.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChargeRechargeComponent : Component
{
    /// <summary>
    /// The amount of time charging occurs for
    /// </summary>
    [DataField, ViewVariables]
    public TimeSpan? ChargeDuration = null;

    /// <summary>
    /// The amount of time recharging occurs for
    /// </summary>
    [DataField, ViewVariables]
    public TimeSpan? RechargeDuration = null;

    /// <summary>
    /// whether charging/recharging may occur
    /// </summary>
    public bool IsEnabled = true;

    /// <summary>
    /// Examine text
    /// </summary>
    [DataField]
    public string? ChargingString = "examine-charging";
    [DataField]
    public string? RechargingString = "examine-recharging";
    [DataField]
    public string? PausedString = "examine-recharging-paused";

}

[NetSerializable, Serializable]
public enum ChargeRechargeVisuals : byte
{
    VisualState
}

[NetSerializable, Serializable]
public enum ChargeRechargeVisualState : byte
{
    On,
    Charging,
    Recharging,
    Off
}
