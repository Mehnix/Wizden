using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;

namespace Content.Shared.Telescience.Systems;

public abstract partial class SharedTeleframeSystem : EntitySystem
{
    protected virtual void InitializeRelay()
    {
        SubscribeLocalEvent<TeleframeComponent, TeleframeInitiatedEvent>(RelayToConsole);
        SubscribeLocalEvent<TeleframeComponent, TeleframeTeleportFailedEvent>(RelayToConsole);
        SubscribeLocalEvent<TeleframeComponent, TeleframeReadyEvent>(RelayToConsole);
        SubscribeLocalEvent<TeleframeComponent, TeleframeIncidentEvent>(RelayToConsole);
    }

    /// <summary>
    /// Relay events on the Teleframe to the Console
    /// </summary>
    protected void RelayToConsole<T>(Entity<TeleframeComponent> ent, ref T args)
    {
        if (ent.Comp.LinkedConsole is not { } console || !HasComp<TeleframeConsoleComponent>(console) || ent.Owner == console)
            return; //do not sent if no linked console exists, the linked console does not have the relevent component, or the both components share the same entity

        var ev = new TeleframeToConsoleRelayEvent<T>(ent, args);
        RaiseLocalEvent(console, ref ev);
    }

    /// <summary>
    /// Relay events on the Console to the Teleframe
    /// </summary>
    protected void RelayToFrame<T>(Entity<TeleframeConsoleComponent> ent, ref T args)
    {
        if (ent.Comp.LinkedTeleframe is not { } frame || !HasComp<TeleframeComponent>(frame) || ent.Owner == frame)
            return; //do not sent if no linked console exists, the linked frame does not have the relevent component, or the both components share the same entity

        var ev = new TeleframeConsoleToFrameRelayEvent<T>(ent, args);
        RaiseLocalEvent(frame, ref ev);
    }
}
