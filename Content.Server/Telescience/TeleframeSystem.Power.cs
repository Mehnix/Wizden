using Content.Shared.ChargeRecharge.Events;
using Content.Shared.ChargeRecharge.Systems;
using Content.Shared.Emp;
using Content.Shared.Telescience.Systems;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;

namespace Content.Server.Telescience;

public sealed partial class TeleframeSystem : SharedTeleframeSystem
{
    [Dependency] private readonly SharedChargeRechargeSystem _chargeRecharge = default!;
    protected override void InitializePower()
    {
        base.InitializePower();

        SubscribeLocalEvent<TeleframeStructurePowerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, EmpPulseEvent>(OnEmpPulseStructure);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, StartChargingEvent>(OnStartChargingPower);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, StartRechargingEvent>(OnStartRechargingPower);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, EndRechargingEvent>(OnEndRechargingPower);
    }

    /// <summary>
    /// checks power situation when spawned
    /// </summary>
    private void OnStartup(Entity<TeleframeStructurePowerComponent> ent, ref ComponentStartup args)
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
    private void ReceivedChanged(Entity<TeleframeStructurePowerComponent> ent, ref PowerConsumerReceivedChanged args)
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
                _chargeRecharge.PauseRecharge(ent.Owner); //pause recharge if we're above idle power but not at Draw Rate (Active Power)
            }
        }
        else
        {
            StructPowerOn(ent);
        }
    }

    /// <summary>
    /// Deals with teleframe structures being in the powered off state
    /// </summary>
    private void StructPowerOff(Entity<TeleframeStructurePowerComponent, TeleframeComponent?, PowerConsumerComponent?> ent)
    {
        Log.Debug("Power Off");
        if (!Resolve(ent, ref ent.Comp2, ref ent.Comp3)) //Stop here if no Teleframe or PowerConsumer component
            return;

        if (ent.Comp3.ReceivedPower <= 0) //total blackout
            ent.Comp3.DrawRate = 1; //draw rate is 1 rather than 0 as this means when power is applied a PowerConsumerRecievedChanged event fires to update power again.

        if (ent.Comp1.IsPowered == true)
        {
            PowerOff((ent.Owner, ent.Comp1)); //go to generic function
            ent.Comp1.IsPowered = false;
            Dirty(ent.Owner, ent.Comp1);
        }

    }

    /// <summary>
    /// Deals with teleframe structures being in the powered on state
    /// </summary>
    private void StructPowerOn(Entity<TeleframeStructurePowerComponent, TeleframeComponent?, PowerConsumerComponent?> ent)
    {
        Log.Debug("Power On");
        if (!Resolve(ent, ref ent.Comp2, ref ent.Comp3)) //Stop here if no Teleframe or PowerConsumer component
            return;

        ent.Comp1.IsPowered = true;
        Dirty(ent.Owner, ent.Comp1);

        if (ent.Comp3.DrawRate == 1)
        {
            ent.Comp3.DrawRate = ent.Comp1.PowerUseIdle;
        }

        PowerOn((ent.Owner, ent.Comp1)); //go to generic function
    }

    /// <summary>
    /// immediately turn off if unanchored
    /// </summary>
    private void OnAnchorStateChanged(Entity<TeleframeStructurePowerComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        StructPowerOff(ent);
    }

    /// <summary>
    /// immediately turn off if EMP'd
    /// </summary>
    private void OnEmpPulseStructure(Entity<TeleframeStructurePowerComponent> ent, ref EmpPulseEvent args)
    {
        StructPowerOff(ent);
    }

    /// <summary>
    /// Switch to active power when the teleframe starts charging
    /// </summary>
    private void OnStartChargingPower(Entity<TeleframeStructurePowerComponent> ent, ref StartChargingEvent args)
    {
        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
        {
            powerConsumer.DrawRate = ent.Comp.PowerUseActive; // set to high power draw, it actually takes a while to build up due to high demand so this preps for recharge
            Log.Debug($"Start Charge power {powerConsumer.DrawRate}");
            CheckPower((ent.Owner, powerConsumer));
        }
    }

    /// <summary>
    /// Make sure the Teleframe is on active power for recharging
    /// </summary>
    private void OnStartRechargingPower(Entity<TeleframeStructurePowerComponent> ent, ref StartRechargingEvent args)
    {
        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
        {
            powerConsumer.DrawRate = ent.Comp.PowerUseActive; // confirm high power draw, should already be set, but we need to check power anyway so may as well do this too
            Log.Debug($"Start Recharge power {powerConsumer.DrawRate}");
            CheckPower((ent.Owner, powerConsumer));
        }
    }

    /// <summary>
    /// Switch to idle power when recharge finishes
    /// </summary>
    private void OnEndRechargingPower(Entity<TeleframeStructurePowerComponent> ent, ref EndRechargingEvent args)
    {
        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
        {
            powerConsumer.DrawRate = ent.Comp.PowerUseIdle; // recharge end so idle power
            Log.Debug($"End recharge power {powerConsumer.DrawRate}");
            CheckPower((ent.Owner, powerConsumer));
        }
    }

    /// <summary>
    /// Make the teleframe check its own power consumption, as it won't always do it on its own if external power doesn't change but its requirements do
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
