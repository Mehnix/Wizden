using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ChargeRecharge.Components;

/// <summary>
/// Component that holds times that something charges or recharges for, as well as the visuals related to doing so.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
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
    /// Immediately begin recharging after charge completes. If false, recharge must be activated through other means
    /// </summary>
    [DataField]
    public bool ImmediateRecharge = true;

    /// <summary>
    /// Whether charging and recharging may occur
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    [DataField]
    public string? ChargingString = "teleporter-examine-charging";
    [DataField]
    public string? RechargingString = "teleporter-examine-recharging";
    [DataField]
    public string? PausedString = "teleporter-examine-recharging-paused";

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
