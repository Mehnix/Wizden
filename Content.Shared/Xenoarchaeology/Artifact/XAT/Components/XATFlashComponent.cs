using Robust.Shared.GameStates;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used a XAT that activates when an entity fulfilling the given whitelist is nearby the artifact.
/// AccessReaderComponent must also be attached to the trigger and holds all the datafields, this just facilitates it triggering the artifact
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATFlashSystem))]
public sealed partial class XATFlashComponent : Component
{
}
