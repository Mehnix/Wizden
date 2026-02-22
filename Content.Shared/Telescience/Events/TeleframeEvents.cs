using Content.Shared.Telescience.Components;
using Robust.Shared.Map;


namespace Content.Shared.Telescience.Events;

[ByRefEvent]
public readonly record struct TeleframeConsoleToFrameRelayEvent<T>(Entity<TeleframeConsoleComponent> Console, T Args);

[ByRefEvent]
public readonly record struct TeleframeToConsoleRelayEvent<T>(Entity<TeleframeComponent> Frame, T Args);

///<summary>
///Event raised on the teleframe and any upgrade modules it begins charging
/// </summary>
[ByRefEvent]
public struct TeleframeStartChargeEvent(EntityUid teleframe, MapCoordinates target)
{
    public readonly EntityUid Teleframe = teleframe;
    public readonly MapCoordinates Target = target;
    public bool Cancelled = false;
}

/// <summary>
///Event raised on the teleframe when teleportation fails
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeTeleportFailedEvent(string Reason);

/// <summary>
/// Event raised on the teleframe when experiencing a teleport incident
/// </summary>
[ByRefEvent]
public record struct TeleframeIncidentEvent(float Score, float IncidentMult);

/// <summary>
/// Event raised on entities that are to experience a teleport incident
/// </summary>
[ByRefEvent]
public record struct TeleframeUserIncidentEvent(float Score, float IncidentMult);

/// <summary>
///Event raised on the teleframe just after teleporting an entity
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeTeleportedEvent(EntityUid Teleported, MapCoordinates To, MapCoordinates From);

/// <summary>
/// Event raised on the teleframe just after it has finished teleporting everything it can
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeTeleportedAllEvent(List<EntityUid> Teleported, MapCoordinates To, MapCoordinates From);

/// <summary>
///Event raised just after the user of a teleframe has teleported
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeUserTeleportedEvent(EntityUid Teleframe, MapCoordinates To, MapCoordinates From);

/// <summary>
/// Event raised on the teleframe when it has finished recharging and may be used again
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeReadyEvent(EntityUid Teleframe);

/// <summary>
/// Event confirming charging has begun and that the charging component is present
/// </summary>
[ByRefEvent]
public readonly record struct ChargingEvent();

/// <summary>
/// Event confirming recharging has begun and that the recharging component is present
/// </summary>
[ByRefEvent]
public readonly record struct RechargingEvent();
