using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Random;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that activates when a reaction occurs on the artifact.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATReactiveSpecificSystem)), AutoGenerateComponentState]
public sealed partial class XATReactiveSpecificComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ReactionMethod> ReactionMethods = new() { ReactionMethod.Touch };

    /// <summary>
    /// Single random reagent that is required in quantity <see cref="MinQuantity"/> to activate trigger
    /// If this specific reagent is present in required amount - activation will be triggered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<ReagentPrototype>> Reagents = new();

    /// <summary>
    /// Reagents that are required in quantity <see cref="MinQuantity"/> to activate trigger.
    /// If any of them are present in required amount - activation will be triggered.
    /// If this is left empty, it will be filled by the RandomSolutionFill
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>? Reagent;

    /// <summary>
    /// Min amount of reagent to trigger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MinQuantity = 5f;

    [DataField, AutoNetworkedField]
    public bool Examinable = true;
}
