using Content.Shared.Telescience.Systems;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;
using Content.Server.Power.EntitySystems;
using Content.Server.Power.Components;

namespace Content.Server.Telescience;

public sealed partial class TeleframeSystem : SharedTeleframeSystem
{
    protected override void InitializePower()
    {
        base.InitializePower();

        SubscribeLocalEvent<TeleframeStructurePowerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, PowerConsumerReceivedChanged>(ReceivedChanged);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, ChargingEvent>(OnTeleframeChargingStart);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, RechargingEvent>(OnTeleframeRechargingStart);
        SubscribeLocalEvent<TeleframeStructurePowerComponent, TeleframeReadyEvent>(OnTeleframeRecharged);
    }

    /// <summary>
    /// checks power situation when spawned
    /// </summary>
    private void OnStartup(Entity<TeleframeStructurePowerComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<PowerConsumerComponent>(ent, out var powerConsume))
            return;

        if (powerConsume.ReceivedPower < powerConsume.DrawRate)
            StructPowerOff(ent);
        else
            StructPowerOn(ent);
    }

    /// <summary>
    /// Checks power situation if received amount changes
    /// </summary>
    private void ReceivedChanged(Entity<TeleframeStructurePowerComponent> ent, ref PowerConsumerReceivedChanged args)
    {
        if (Math.Ceiling(args.ReceivedPower) < Math.Floor(args.DrawRate)) //round or get floating point errors at large values
        { //must have at least idle power levels to turn on
            if (args.ReceivedPower < ent.Comp.PowerUseIdle) //if power levels below idle, turn off
            {
                StructPowerOff(ent);
            }
            else
            {
                if (TryComp<TeleframeRechargingComponent>(ent, out var rechargeComp) && rechargeComp.Pause == false)
                { //if we do have decent power but not enough for recharge, pause recharge and update its pause time
                    rechargeComp.Pause = true;
                    rechargeComp.PauseTime = rechargeComp.EndTime - Timing.CurTime;
                    Dirty(ent.Owner, rechargeComp);
                }
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
        if (!Resolve(ent, ref ent.Comp2, ref ent.Comp3)) //Stop here if no Teleframe or PowerConsumer component
            return;

        if (ent.Comp3.ReceivedPower <= 0) //total blackout
            ent.Comp3.DrawRate = 1; //draw rate is 1 rather than 0 as this means when power is applied a PowerConsumerRecievedChanged event fires to update power again.

        PowerOff((ent.Owner, ent.Comp2)); //go to generic function
    }

    /// <summary>
    /// Deals with teleframe structures being in the powered on state
    /// </summary>
    private void StructPowerOn(Entity<TeleframeStructurePowerComponent, TeleframeComponent?, PowerConsumerComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp2, ref ent.Comp3)) //Stop here if no Teleframe or PowerConsumer component
            return;

        PowerOn((ent.Owner, ent.Comp2)); //go to generic function

        if (HasComp<TeleframeRechargingComponent>(ent) || HasComp<TeleframeChargingComponent>(ent)) //restore power draw
            ent.Comp3.DrawRate = ent.Comp1.PowerUseActive; // set to active power draw as still recharging
        else
            ent.Comp3.DrawRate = ent.Comp1.PowerUseIdle; // set to idle power draw

        Dirty(ent.Owner, ent.Comp2);
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
    /// Switch to active power when the teleframe starts charging
    /// </summary>
    private void OnTeleframeChargingStart(Entity<TeleframeStructurePowerComponent> ent, ref ChargingEvent args)
    {
        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
        {
            powerConsumer.DrawRate = ent.Comp.PowerUseActive; // set to high power draw, it actually takes a while to build up due to high demand so this preps for recharge
            Dirty(ent);
            CheckPower((ent.Owner, powerConsumer));
        }
    }

    /// <summary>
    /// Make sure the Teleframe is on active power for recharging
    /// </summary>
    private void OnTeleframeRechargingStart(Entity<TeleframeStructurePowerComponent> ent, ref RechargingEvent args)
    {
        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
        {
            powerConsumer.DrawRate = ent.Comp.PowerUseActive; // confirm high power draw, should already be set, but we need to check power anyway so may as well do this too
            Dirty(ent);
            CheckPower((ent.Owner, powerConsumer));
        }
    }

    /// <summary>
    /// Switch to idle power when recharge finishes
    /// </summary>
    private void OnTeleframeRecharged(Entity<TeleframeStructurePowerComponent> ent, ref TeleframeReadyEvent args)
    {
        if (TryComp<PowerConsumerComponent>(ent, out var powerConsumer))
        {
            powerConsumer.DrawRate = ent.Comp.PowerUseIdle; // recharge end so idle power
            Dirty(ent);
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
        Log.Debug($"Checked {newRecv} {ent.Comp.DrawRate}");
    }
}
