using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Server.Popups;
using Content.Server.Research.Systems;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Systems;
using Content.Shared.Interaction;
using Content.Shared.Research.Components;
using Robust.Shared.Map;

namespace Content.Server.Telescience;

public sealed partial class TelechargeSystem : SharedTelechargeSystem
{
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ResearchSystem _research = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelechargeComponent, AfterInteractEvent>(OnAfterInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //search for Teleframe entities with the TeleframeRechargingComponent and check if they've reached the end of their timer.
        var queryRecharge = EntityQueryEnumerator<TelechargeRechargingComponent, TelechargeComponent>();
        while (queryRecharge.MoveNext(out var uid, out var recharge, out var telecharge))
        {
            if (Timing.CurTime < recharge.EndTime)
                continue;

            EndTelechargeRecharge((uid, telecharge), recharge);
        }
    }

    private void EndTelechargeRecharge(Entity<TelechargeComponent> ent, TelechargeRechargingComponent recharge)
    {
        RemCompDeferred<TelechargeRechargingComponent>(ent);
    }

    /// <summary>
    /// Transfer science stored in the telecharge to a research server if it has any.
    /// </summary>
    private void OnAfterInteract(Entity<TelechargeComponent> ent, ref AfterInteractEvent args) //basically the same as science disks
    {
        if (!args.CanReach)
            return;

        if (!TryComp<ResearchServerComponent>(args.Target, out var server))
            return;

        if (ent.Comp.Science > 0) //if there's stored science
        {
            _research.ModifyServerPoints(args.Target.Value, ent.Comp.Science, server); //add the science to the server
            _popupSystem.PopupEntity(Loc.GetString("telecharge-inserted", ("points", ent.Comp.Science)), args.Target.Value, args.User);
            ent.Comp.Science = 0; //then remove it from the telecharge
            args.Handled = true;
        }
        UpdateAppearance(ent);
    }
    ///<summary>
    /// Gets coordinate location relative to a beacon only use if there truly is nothing nearby
    /// </summary>
    protected override string GetVagueLocation(MapCoordinates coords)
    {
        return _navMap.GetNearestBeaconString(coords);
    }

    ///<summary>
    /// Telecharge speaks its findings, now go get it lmao
    /// </summary>
    protected override void SendRadioMessage(Entity<TelechargeComponent> ent, string message)
    {
        if (ent.Comp.AnnouncementChannel is { } channel)
            _radio.SendRadioMessage(ent.Owner, message, channel, ent.Owner, escapeMarkup: false);
    }
}
