using Content.Shared.ChargeRecharge.Components;
using Content.Shared.Telescience.Components;
using Content.Shared.Examine;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    protected virtual void InitializePower()
    {
        base.Initialize();
        SubscribeLocalEvent<TeleframeStructurePowerComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    /// turn off teleframe, interrupt charge and fail it, pause recharge and update its pause time (if it wasn't already)
    /// </summary>
    protected void PowerOff(Entity<TeleframeStructurePowerComponent> ent)
    {
        if (!HasComp<ChargeRechargeComponent>(ent.Owner))
            return;

        ent.Comp.IsPowered = false;

        _chargeRecharge.DisableCharge(ent.Owner, "a-power");

        if (_lights.TryGetLight(ent.Owner, out var light) && light.Enabled == true) //set light off whilst here
            _lights.SetEnabled(ent.Owner, false);

        Dirty(ent);
    }

    /// <summary>
    /// power on teleframe, unpause recharge if it was there.
    /// </summary>
    protected void PowerOn(Entity<TeleframeStructurePowerComponent> ent)
    {
        _chargeRecharge.EnableCharge(ent.Owner);

        ent.Comp.IsPowered = true;

        if (_lights.TryGetLight(ent.Owner, out var light) && light.Enabled == false) //set light on whilst here
            _lights.SetEnabled(ent.Owner, true); //dirties itself

        Dirty(ent);
    }

    public void OnExamined(Entity<TeleframeStructurePowerComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.IsPowered == true) //manually apply power level descriptions
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main", ("stateText", Loc.GetString("power-receiver-component-on-examine-powered"))));
        else
            args.PushMarkup(Loc.GetString("power-receiver-component-on-examine-main", ("stateText", Loc.GetString("power-receiver-component-on-examine-unpowered"))));
    }
}
