using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Telescience.Components;

/// <summary>
/// Tracker for a recharging Telecharge
/// <seealso cref="TelechargeComponent"/>
/// </summary>
/// <remarks>
/// Gee Bill! Is that a THIRD timer-related component?
/// Sure is Bob. Telecharges specifically won't scan if they detect another recharging telecharge, that way you can't stack a load at once to drain a bajillion science and spam the radio channel!
/// That makes no se- It's game balance Bob now shut your mouth they need to be unique because you wouldn't want this effect triggering if a telecharge scans a recharging teleframe.
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class TelechargeRechargingComponent : Component
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
}
