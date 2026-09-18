using Robust.Shared.GameStates;
using Content.Shared._BPL.Botany.Items.Systems;

namespace Content.Shared._BPL.Botany.Items.Components;

[RegisterComponent, Access(typeof(GrowLightSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrowLightComponent : Component
{
    /// <summary>
    /// The number of tray cycles before a boost is given.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? BoostCount = 5;

    [DataField, AutoNetworkedField]
    public TimeSpan ApplyTime = TimeSpan.FromSeconds(5);
}

