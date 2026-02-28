using Content.Shared.Telescience.Components;
using Content.Shared.Emp;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    protected virtual void InitializeTeleportal()
    {
        base.Initialize();
        SubscribeLocalEvent<TeleframeTeleportalComponent, EmpPulseEvent>(OnEmpPulseTeleportal);
        SubscribeLocalEvent<TeleframeTeleportalComponent, EntityTerminatingEvent>(OnDeletion);
        SubscribeLocalEvent<TeleframeComponent, EmpPulseEvent>(OnEmpPulseFrame);

    }

    /// <summary>
    /// EMP's kill teleportals because the pulse travels through the portal and zaps the teleframe or something like that
    /// </summary>
    private void OnEmpPulseTeleportal(Entity<TeleframeTeleportalComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Teleframe is not { } teleEnt)
            return;

        FailCharge(teleEnt);
    }

    /// <summary>
    /// If portals are somehow deleted, fail immediately
    /// EndTeleportCharge has another contingency that also does this
    /// </summary>
    private void OnDeletion(Entity<TeleframeTeleportalComponent> ent, ref EntityTerminatingEvent args)
    {
        PredictedSpawnNextToOrDrop(ent.Comp.TeleportalDestructionEffect, ent.Owner); //kaput!
        if (ent.Comp.Teleframe is not { } teleEnt)
            return;

        FailCharge(teleEnt);
    }

    /// <summary>
    /// If the frame is emp'd, fail immediately
    /// Portals should be emp'd at the same time but just covering all cases
    /// </summary>
    private void OnEmpPulseFrame(Entity<TeleframeComponent> ent, ref EmpPulseEvent args)
    {
        if (!TryComp<TeleframeChargingComponent>(ent, out var chargeComp))
            return;

        FailCharge(ent.Owner);
    }

    /// <summary>
    /// Kill charging
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="reason"></param>
    private void FailCharge(EntityUid uid, string reason = "teleport-fail-nolink")
    {
        if (!TryComp<TeleframeChargingComponent>(uid, out var chargeComp))
            return;

        chargeComp.FailReason = reason;
        chargeComp.TeleportSuccess = false; //fail immediately
        Dirty(uid, chargeComp);
    }
}
