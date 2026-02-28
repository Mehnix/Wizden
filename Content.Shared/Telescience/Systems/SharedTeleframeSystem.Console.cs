using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Systems;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Ui;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    protected virtual void InitializeConsole()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleframeConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<TeleframeConsoleComponent, PortDisconnectedEvent>(OnPortDisconnected);
        SubscribeLocalEvent<TeleframeConsoleComponent, GotEmaggedEvent>(OnConsoleEmagged);

        SubscribeLocalEvent<TeleframeConsoleComponent, BoundUIOpenedEvent>(OnUiOpen);
        SubscribeLocalEvent<TeleframeConsoleComponent, BoundUIClosedEvent>(OnUiClosed);
    }

    /// <summary>
    /// Links both Teleframe console and Teleframe
    /// </summary>
    private void OnNewLink(Entity<TeleframeConsoleComponent> ent, ref NewLinkEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        if (TryComp<TeleframeComponent>(args.Sink, out var tp)) //link Teleframe to Teleframe console
        {
            ent.Comp.LinkedTeleframe = args.Sink;
            tp.LinkedConsole = ent;
            Dirty(args.Sink, tp);
            Dirty(ent);
        }
    }

    /// <summary>
    /// Disconnects Teleframe Console and Teleframe, setting both sides' Linked variables to null
    /// </summary>
    private void OnPortDisconnected(Entity<TeleframeConsoleComponent> ent, ref PortDisconnectedEvent args) //stolen from SharedArtifactAnalyzerSystem
    {
        var tpUid = ent.Comp.LinkedTeleframe;
        if (args.Port == ent.Comp.LinkingPort && tpUid != null)
        {
            if (TryComp<TeleframeComponent>(tpUid, out var tp))
            {
                tp.LinkedConsole = null;
                Dirty(tpUid.Value, tp);
            }

            ent.Comp.LinkedTeleframe = null;
            Dirty(ent);
        }
    }

    /// <summary>
    /// Adds the emag flag
    /// </summary>
    private void OnConsoleEmagged(Entity<TeleframeConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Interaction))
            return;

        if (_emag.CheckFlag(ent, EmagType.Interaction))
            return;

        args.Handled = true;
    }

    /// <summary>
    /// on opening UI add beacons to pvs override list so client can see them outside of view range
    /// </summary>
    private void OnUiOpen(Entity<TeleframeConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        if (!args.UiKey.Equals(TeleframeConsoleUiKey.Key))
            return;

        if (!_player.TryGetSessionByEntity(args.Actor, out var session)) //one would assume someone interacting with a UI is a player
            return;

        foreach (var beacon in ent.Comp.BeaconList)
        {
            if (TryGetEntity(beacon.TelePoint, out var beaconEnt))
                _pvs.AddSessionOverride(beaconEnt.Value, session);
            else
                ent.Comp.BeaconList.Remove(beacon); //do some housecleaning and remove beacons that have been deleted outright.

            if (ent.Comp.LinkedTeleframe != null)
                _pvs.AddSessionOverride(ent.Comp.LinkedTeleframe.Value, session);
        }

        Dirty(ent);
    }

    /// <summary>
    /// on closing UI remove beacons from pvs list again
    /// </summary>
    private void OnUiClosed(Entity<TeleframeConsoleComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!Timing.IsFirstTimePredicted) //prevent it getting spammed
            return;

        if (!args.UiKey.Equals(TeleframeConsoleUiKey.Key))
            return;

        if (!_player.TryGetSessionByEntity(args.Actor, out var session)) //one would assume someone interacting with a UI is a player
            return;

        foreach (var beacon in ent.Comp.BeaconList)
        {
            if (TryGetEntity(beacon.TelePoint, out var beaconEnt))
                _pvs.RemoveSessionOverride(beaconEnt.Value, session);
            else
                ent.Comp.BeaconList.Remove(beacon); //do some housecleaning and remove beacons that have been deleted outright.
        }

        if (ent.Comp.LinkedTeleframe != null)
            _pvs.RemoveSessionOverride(ent.Comp.LinkedTeleframe.Value, session);

        Dirty(ent);
    }
}
