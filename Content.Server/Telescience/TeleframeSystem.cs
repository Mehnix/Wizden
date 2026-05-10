using Content.Shared.ChargeRecharge.Events;
using Content.Shared.Physics;
using Content.Shared.Telescience.Components;
using Content.Shared.Telescience.Events;
using Content.Shared.Telescience.Systems;
using Robust.Shared.Map;

namespace Content.Server.Telescience;

public sealed partial class TeleframeSystem : SharedTeleframeSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeleframeComponent, StartChargingEvent>(OnStartTeleportCharge);
        SubscribeLocalEvent<TeleframeComponent, EndChargingEvent>(OnEndTeleportCharge);
        SubscribeLocalEvent<TeleframeComponent, EndRechargingEvent>(OnEndTeleportRecharge);
    }

    #region Charge/Recharge
    // Functions handling whether teleportation is successful are unreliable to predict, as teleportation can go anywhere.
    // While the base functions that call these checks could be in shared and call an overridden abstract function, it results in teleport failiure mispredictions that feel weird.
    /// <summary>
    /// When Teleport Charge starts, check teleframe and teleportals have initialised correctly
    /// </summary>
    public void OnStartTeleportCharge(Entity<TeleframeComponent> ent, ref StartChargingEvent args)
    {
        var (teleportSuccess, failReason) = CheckTeleportation(ent);
        if (teleportSuccess == false) //start of charge wellness check on the teleframe, if not good, just end the charge immediately
        {
            ChargeRecharge.EndCharge(ent.Owner, false, failReason);
        }
        else
        {
            ent.Comp.ReadyToTeleport = false;
            Dirty(ent);

            var ev = new TeleframeInitiatedEvent(ent.Owner, ent.Comp.ActiveTeleportInfo!.Value);
            RaiseLocalEvent(ent, ref ev);
        }
    }

    /// <summary>
    /// When Teleport Charge completes, check whether Teleportation is allowed
    /// </summary>
    public void OnEndTeleportCharge(Entity<TeleframeComponent> ent, ref EndChargingEvent args)
    {
        ChargeRecharge.StartRecharge(ent.Owner);

        if (args.Success == false) //if anything caused a fail during charging, cleanup
        {
            TeleportFail(ent, args.FailReason);
        }
        else
        {
            var (teleportSuccess, failReason) = CheckTeleportation(ent);
            if (teleportSuccess == false) //end of charge wellness check on the teleframe
            {
                TeleportFail(ent, failReason);
            }
            else
            {
                var ev = new TeleframeTeleportBeginEvent(ent.Owner, ent.Comp.ActiveTeleportInfo!.Value);
                RaiseLocalEvent(ent, ref ev);
            }
        }
    }

    /// <summary>
    /// Recharge is done, indicate this to player at console
    /// </summary>
    public void OnEndTeleportRecharge(Entity<TeleframeComponent> ent, ref EndRechargingEvent args)
    {
        ent.Comp.ReadyToTeleport = true;

        var ev = new TeleframeReadyEvent(ent.Owner);
        RaiseLocalEvent(ent, ref ev);

        Dirty(ent);
    }
    #endregion

    // these checks require information the client potentially doesn't know.
    /// <summary>
    /// Prevent teleportation if receive teleportal is not on a grid or inside a wall, send teleportal is allowed to be off grid so you can teleport from empty space but not to.
    /// </summary>
    /// <param name="teleportal">teleportal entity</param>
    /// <returns></returns>
    public (bool, string?) CheckTeleportal(EntityUid teleportal, bool allowCollision = false, bool allowGridless = false)
    {
        if (!Exists(teleportal) || Transform(teleportal).MapID == MapId.Nullspace) //does this entity exist and is not in nullspace
            return (false, "teleport-fail-nolink");

        if (!HasComp<TeleframeTeleportalComponent>(teleportal)) //does the teleportal have its tracking component
            return (false, "teleport-fail-nolink");

        if (Xform.GetGrid(teleportal) == null && allowGridless == false) //prevent portals off grids unless permitted
            return (false, "teleport-fail-nogrid");

        if (Physics.GetEntitiesIntersectingBody(teleportal, (int)CollisionGroup.Impassable).Count > 0 && allowCollision == false) //prevent collision with impassible objects unless permitted
            return (false, "teleport-fail-collision");

        return (true, null);
    }

    /// <summary>
    /// Check teleframe has done its book-keeping and that it knows where it wants to go
    /// Then check teleportals to see if they're valid
    /// </summary>
    /// <param name="ent">The teleframe</param>
    /// <returns>validity , fail reason if there is one</returns>
    public (bool, string?) CheckTeleportation(Entity<TeleframeComponent> ent)
    {
        if (ent.Comp.ActiveTeleportInfo == null || ent.Comp.ActiveTeleportInfo is not { } teleInfo || !Exists(GetEntity(teleInfo.From)) || !Exists(GetEntity(teleInfo.To))) //is active teleport info null, is the teleport info empty, do either teleport entity not exist
            return (false, "teleport-fail-nolink");
        //check From teleportal
        var (teleportSuccess, failReason) = CheckTeleportal(GetEntity(teleInfo.From), ent.Comp.AllowCollision, ent.Comp.AllowGridless ?? true);
        if (teleportSuccess == false)
            return (teleportSuccess, failReason);
        //check To teleportal
        (teleportSuccess, failReason) = CheckTeleportal(GetEntity(teleInfo.To), ent.Comp.AllowCollision, ent.Comp.AllowGridless ?? false);
        if (teleportSuccess == false)
            return (teleportSuccess, failReason);

        return (true, null);
    }
}
