using Content.Shared.Telescience.Systems;
using Content.Shared.Telescience.Components;

namespace Content.Server.Telescience;

public sealed partial class TeleframeSystem : SharedTeleframeSystem
{
    [Dependency] private readonly SharedMapSystem _maps = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        //search for Teleframe entities with the TeleframeChargingComponent and check if they've reached the end of their timer.
        var queryCharge = EntityQueryEnumerator<TeleframeChargingComponent, TeleframeComponent>();
        while (queryCharge.MoveNext(out var uid, out var charge, out var teleframe))
        {
            if (Timing.CurTime < charge.EndTime && charge.TeleportSuccess == true) //end if charge time runs out or we're failing
                continue;

            EndTeleportCharge((uid, teleframe, charge));
        }

        //search for Teleframe entities with the TeleframeRechargingComponent and check if they've reached the end of their timer.
        var queryRecharge = EntityQueryEnumerator<TeleframeRechargingComponent, TeleframeComponent>();
        while (queryRecharge.MoveNext(out var uid, out var recharge, out var teleframe))
        {
            if (recharge.Pause || Timing.CurTime < recharge.EndTime)
                continue;

            EndTeleportRecharge((uid, teleframe), recharge);
        }
    }
}
