using Robust.Shared.Timing;
using Content.Shared.ChargeRecharge.Components;
using Content.Shared.ChargeRecharge.Events;
using Content.Shared.Examine;

namespace Content.Shared.ChargeRecharge.Systems;

public abstract partial class SharedChargeRechargeSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChargeRechargeComponent, ExaminedEvent>(OnExamined);
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //search for entities with the ChargingComponent and check if they've reached the end of their timer.
        var queryCharge = EntityQueryEnumerator<ChargingComponent, ChargeRechargeComponent>();
        while (queryCharge.MoveNext(out var uid, out var charge, out var chargeRecharge))
        {
            if (Timing.CurTime < charge.EndTime) //end if charge time runs
                continue;

            EndCharge(uid);
        }

        //search for entities with the RechargingComponent and check if they've reached the end of their timer.
        var queryRecharge = EntityQueryEnumerator<RechargingComponent, ChargeRechargeComponent>();
        while (queryRecharge.MoveNext(out var uid, out var recharge, out var chargeRecharge))
        {
            if (recharge.Pause || Timing.CurTime < recharge.EndTime) //end if recharge time runs out, unless we're currently paused
                continue;

            EndRecharge(uid);
        }
    }

    public void StartCharge(EntityUid uid)
    {
        if (TryComp<ChargeRechargeComponent>(uid, out var charReComp)) //can allow this to be requested even if charge already present as it'll just extend the charge time
        {
            if (charReComp.ChargeDuration == null)
                return;

            var chargeComp = EnsureComp<ChargingComponent>(uid); //create component and set up its duration and end time
            chargeComp.Duration = charReComp.ChargeDuration!.Value;
            chargeComp.EndTime = charReComp.ChargeDuration!.Value + Timing.CurTime;
            Dirty(uid, chargeComp);

            _appearance.SetData(uid, ChargeRechargeVisuals.VisualState, ChargeRechargeVisualState.Charging); //Dirties itself
            Log.Debug("Charge");

            var ev = new StartChargingEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }
    public void EndCharge(EntityUid uid, bool success = true, string? failReason = null)
    {
        if (TryComp<ChargeRechargeComponent>(uid, out var charReComp) && HasComp<ChargingComponent>(uid))
        {
            RemComp<ChargingComponent>(uid); //stop charging
            _appearance.SetData(uid, ChargeRechargeVisuals.VisualState, ChargeRechargeVisualState.On); //Dirties itself

            Log.Debug("End Charge");
            var ev = new EndChargingEvent(success, failReason);
            RaiseLocalEvent(uid, ref ev);
        }
    }
    public void StartRecharge(EntityUid uid)
    {
        if (TryComp<ChargeRechargeComponent>(uid, out var charReComp)) //can allow this to be requested even if charge already present as it'll just extend the recharge time
        {
            if (charReComp.RechargeDuration == null)
                return;

            var rechargeComp = EnsureComp<RechargingComponent>(uid); //create component and set up its duration and end time
            rechargeComp.Duration = charReComp.RechargeDuration!.Value;
            rechargeComp.EndTime = charReComp.RechargeDuration!.Value + Timing.CurTime;
            Dirty(uid, rechargeComp);
            _appearance.SetData(uid, ChargeRechargeVisuals.VisualState, ChargeRechargeVisualState.Recharging); //Dirties itself

            Log.Debug("Recharge");
            var ev = new StartRechargingEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }
    public void EndRecharge(EntityUid uid)
    {
        if (TryComp<ChargeRechargeComponent>(uid, out var charReComp) && HasComp<RechargingComponent>(uid))
        {
            RemCompDeferred<RechargingComponent>(uid); //stop recharging
            _appearance.SetData(uid, ChargeRechargeVisuals.VisualState, ChargeRechargeVisualState.On); //Dirties itself

            Log.Debug("End Recharge");
            var ev = new EndRechargingEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }
    public void PauseRecharge(EntityUid uid)
    {
        if (HasComp<ChargeRechargeComponent>(uid) && TryComp<RechargingComponent>(uid, out var rechargeComp) && rechargeComp.Pause == false)
        {
            rechargeComp.Pause = true;
            rechargeComp.PauseTime = rechargeComp.EndTime - Timing.CurTime;
            Dirty(uid, rechargeComp);
            Log.Debug($"pause {rechargeComp.PauseTime}");
            var ev = new PauseRechargingEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }
    public void ResumeRecharge(EntityUid uid)
    {
        if (HasComp<ChargeRechargeComponent>(uid) && TryComp<RechargingComponent>(uid, out var rechargeComp) && rechargeComp.Pause == true)
        {
            rechargeComp.Pause = false;
            rechargeComp.EndTime = Timing.CurTime + rechargeComp.PauseTime;
            rechargeComp.PauseTime = TimeSpan.FromSeconds(0);
            _appearance.SetData(uid, ChargeRechargeVisuals.VisualState, ChargeRechargeVisualState.Recharging); //Dirties itself
            Dirty(uid, rechargeComp);
            Log.Debug($"unpause {rechargeComp.EndTime - Timing.CurTime}");
            var ev = new ResumeRechargingEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    public void DisableCharge(EntityUid uid, string failReason)
    {
        if (!HasComp<ChargeRechargeComponent>(uid))
            return;

        Log.Debug("Disabled");

        if (HasComp<ChargingComponent>(uid))
        {
            EndCharge(uid, false, failReason);
        }

        if (HasComp<RechargingComponent>(uid))
        {
            PauseRecharge(uid);
        }

        _appearance.SetData(uid, ChargeRechargeVisuals.VisualState, ChargeRechargeVisualState.Off); //Dirties itself
    }

    public void EnableCharge(EntityUid uid)
    {
        if (!HasComp<ChargeRechargeComponent>(uid))
            return;

        Log.Debug("Enabled");

        if (HasComp<RechargingComponent>(uid))
        {
            ResumeRecharge(uid);
        }

        _appearance.SetData(uid, ChargeRechargeVisuals.VisualState, ChargeRechargeVisualState.On); //Dirties itself

    }

    public void UpdateAppearance(Entity<ChargeRechargeComponent> ent)
    {
        ChargeRechargeVisualState state;
        if (ent.Comp.Enabled == true) //check if powered, set to on state
        {
            state = ChargeRechargeVisualState.On;
            if (HasComp<ChargingComponent>(ent)) //override if charging
            {
                state = ChargeRechargeVisualState.Charging;
            }

            if (HasComp<RechargingComponent>(ent)) //override if recharging, this state takes highest priority
            {
                state = ChargeRechargeVisualState.Recharging;
            }
        }
        else
        {
            state = ChargeRechargeVisualState.Off;
        }

        _appearance.SetData(ent.Owner, ChargeRechargeVisuals.VisualState, state); //Dirties itself
    }

    public void OnExamined(Entity<ChargeRechargeComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<ChargingComponent>(ent) && ent.Comp.ChargingString != null)
        {
            args.PushMarkup(Loc.GetString(ent.Comp.ChargingString));
        }

        if (TryComp<RechargingComponent>(ent, out var rechargeComp) && ent.Comp.RechargingString != null && ent.Comp.PausedString != null)
        {
            if (rechargeComp.Pause == false)
                args.PushMarkup(Loc.GetString(ent.Comp.RechargingString));
            else
                args.PushMarkup(Loc.GetString(ent.Comp.PausedString));
        }
    }

    public bool? IsRechargePaused(EntityUid uid)
    {
        if (!TryComp<RechargingComponent>(uid, out var rechargeComp))
            return null;

        return rechargeComp.Pause;
    }

    public bool? IsChargingEnabled(EntityUid uid)
    {
        if (!TryComp<ChargeRechargeComponent>(uid, out var charReComp))
            return null;

        return charReComp.Enabled;
    }

}
