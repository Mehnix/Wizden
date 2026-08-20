using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used a XAT that activates when an entity uses a device with access
/// AccessReaderComponent must also be attached to the trigger and holds all the datafields, this just facilitates it triggering the artifact
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATAccessSystem))]
public sealed partial class XATAccessComponent : Component
{
    [DataField]
    public SoundSpecifier? AccessSound;

    [DataField]
    public SoundSpecifier? DeniedSound;
}
