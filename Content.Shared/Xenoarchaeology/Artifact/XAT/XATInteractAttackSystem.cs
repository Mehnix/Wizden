using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Whitelist;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Robust.Shared.Physics.Events;


namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// System for xeno artifact trigger for when a projectile collides with this object, or a person attacks it
/// Damage doesn't matter here, only the act of colliding/attacking
/// </summary>
public sealed partial class XATInteractAttackSystem : BaseXATSystem<XATInteractAttackComponent>
{
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<StartCollideEvent>(OnStartCollide);
        XATSubscribeDirectEvent<AttackedEvent>(OnAttacked);
        XATSubscribeDirectEvent<HitScanReflectAttemptEvent>(OnHitscan);
    }

    /// <summary>
    /// Trigger the node if the entity used to attack matches the whitelist
    /// </summary>
    private void OnAttacked(Entity<XenoArtifactComponent> artifact, Entity<XATInteractAttackComponent, XenoArtifactNodeComponent> node, ref AttackedEvent args)
    {
        if (_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, args.Used))
            Trigger(artifact, node);
    }

    /// <summary>
    /// Trigger the node if the colliding entity matches the whitelist
    /// </summary>
    private void OnStartCollide(Entity<XenoArtifactComponent> artifact, Entity<XATInteractAttackComponent, XenoArtifactNodeComponent> node, ref StartCollideEvent args)
    {
        if (_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, args.OtherEntity))
            Trigger(artifact, node);
    }

    /// <summary>
    /// Trigger the node if the colliding entity matches the whitelist
    /// </summary>
    private void OnHitscan(Entity<XenoArtifactComponent> artifact, Entity<XATInteractAttackComponent, XenoArtifactNodeComponent> node, ref HitScanReflectAttemptEvent args)
    {
        if (!TryComp<BatteryAmmoProviderComponent>(args.SourceItem, out var batteryComp))
            return;

        if (_whitelistSystem.IsWhitelistPassOrNull(node.Comp1.Whitelist, batteryComp.Prototype))
            Trigger(artifact, node);
    }
}
