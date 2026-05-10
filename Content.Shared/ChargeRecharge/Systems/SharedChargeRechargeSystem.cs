using Content.Shared.ChargeRecharge.Components;
using Content.Shared.ChargeRecharge.Events;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared.ChargeRecharge.Systems;

public abstract partial class SharedChargeRechargeSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChargingComponent, ExaminedEvent>(OnChargeExamined);
        SubscribeLocalEvent<RechargingComponent, ExaminedEvent>(OnRechargeExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //search for entities with the ChargingComponent and check if they've reached the end of their timer.
        var queryCharge = EntityQueryEnumerator<ChargingComponent>();
        while (queryCharge.MoveNext(out var uid, out var charge))
        {
            if (Timing.CurTime < charge.EndTime) //end if charge time runs
                continue;

            EndCharge(uid);
        }

        //search for entities with the RechargingComponent and check if they've reached the end of their timer.
        var queryRecharge = EntityQueryEnumerator<RechargingComponent>();
        while (queryRecharge.MoveNext(out var uid, out var recharge))
        {
            if (recharge.Pause || Timing.CurTime < recharge.EndTime) //end if recharge time runs out, unless we're currently paused
                continue;

            EndRecharge(uid);
        }
    }

    #region Charge/Recharge
    /// <summary>
    /// Initiate charging. Set charge time manually here or using <see cref="ChargeRechargeComponent"/>
    /// </summary>
    public void StartCharge(EntityUid uid, TimeSpan? chargeTime = null)
    {
        if (HasComp<RechargingComponent>(uid) || TryComp<ChargeRechargeComponent>(uid, out var charReComp) && charReComp.IsEnabled == false)
        {
            EndCharge(uid, false, "charge-fail-halted");
            return;
        }

        if (chargeTime != null || charReComp != null && charReComp.ChargeDuration != null) //requires either component or provided charge time
        { //can allow this to be requested even if charge already present as it'll just extend the charge time
            var chargeComp = EnsureComp<ChargingComponent>(uid); //create component and set up its duration and end time
            chargeComp.Duration = chargeTime ?? charReComp!.ChargeDuration!.Value;
            chargeComp.EndTime = chargeTime ?? charReComp!.ChargeDuration!.Value + Timing.CurTime;

            Dirty(uid, chargeComp);

            var ev = new StartChargingEvent();
            RaiseLocalEvent(uid, ref ev);

            UpdateAppearance(uid);
        }
    }

    /// <summary>
    /// Finish charging. Charging may have failed, and if so a reason should be provided
    /// </summary>
    public void EndCharge(EntityUid uid, bool success = true, string? failReason = null)
    {
        RemComp<ChargingComponent>(uid); //stop charging

        var ev = new EndChargingEvent(success, failReason);
        RaiseLocalEvent(uid, ref ev);

        UpdateAppearance(uid);

    }

    /// <summary>
    /// Initiate recharging. Set recharge time manually here or using <see cref="ChargeRechargeComponent"/>
    /// </summary>
    public void StartRecharge(EntityUid uid, TimeSpan? rechargeTime = null)
    {
        if (HasComp<ChargingComponent>(uid)) //end charge if recharge starts
        {
            EndCharge(uid, false, "charge-fail-halted");
            return;
        }

        if (TryComp<ChargeRechargeComponent>(uid, out var charReComp) && charReComp.RechargeDuration != null || rechargeTime != null) //can allow this to be requested even if charge already present as it'll just extend the recharge time
        {
            var rechargeComp = EnsureComp<RechargingComponent>(uid); //create component and set up its duration and end time
            rechargeComp.Duration = rechargeTime ?? charReComp!.RechargeDuration!.Value;
            rechargeComp.EndTime = rechargeTime ?? charReComp!.RechargeDuration!.Value + Timing.CurTime;
            Dirty(uid, rechargeComp);

            var ev = new StartRechargingEvent();
            RaiseLocalEvent(uid, ref ev);

            UpdateAppearance(uid);
        }

        if (charReComp != null && charReComp.IsEnabled == false) // if we are disabled, immediately pause the recharging
            PauseRecharge(uid);
    }

    /// <summary>
    /// Ends recharging. Usually meaning a system returns to idle
    /// </summary>
    /// <param name="uid"></param>
    public void EndRecharge(EntityUid uid)
    {
        RemComp<RechargingComponent>(uid); //stop recharging

        var ev = new EndRechargingEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    /// <summary>
    /// Pause recharging, this isn't a failiure state and recharging can be picked up again in the future.
    /// </summary>
    public void PauseRecharge(EntityUid uid)
    {
        if (TryComp<RechargingComponent>(uid, out var rechargeComp) && rechargeComp.Pause == false)
        {
            rechargeComp.Pause = true;
            rechargeComp.PauseTime = rechargeComp.EndTime - Timing.CurTime;
            Dirty(uid, rechargeComp);

            var ev = new PauseRechargingEvent();
            RaiseLocalEvent(uid, ref ev);

            UpdateAppearance(uid);
        }
    }

    /// <summary>
    /// Resume recharging
    /// </summary>
    public void ResumeRecharge(EntityUid uid)
    {
        if (TryComp<RechargingComponent>(uid, out var rechargeComp) && rechargeComp.Pause == true)
        {
            rechargeComp.Pause = false;
            rechargeComp.EndTime = Timing.CurTime + rechargeComp.PauseTime;
            rechargeComp.PauseTime = TimeSpan.FromSeconds(0);
            Dirty(uid, rechargeComp);

            var ev = new ResumeRechargingEvent();
            RaiseLocalEvent(uid, ref ev);

            UpdateAppearance(uid);
        }
    }

    /// <summary>
    /// Disabling function, fails charging, pauses recharging
    /// </summary>

    public void DisableCharge(EntityUid uid, string failReason)
    {
        if (TryComp<ChargeRechargeComponent>(uid, out var charReComp))
        {
            charReComp.IsEnabled = false;
            Dirty(uid, charReComp);
        }

        if (HasComp<ChargingComponent>(uid))
            EndCharge(uid, false, failReason);

        if (HasComp<RechargingComponent>(uid))
            PauseRecharge(uid);

        UpdateAppearance(uid);
    }

    /// <summary>
    /// Enabling function, resumes recharging
    /// </summary>
    public void EnableCharge(EntityUid uid)
    {
        if (TryComp<ChargeRechargeComponent>(uid, out var charReComp))
        {
            charReComp.IsEnabled = true;
            Dirty(uid, charReComp);
        }

        if (HasComp<RechargingComponent>(uid))
            ResumeRecharge(uid);
        else
            UpdateAppearance(uid);
    }

    #endregion
    #region Examine
    /// <summary>
    /// Display that we are charging
    /// </summary>
    public void OnChargeExamined(Entity<ChargingComponent> ent, ref ExaminedEvent args)
    {
        var examine = "examine-charging";
        if (TryComp<ChargeRechargeComponent>(ent, out var charReComp) && charReComp.ChargingString != null)
            examine = charReComp.ChargingString;

        args.PushMarkup(Loc.GetString(examine));

    }

    /// <summary>
    /// Display that we are recharging
    /// </summary>
    public void OnRechargeExamined(Entity<RechargingComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Pause == false)
        {
            var examine = "examine-recharging";
            if (TryComp<ChargeRechargeComponent>(ent, out var charReComp) && charReComp.RechargingString != null)
                examine = charReComp.RechargingString;
            args.PushMarkup(Loc.GetString(examine));
        }
        else
        {
            var examine = "examine-recharging-paused";
            if (TryComp<ChargeRechargeComponent>(ent, out var charReComp) && charReComp.PausedString != null)
                examine = charReComp.PausedString;
            args.PushMarkup(Loc.GetString(examine));
        }
    }
    #endregion
    #region Helpers
    /// <summary>
    /// Visualiser updating function, called whenever a change in state occurs.
    /// ChargeRechargeComponent required for handling being enabled/disabled
    /// </summary>
    public void UpdateAppearance(EntityUid uid)
    {
        ChargeRechargeVisualState state;
        if (!TryComp<ChargeRechargeComponent>(uid, out var charReComp) || charReComp.IsEnabled == true) //check if powered, set to on/off state
        {
            state = ChargeRechargeVisualState.On;
            if (HasComp<ChargingComponent>(uid)) //override if charging
            {
                state = ChargeRechargeVisualState.Charging;
            }

            if (HasComp<RechargingComponent>(uid)) //override if recharging, this state takes highest priority
            {
                state = ChargeRechargeVisualState.Recharging;
            }
        }
        else
        {
            state = ChargeRechargeVisualState.Off;
        }

        _appearance.SetData(uid, ChargeRechargeVisuals.VisualState, state); //Dirties itself
    }

    /// <summary>
    /// Helper function, check if we're paused.
    /// </summary>
    public bool? IsRechargePaused(EntityUid uid)
    {
        if (!TryComp<RechargingComponent>(uid, out var rechargeComp))
            return null;

        return rechargeComp.Pause;
    }

    /// <summary>
    /// Helper function, check if we're enabled
    /// </summary>
    public bool? IsChargingEnabled(EntityUid uid)
    {
        if (!TryComp<ChargeRechargeComponent>(uid, out var charReComp))
            return null;

        return charReComp.IsEnabled;
    }

    #endregion

}
