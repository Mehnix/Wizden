
using Robust.Shared.Prototypes;
using Content.Shared.Antag;

namespace Content.Shared.Revolutionary.Components;

/// <summary>
/// This is a marker component that indicates that a flash can be used to convert someone into a revolutionary.
/// Viva la revolution!
/// </summary>
[RegisterComponent]
public sealed partial class RevolutionaryConverterComponent : Component
{
    /// <summary>
    /// Amount of funding to provide
    /// </summary>
    [DataField]
    public float Amount = 1000f;

    /// <summary>
    /// Bonus multipliers if the converted target belongs to a job contained within an antagspecifier
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AntagSpecifierPrototype>, float> Bonuses = new()
    {
        ["SecurityBlacklistedAntag"] = 2.5f,
        ["CommandBlacklistedAntag"] = 5f,
    };
}
