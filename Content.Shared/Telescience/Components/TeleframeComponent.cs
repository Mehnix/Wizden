using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Telescience.Components;

/// <summary>
/// A machine that is combined and linked to the <see cref="TeleframeConsoleComponent"/>
/// in order to teleport entities.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TeleframeComponent : Component
{
    /// <summary>
    /// Teleportal entities placed at the source and target. Everything valid around the "From" entity will be moved to the "To" entity on teleportation.
    /// The Datafield defines which entity is placed at the source during a send and a receive, the opposite entity is placed at the target.
    /// EG: When sending, "From" is placed at source, "To" at target.
    /// </summary>
    [DataField, ViewVariables]
    public Dictionary<TeleframeActivationMode, EntProtoId?> TeleportModeEffects = new()
    {
        { TeleframeActivationMode.Send, "TeleportFromEffect" },
        { TeleframeActivationMode.Receive, "TeleportToEffect" }
    };

    /// <summary>
    /// Effect produced when at both teleportals when teleportation begins.
    /// </summary>
    [DataField, ViewVariables]
    public List<EntProtoId>? TeleportBeginEffect = null;

    /// <summary>
    /// Effect produced at both teleportals when teleportation finishes
    /// </summary>
    [DataField, ViewVariables]
    public List<EntProtoId>? TeleportFinishEffect = null;

    /// <summary>
    /// Effect produced at the source if teleportation fails
    /// </summary>
    [DataField, ViewVariables]
    public List<EntProtoId>? TeleportFailEffect = null;

    /// <summary>
    /// Randomness of Teleportation arrival positions. Entities will be placed +/- of this value from exact target
    /// </summary>
    /// <remarks>Scattering won't check if the scattered position is inside a wall so keep this value low</remarks>
    [DataField, ViewVariables]
    public float TeleportScatterRange = 0.75f;

    /// <summary>
    /// Radius from centre of teleportation within which entities will be teleported
    /// Don't make this value too high as it becomes awkward
    /// </summary>
    [DataField, ViewVariables]
    public float TeleportRadius = 1.5f;

    /// <summary>
    /// Blacklisted Tags and Components that won't be teleported
    /// Amusing things that haven't been included: Observers
    /// </summary>
    [DataField, ViewVariables]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Allow teleportation inside walls, default false
    /// </summary>
    [DataField, ViewVariables]
    public bool AllowCollision = false;

    /// <summary>
    /// Allow Teleportation to places with no grid underneath, default null
    /// If left null, "From" teleportals can and "To" teleportals can't. Letting you teleport from space but not to it.
    /// </summary>
    [DataField, ViewVariables]
    public bool? AllowGridless = null;

    //##########################################

    /// <summary>
    /// The corresponding Teleframe Console entity this Teleframe is linked to.
    /// Can be null if not linked.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? LinkedConsole;

    /// <summary>
    /// Marker, is Teleframe ready to teleport again?
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public bool ReadyToTeleport = true;

    /// <summary>
    /// Stored information regarding the current teleporation cycle, cleared after it concludes
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public TeleframeActiveTeleportInfo? ActiveTeleportInfo;
}

[Serializable, NetSerializable]
public readonly record struct TeleframeActiveTeleportInfo(TeleframeActivationMode Mode, NetEntity To, NetEntity From, NetEntity? User);
