using Content.Shared.EntityEffects;
using Content.Shared._BPL.Botany.Items.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._BPL.Botany.Items.Components;

[RegisterComponent, Access(typeof(PlantMutatorSystem)), NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlantMutatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityEffect EntityEffect { get; set; }
}
