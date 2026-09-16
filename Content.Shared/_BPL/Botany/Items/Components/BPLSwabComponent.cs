using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._BPL.Botany.Items.Components
{
    /// <summary>
    /// Misc stuff to add to swabs added in a way that doesn't touch upstream code.
    /// </summary>
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class BPLSwabComponent : Component
    {
        /// <summary>
        /// Name of a base plant prototype for the stored pollen snapshot.
        /// </summary>
        [DataField, AutoNetworkedField]
        public bool Cleanable = false;

        [DataField, AutoNetworkedField]
        public SoundSpecifier? SwabSound = new SoundPathSpecifier("/Audio/Effects/Footsteps/grass2.ogg", AudioParams.Default.WithVolume(-4f));

        [DataField, AutoNetworkedField]
        public SoundSpecifier? CleanSound = new SoundPathSpecifier("/Audio/Effects/unwrap.ogg");
    }
}
