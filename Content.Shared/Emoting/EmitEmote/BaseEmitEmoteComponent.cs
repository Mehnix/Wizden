using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Emoting.EmitEmote;

/// <summary>
/// Base emote emitter which defines most of the data fields.
/// </summary>
public abstract partial class BaseEmitEmoteComponent : Component
{
    /// <summary>
    /// The emote the entity will perform.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<EmotePrototype> Emote;

    /// <summary>
    /// If the emote should be recorded in chat.
    /// Default false
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowInChat = false;

    /// <summary>
    /// If true, the entity will perform the emote even if they normally can't.
    /// Default true
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Force = true;
}
