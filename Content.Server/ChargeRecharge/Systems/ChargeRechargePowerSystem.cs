using Content.Shared.ChargeRecharge.Components;
using Content.Shared.ChargeRecharge.Events;
using Content.Shared.ChargeRecharge.Systems;
using Content.Shared.Emp;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;

namespace Content.Server.ChargeRecharge.Systems;

public sealed partial class ChargeRechargePowerSystem : SharedChargeRechargePowerSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChargeRechargePowerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChargeRechargePowerComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
        SubscribeLocalEvent<ChargeRechargePowerComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<ChargeRechargePowerComponent, EmpPulseEvent>(OnEmpPulseStructure);

        SubscribeLocalEvent<ChargeRechargePowerComponent, StartChargingEvent>(OnStartChargingPower);
        SubscribeLocalEvent<ChargeRechargePowerComponent, EndChargingEvent>(OnEndChargingPower);
        SubscribeLocalEvent<ChargeRechargePowerComponent, StartRechargingEvent>(OnStartRechargingPower);
        SubscribeLocalEvent<ChargeRechargePowerComponent, EndRechargingEvent>(OnEndRechargingPower);
    }

    /// <summary>
    /// checks power situation when spawned
    /// </summary>
    private void OnStartup(Entity<ChargeRechargePowerComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
            return;

        powerConsumer.DrawRate = ent.Comp.PowerUseIdle;
        Dirty(ent);
        CheckPower((ent.Owner, powerConsumer));
    }

    /// <summary>
    /// Checks power situation if received amount changes
    /// </summary>
    private void ReceivedChanged(Entity<ChargeRechargePowerComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        //Log.Debug($"{args.ReceivedPower} {args.DrawRate}");
        if (Math.Ceiling(args.ReceivedPower) < Math.Floor(args.DrawRate)) //round or get floating point errors at large values
        { //must have at least idle power levels to turn on
            if (args.ReceivedPower < ent.Comp.PowerUseIdle) //if power levels below idle, turn off
            {
                StructPowerOff(ent);
            }
            else
            {
                ChargeRecharge.PauseRecharge(ent.Owner); //pause recharge if we're above idle power but not at Draw Rate (Active Power)
            }
        }
        else
        {
            StructPowerOn(ent);
        }
    }

    /// <summary>
    /// Deals with structures being in the powered off state
    /// </summary>
    private void StructPowerOff(Entity<ChargeRechargePowerComponent, PowerConsumerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2)) //Stop here if no chargerecharge or PowerConsumer component
            return;

        if (ent.Comp2.ReceivedPower <= 0) //total blackout
            ent.Comp2.DrawRate = 1; //draw rate is 1 rather than 0 as this means when power is applied a PowerConsumerRecievedChanged event fires to update power again.

        if (ent.Comp1.IsPowered == true)
        {
            PowerOff((ent.Owner, ent.Comp1)); //go to generic function
            ent.Comp1.IsPowered = false;
            Dirty(ent.Owner, ent.Comp1);
        }
    }

    /// <summary>
    /// Deals with structures being in the powered on state
    /// </summary>
    private void StructPowerOn(Entity<ChargeRechargePowerComponent, PowerConsumerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2)) //Stop here if no PowerConsumer component
            return;

        ent.Comp1.IsPowered = true;
        Dirty(ent.Owner, ent.Comp1);

        if (ent.Comp2.DrawRate == 1)
        {
            ent.Comp2.DrawRate = ent.Comp1.PowerUseIdle;
        }

        PowerOn((ent.Owner, ent.Comp1)); //go to generic function
    }

    /// <summary>
    /// immediately turn off if unanchored
    /// </summary>
    private void OnAnchorStateChanged(Entity<ChargeRechargePowerComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        StructPowerOff(ent);
    }

    /// <summary>
    /// immediately turn off if EMP'd
    /// </summary>
    private void OnEmpPulseStructure(Entity<ChargeRechargePowerComponent> ent, ref EmpPulseEvent args)
    {
        StructPowerOff(ent);
    }

    /// <summary>
    /// Switch to active power when the structure starts charging
    /// </summary>
    private void OnStartChargingPower(Entity<ChargeRechargePowerComponent> ent, ref StartChargingEvent args)
    {
        SetPower(ent.Owner, ent.Comp.PowerUseActive);
    }

    /// <summary>
    /// Switch to idle power when recharge finishes
    /// </summary>
    private void OnEndChargingPower(Entity<ChargeRechargePowerComponent> ent, ref EndChargingEvent args)
    {
        if (TryComp<ChargeRechargeComponent>(ent, out var charReComp) && charReComp.ImmediateRecharge == true)
            return; //avoid power draw flicker if recharge is starting immediately

        SetPower(ent.Owner, ent.Comp.PowerUseIdle);
    }

    /// <summary>
    /// Make sure the structure is on active power for recharging
    /// </summary>
    private void OnStartRechargingPower(Entity<ChargeRechargePowerComponent> ent, ref StartRechargingEvent args)
    {
        SetPower(ent.Owner, ent.Comp.PowerUseActive);
    }

    /// <summary>
    /// Switch to idle power when recharge finishes
    /// </summary>
    private void OnEndRechargingPower(Entity<ChargeRechargePowerComponent> ent, ref EndRechargingEvent args)
    {
        SetPower(ent.Owner, ent.Comp.PowerUseIdle);
    }

    /// <summary>
    /// Sets the power of the structure, then makes it re-assess its power situation
    /// </summary>
    private void SetPower(EntityUid uid, int power)
    {
        if (TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
        {
            powerConsumer.DrawRate = power; // recharge end so idle power
            CheckPower((uid, powerConsumer));
        }
    }

    /// <summary>
    /// Make the structure check its own power consumption, as it won't always do it on its own if external power doesn't change but its requirements do
    /// </summary>
    private void CheckPower(Entity<PowerConsumerComponent> ent)
    {
        var newRecv = ent.Comp.NetworkLoad.ReceivingPower;
        ref var lastRecv = ref ent.Comp.LastReceived;
        lastRecv = newRecv;
        var msg = new PowerConsumerReceivedChanged(newRecv, ent.Comp.DrawRate);
        RaiseLocalEvent(ent, ref msg);
    }
}
