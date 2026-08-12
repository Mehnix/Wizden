using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;

namespace Content.Shared.Emoting.EmitEmote;

public abstract partial class SharedEmitEmoteSystem : EntitySystem
{
    [Dependency] private SharedChatSystem _chat = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EmitEmoteOnUseComponent, UseInHandEvent>(OnEmitEmoteOnUseInHand);
        SubscribeLocalEvent<EmitEmoteOnActivateComponent, ActivateInWorldEvent>(OnEmitEmoteOnActivateInWorld);
    }

    protected void TryEmitEmote(EntityUid uid, BaseEmitEmoteComponent comp, string? nameOverride = null)
    {
        Log.Debug("honk");
        if (comp.ShowInChat)
            _chat.TryEmoteWithChat(uid, comp.Emote, ChatTransmitRange.GhostRangeLimit, forceEmote: comp.Force, nameOverride: nameOverride);
        else
            _chat.TryEmoteWithChat(uid, comp.Emote, ChatTransmitRange.HideChat, forceEmote: comp.Force, nameOverride: nameOverride);
    }

    private void OnEmitEmoteOnUseInHand(EntityUid uid, EmitEmoteOnUseComponent comp, UseInHandEvent args)
    {
        Log.Debug("start");
        // as we're holding the item, claim ownership over it's emotes.
        var name = Loc.GetString("emit-emote-owner", ("owner", Identity.Name(args.User, EntityManager)), ("entity", Identity.Name(uid, EntityManager)));
        TryEmitEmote(args.User, comp, name);
    }

    private void OnEmitEmoteOnActivateInWorld(EntityUid uid, EmitEmoteOnActivateComponent comp, ActivateInWorldEvent args)
    {
        Log.Debug("start");
        TryEmitEmote(args.User, comp);
    }
}
