using Content.Shared.Construction.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Construction.EntitySystems;

public sealed partial class QuickAnchorSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AnchorableSystem _anchor = default!;
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
        var xform = Transform(ent);
        args.Verbs.Add(new()
        {
            Act = () =>
            {
                ToggleAnchor(ent, xform);
            },
            Text = xform.Anchored ? Loc.GetString(ent.Comp.UnanchorText) : Loc.GetString(ent.Comp.AnchorText),
            Disabled = !CanAnchor(ent, xform) || !HasComp<HandsComponent>(args.User), //check anchorability, user must also have hands (no mothroaches unanchoring things!)
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

        return !(Transform(ent).GridUid == null || _anchor.CanAnchorAt(ent.Owner)); //must be on a grid, must not be colliding with something. Must have hands (no mothroaches unanchoring).
    }

    /// <summary>
    /// swap anchor/unanchor states
    /// </summary>
    private void ToggleAnchor(Entity<QuickAnchorComponent> ent, TransformComponent xform)
    {
        if (!CanAnchor(ent, xform)) //test again because time passed between the verb button being shown at the player pressing it
            return;

        if (xform.Anchored)
            _transform.Unanchor(ent, xform);
        else
            _transform.AnchorEntity(ent, xform);
    }

}
