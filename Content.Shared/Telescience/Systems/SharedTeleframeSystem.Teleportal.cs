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
    }

    /// <summary>
    /// EMP's kill teleportals because the pulse travels through the portal and zaps the teleframe or something like that
    /// </summary>
    private void OnEmpPulseTeleportal(Entity<TeleframeTeleportalComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Teleframe is not { } teleEnt)
            return;

        ChargeRecharge.EndCharge(teleEnt, false, "teleport-fail-emp");
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

        if (ent.Comp.Complete == false) //if teleportation is complete, not an failiure that this dies
            ChargeRecharge.EndCharge(teleEnt, false, "teleport-fail-nolink");
    }
}
