
namespace Content.Shared.Construction.Components;

public sealed partial class QuickAnchorComponent : Component
{
    /// <summary>
    /// Text used for the Alt Verb
    /// </summary>
    [DataField]
    public string AnchorText = "anchor-text";

    /// <summary>
    /// Text used for the Alt Verb
    /// </summary>
    [DataField]
    public string UnanchorText = "unanchor-text";

    /// <summary>
    /// Whether the device can be anchored, unanchored, or both using the quick anchoring method.
    /// </summary>
    [DataField]
    public AnchorableFlags Flags = AnchorableFlags.Anchorable | AnchorableFlags.Unanchorable;
}
