using Content.Shared.ChargeRecharge.Components;
using Content.Shared.Examine;

namespace Content.Shared.ChargeRecharge.Systems;

public abstract partial class SharedChargeRechargePowerSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] protected readonly SharedChargeRechargeSystem ChargeRecharge = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChargeRechargePowerComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    /// turn off teleframe, interrupt charge and fail it, pause recharge and update its pause time (if it wasn't already)
    /// </summary>
    protected void PowerOff(Entity<ChargeRechargePowerComponent> ent)
    {
        ent.Comp.IsPowered = false;

        ChargeRecharge.DisableCharge(ent.Owner, ent.Comp.FailReason);

        if (_lights.TryGetLight(ent.Owner, out var light) && light.Enabled == true) //set light off whilst here if there is one
            _lights.SetEnabled(ent.Owner, false);

        Dirty(ent);
    }

    /// <summary>
    /// power on teleframe, unpause recharge if it was there.
    /// </summary>
    protected void PowerOn(Entity<ChargeRechargePowerComponent> ent)
    {
        ent.Comp.IsPowered = true;

        ChargeRecharge.EnableCharge(ent.Owner);

        if (_lights.TryGetLight(ent.Owner, out var light) && light.Enabled == false) //set light on whilst here if there is one
            _lights.SetEnabled(ent.Owner, true); //dirties itself

        Dirty(ent);
    }

    public void OnExamined(Entity<ChargeRechargePowerComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.IsPowered == true) //manually apply power level descriptions
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main", ("stateText", Loc.GetString("power-receiver-component-on-examine-powered"))));
        else
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main", ("stateText", Loc.GetString("power-receiver-component-on-examine-unpowered"))));
    }
}
