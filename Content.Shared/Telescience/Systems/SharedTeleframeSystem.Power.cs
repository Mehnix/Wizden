using Content.Shared.Telescience.Components;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    protected virtual void InitializePower()
    {
        base.Initialize();
    }

    /// <summary>
    /// turn off teleframe, interrupt charge and fail it, pause recharge and update its pause time (if it wasn't already)
    /// </summary>
    protected void PowerOff(Entity<TeleframeComponent> ent)
    {
        ent.Comp.IsPowered = false;

        if (TryComp<TeleframeChargingComponent>(ent, out var chargeComp)) // power off during charge is a failed teleport, so prepare for fail
        {   //we can't punish non brownout powerloss as power increase isn't instant
            chargeComp.TeleportSuccess = false;
            chargeComp.FailReason = Loc.GetString("teleport-fail-power");
            EndTeleportCharge((ent.Owner, ent.Comp, chargeComp));
            Dirty(ent.Owner, chargeComp);
            return; //EndTeleportCharge already updates appearance
        }

        if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp) && rechargeComp.Pause == false) //pause recharge and update its pause time
        {
            rechargeComp.Pause = true;
            rechargeComp.PauseTime = rechargeComp.EndTime - Timing.CurTime;
            Dirty(ent.Owner, rechargeComp);
        }

        UpdateAppearance(ent);
        Dirty(ent);
    }

    /// <summary>
    /// power on teleframe, unpause recharge if it was there.
    /// </summary>
    protected void PowerOn(Entity<TeleframeComponent> ent)
    {
        ent.Comp.IsPowered = true;

        if (HasComp<TeleframeChargingComponent>(ent)) //full power while charging? All good so just end here.
            return;

        if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp)) //full power while recharging? Enable if we were previously recharging
        {
            if (rechargeComp.Pause == true) //if we were paused, restart charging process by adding on pause time to get a new end time for recharge completion
            {
                rechargeComp.Pause = false;
                rechargeComp.EndTime = Timing.CurTime + rechargeComp.PauseTime;
                rechargeComp.PauseTime = TimeSpan.FromSeconds(0);
                Dirty(ent.Owner, rechargeComp);
            }
            else //if not paused, all good so just end here.
            {
                return;
            }
        }

        UpdateAppearance(ent);
        Dirty(ent);
    }
}
