using Robust.Shared.GameStates;

namespace Content.Shared.Emoting.EmitEmote;

/// <summary>
/// Emits emote when used in world
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmitEmoteOnActivateComponent : BaseEmitEmoteComponent
{
    /// <summary>
    ///     Whether or not to mark an interaction as handled after emoting. Useful if this component is
    ///     used to emote for some other component with on-use functionality
    /// </summary>
    /// <remarks>
    ///     If false, you should be confident that the interaction will also be handled by some other system, as
    ///     otherwise this might enable emote spamming, as use-delays are only initiated if the interaction was
    ///     handled.
    /// </remarks>
    [DataField]
    public bool Handle = true;
}
