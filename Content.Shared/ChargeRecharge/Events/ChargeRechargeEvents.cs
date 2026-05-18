
namespace Content.Shared.ChargeRecharge.Events;

/// <summary>
/// Event to initiate charging
/// </summary>
[ByRefEvent]
public readonly record struct StartChargingEvent(NetEntity? User = null);

/// <summary>
/// Event calling for an end to charging, confirming if it was successful or not
/// </summary>
[ByRefEvent]
public readonly record struct EndChargingEvent(bool Success = true, string? FailReason = null, NetEntity? User = null);

/// <summary>
/// Event to initiate recharging
/// </summary>
[ByRefEvent]
public readonly record struct StartRechargingEvent(NetEntity? User = null);

/// <summary>
/// Event calling for an end to recharging
/// </summary>
[ByRefEvent]
public readonly record struct EndRechargingEvent(NetEntity? User = null);

/// <summary>
/// Event requesting recharging to be paused
/// </summary>
[ByRefEvent]
public readonly record struct PauseRechargingEvent(NetEntity? User = null);

/// <summary>
/// Event requesting recharging to be resumed
/// </summary>
[ByRefEvent]
public readonly record struct ResumeRechargingEvent(NetEntity? User = null);
