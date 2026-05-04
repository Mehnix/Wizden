using Content.Shared.Construction.Components;
using Robust.Shared.Audio.Systems;
using Content.Shared.Verbs;

namespace Content.Shared.Construction.EntitySystems;

public sealed partial class QuickAnchorSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AnchorableSystem _anchor = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<QuickAnchorComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    /// <summary>
    /// When the player right clicks this entity, display an additional verb that lets them anchor/unanchor it immediately without a tool
    /// Don't display it if we're unable to anchor/unanchor
    /// </summary>
    private void OnGetAltVerbs(Entity<QuickAnchorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands is null)
            return;

        var xform = Transform(ent);
        var user = args.User;
        args.Verbs.Add(new()
        {
            Act = () =>
            {
                ToggleAnchor(ent, xform, user);
            },
            Text = xform.Anchored ? Loc.GetString(ent.Comp.UnanchorText) : Loc.GetString(ent.Comp.AnchorText),
            Disabled = !CanAnchor(ent, xform), //check anchorability
            TextStyleClass = "InteractionVerb",
        });
    }

    /// <summary>
    /// Check anchor/unanchor flags, whether we're on a grid, and whether we're colliding
    /// </summary>
    /// <returns>true if we can anchor, false if we can't</returns>
    private bool CanAnchor(Entity<QuickAnchorComponent> ent, TransformComponent xform)
    {
        if (!xform.Anchored && (ent.Comp.Flags & AnchorableFlags.Anchorable) == 0x0) //if we're not anchored and don't have the anchorable flag, can't anchor
            return false;

        if (xform.Anchored && (ent.Comp.Flags & AnchorableFlags.Unanchorable) == 0x0) //if we're anchored and don't have the unanchorable flag, can't unanchor
            return false;

        if (Transform(ent).GridUid == null) //must be on a grid
            return false;

        if (_anchor.CanAnchorAt(ent.Owner) == false) //must not be colliding with something.
            return false;

        return true;
    }

    /// <summary>
    /// swap anchor/unanchor states
    /// </summary>
    private void ToggleAnchor(Entity<QuickAnchorComponent> ent, TransformComponent xform, EntityUid user)
    {
        if (!CanAnchor(ent, xform)) //test again because time passed between the verb button being shown at the player pressing it
            return;

        if (xform.Anchored)
            _transform.Unanchor(ent, xform);
        else
            _transform.AnchorEntity(ent, xform);

        _audio.PlayPredicted(ent.Comp.AnchorSound, ent.Owner, user);
    }

}
