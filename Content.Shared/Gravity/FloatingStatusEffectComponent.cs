using Robust.Shared.GameStates;

namespace Content.Shared.Gravity;

/// <summary>
/// This is used for a status effect that lets you ignore gravity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class FloatingStatusEffectComponent : Component;
