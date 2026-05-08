using Content.Shared.Telescience.Components;

namespace Content.Shared.Telescience.Events;

[ByRefEvent]
public readonly record struct TeleframeConsoleToFrameRelayEvent<T>(Entity<TeleframeConsoleComponent> Console, T Args);

[ByRefEvent]
public readonly record struct TeleframeToConsoleRelayEvent<T>(Entity<TeleframeComponent> Frame, T Args);

/// <summary>
/// Event raised on the teleframe and console when teleportation fails
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeTeleportFailedEvent(string Reason, TeleframeActiveTeleportInfo? TeleportInfo);

///<summary>
/// Event raised on the teleframe, console, and any upgrade modules when teleportation charging is successfully initiated
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeInitiatedEvent(EntityUid Teleframe, TeleframeActiveTeleportInfo TeleportInfo);

/// <summary>
/// Event raised on the teleframe and any upgrade modules just before it begins teleporting
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeTeleportBeginEvent(EntityUid Teleframe, TeleframeActiveTeleportInfo TeleportInfo);

/// <summary>
/// Event raised on an entity that has been teleported by a teleframe
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeUserTeleportedEvent(EntityUid Teleframe, TeleframeActiveTeleportInfo TeleportInfo);

/// <summary>
/// Event raised on the teleframe just after it has finished teleporting everything it can
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeTeleportedAllEvent(List<EntityUid> Teleported, TeleframeActiveTeleportInfo TeleportInfo);

/// <summary>
/// Event raised on the teleframe and console when experiencing a teleport incident
/// </summary>
[ByRefEvent]
public record struct TeleframeIncidentEvent(float Score, float IncidentMult);

/// <summary>
/// Event raised on entities that are to experience a teleport incident
/// </summary>
[ByRefEvent]
public record struct TeleframeUserIncidentEvent(float Score, float IncidentMult);

/// <summary>
/// Event raised on the teleframe and upgrades when it has finished recharging and may be used again
/// </summary>
[ByRefEvent]
public readonly record struct TeleframeReadyEvent(EntityUid Teleframe);
