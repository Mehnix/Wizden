using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Systems;
namespace Content.Client.Telescience;

/// <summary>
/// <inheritdoc cref="SharedTeleframeSystem"/>
/// </summary>
public sealed partial class TeleframeSystem : SharedTeleframeSystem
{
    public override (bool, string?) CheckTeleportation(Entity<TeleframeComponent> ent)
    {
        return (true, null);
    }
}

