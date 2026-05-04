using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ChargeRecharge.Components;

/// <summary>
/// Tracker for somoething recharging before an effect can occur again
/// <seealso cref="ChargeRechargeComponent"/>
/// </summary>

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class RechargingComponent : Component
{
    /// <summary>
    /// when charge will finish
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan EndTime;

    /// <summary>
    /// total charge time
    /// </summary>
    [DataField]
    public TimeSpan Duration;

    /// <summary>
    /// pause recharge, such as if there's a lack of power
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Pause = false;

    /// <summary>
    /// Time that still needs to count down after pause ends
    /// </summary>

    [DataField, AutoNetworkedField]
    public TimeSpan PauseTime;
}
