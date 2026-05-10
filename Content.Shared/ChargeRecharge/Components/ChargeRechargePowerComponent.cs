using Robust.Shared.GameStates;
using Content.Shared.ChargeRecharge.Systems;

namespace Content.Shared.ChargeRecharge.Components;

/// <summary>
/// Component allowing varying draw for PowerConsumer devices.
/// Does nothing without <see cref="PowerConsumerComponent"/>
/// Doesn't actually need <see cref="ChargeRechargeComponent"/> but relies on the events <see cref="SharedChargeRechargeSystem"/> creates
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChargeRechargePowerComponent : Component
{
    /// <summary>
    /// Power draw when actively charging/recharging
    /// </summary>
    [DataField]
    public int PowerUseActive = 20000;

    /// <summary>
    /// Power draw when idle
    /// </summary>
    [DataField]
    public int PowerUseIdle = 1000;

    /// <summary>
    /// Whether the structure is powered
    /// </summary>
    [DataField]
    public bool IsPowered = true;

    /// <summary>
    /// Whether the structure is powered
    /// </summary>
    [DataField]
    public string FailReason = "teleport-fail-power";

}
