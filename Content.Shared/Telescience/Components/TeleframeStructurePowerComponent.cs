using Robust.Shared.GameStates;

namespace Content.Shared.Telescience.Components;

/// <summary>
/// Component representing the power draw of teleframe structures.
/// Does nothing without <see cref="TeleframeComponent"/> or <see cref="PowerConsumerComponent"/>
/// Teleframe structures won't draw power without this and so always be on
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TeleframeStructurePowerComponent : Component
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
    /// Whether the teleframe is powered
    /// </summary>
    [DataField]
    public bool IsPowered = true;

}
