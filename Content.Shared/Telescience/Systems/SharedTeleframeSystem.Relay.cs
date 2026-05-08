using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;
using Content.Shared.ChargeRecharge.Events;

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
        if (ent.Comp.LinkedConsole is not { } console)
            return;

        var ev = new TeleframeToConsoleRelayEvent<T>(ent, args);
        RaiseLocalEvent(console, ref ev);
    }

    /// <summary>
    /// Relay events on the Console to the Teleframe
    /// </summary>
    protected void RelayToFrame<T>(Entity<TeleframeConsoleComponent> ent, ref T args)
    {
        if (ent.Comp.LinkedTeleframe is not { } frame)
            return;

        var ev = new TeleframeConsoleToFrameRelayEvent<T>(ent, args);
        RaiseLocalEvent(frame, ref ev);
    }
}
