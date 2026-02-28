namespace Content.Shared.Telescience.Events;
///<summary>
///Event raised by a telecharge on whitelisted entities it is in scanning range of.
/// </summary>
[ByRefEvent]
public readonly record struct TelechargeScanEvent(EntityUid Telecharge, float Distance, float IncidentMult);

///<summary>
///Event raised by a telecharge on whitelisted entities it is in scanning range of if it suffered a teleport incident.
/// </summary>
[ByRefEvent]
public readonly record struct TelechargeScanIncidentEvent(float Score, float IncidentMult);

///<summary>
/// Event raised on a telecharge by other entities to give the telecharge science points
/// </summary>
[ByRefEvent]
public readonly record struct TelechargeAddScienceEvent(int Science, float Status);

