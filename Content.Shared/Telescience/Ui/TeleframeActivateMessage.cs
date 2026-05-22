using Robust.Shared.Serialization;
using Robust.Shared.Map;

namespace Content.Shared.Telescience.Ui;

/// <summary>
/// Sends message to request the console to initiate the teleframe
/// EntityCoordinates are not Serializable so we make do
/// </summary>
[Serializable, NetSerializable]
public sealed class TeleframeActivateMessage(MapCoordinates coords, string name, TeleframeActivationMode mode, NetEntity targetEnt, NetEntity? user) : BoundUserInterfaceMessage
{
    public MapCoordinates Coords = coords; //coordinates of target
    public string Name = name;  // name of target, may be seperate from entity name
    public TeleframeActivationMode Mode = mode; //whether we are sending to target (true) or receiving from target (false)
    public NetEntity TargetEnt = targetEnt; // entity associated with target if there is one
    public NetEntity? User = user; //user of the console

}
