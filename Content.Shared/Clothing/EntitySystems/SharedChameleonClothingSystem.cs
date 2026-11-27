using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Clothing.Components;
using Content.Shared.Contraband;
using Content.Shared.Emp;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.EntitySystems;

public abstract partial class SharedChameleonClothingSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private ClothingSystem _clothingSystem = default!;
    [Dependency] private ContrabandSystem _contraband = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedItemSystem _itemSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private LockSystem _lock = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] protected SharedUserInterfaceSystem UI = default!;
    [Dependency] private INetManager _net = default!;

    private static readonly SlotFlags[] IgnoredSlots =
    {
        SlotFlags.All,
        SlotFlags.PREVENTEQUIP,
    };

    private static readonly SlotFlags[] Slots = Enum.GetValues<SlotFlags>().Except(IgnoredSlots).ToArray();

    private readonly Dictionary<SlotFlags, List<EntProtoId>> _data = new();

    public readonly Dictionary<SlotFlags, List<string>> ValidVariants = new();

    private static readonly ProtoId<TagPrototype> WhitelistChameleonTag = "WhitelistChameleon";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<ChameleonClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<ChameleonClothingComponent, GetVerbsEvent<InteractionVerb>>(OnVerb);
        SubscribeLocalEvent<ChameleonClothingComponent, EmpPulseEvent>(OnEmpPulse);

        SubscribeLocalEvent<ChameleonClothingComponent, PrototypesReloadedEventArgs>(OnPrototypeReload);
        PrepareAllVariants();
    }

    private void OnPrototypeReload(EntityUid uid, ChameleonClothingComponent component, PrototypesReloadedEventArgs args)
    {
        PrepareAllVariants();
    }

    private void OnGotEquipped(EntityUid uid, ChameleonClothingComponent component, GotEquippedEvent args)
    {
        if (Timing.ApplyingState)
            return; // Already networked as part of the same gamestate

        component.User = args.EquipTarget;
        Dirty(uid, component);
    }

    private void OnGotUnequipped(EntityUid uid, ChameleonClothingComponent component, GotUnequippedEvent args)
    {
        if (Timing.ApplyingState)
            return; // Already networked as part of the same gamestate

        component.User = null;
        Dirty(uid, component);
    }

    /// <summary>
    /// Update the original and current access, retaining access that the entity began with.
    /// </summary>
    protected void UpdateAccess(EntityUid uid, EntityPrototype proto)
    {
        if (!TryComp(uid, out AccessReaderComponent? accessUid) || !proto.TryComp(out AccessReaderComponent? accessProto, Factory))
            return;

        var test = accessUid;
        var accesses = _access.CompareNewAccess(test); //gets the difference between the "original" and "current" which should be whatever the intial access is
        if (accesses.Count <= 0) //if there is no difference, there has been no chameleoning or there is no other access attached to the current proto so we just take the current initial access list
        {
            accesses = accessUid.AccessLists; //this also results in any changes to access from an access config being copied over which is also fine.
            Dirty(uid, accessUid);
        }
        _access.ReplaceOriginalAccess((uid, accessUid), accessProto.AccessLists); //remove the original proto accesses and replace with the new
        _access.TrySetAccesses((uid, accessUid), accessProto.AccessLists.Concat(accesses).ToList()); //Add new proto access along with original access this entity began with
    }

    protected void UpdateSize(EntityUid uid, EntityPrototype proto)
    {
        if (!TryComp(uid, out ItemComponent? itemComp) || !proto.TryComp(out ItemComponent? itemProto, Factory))
            return;

        _itemSystem.SetSize(uid, itemProto.Size, itemComp);
        _itemSystem.SetShape(uid, itemProto.Shape, itemComp);
    }

    // Updates chameleon visuals and meta information.
    // This function is called on a server after user selected new outfit.
    // And after that on a client after state was updated.
    // This 100% makes sure that server and client have exactly same data.
    protected void UpdateVisuals(EntityUid uid, ChameleonClothingComponent component)
    {
        if (string.IsNullOrEmpty(component.Default) ||
            !ProtoMan.Resolve(component.Default, out EntityPrototype? proto))
            return;

        // world sprite icon
        UpdateSprite(uid, proto);

        // copy name and description, unless its an ID card
        if (!HasComp<IdCardComponent>(uid))
        {
            var meta = MetaData(uid);
            _metaData.SetEntityName(uid, proto.Name, meta);
            _metaData.SetEntityDescription(uid, proto.Description, meta);
        }

        // item sprite logic
        if (TryComp(uid, out ItemComponent? item) &&
            proto.TryComp(out ItemComponent? otherItem, Factory))
        {
            _itemSystem.CopyVisuals(uid, otherItem, item);
        }

        // clothing sprite logic
        if (TryComp(uid, out ClothingComponent? clothing) &&
            proto.TryComp(out ClothingComponent? otherClothing, Factory))
        {
            _clothingSystem.CopyVisuals(uid, otherClothing, clothing);
        }

        // appearance data logic
        if (TryComp(uid, out AppearanceComponent? appearance) &&
            proto.TryComp(out AppearanceComponent? appearanceOther, Factory))
        {
            _appearance.AppendData(appearanceOther, uid);
            Dirty(uid, appearance);
        }

        // properly mark contraband
        if (proto.TryComp(out ContrabandComponent? contra, Factory))
        {
            EnsureComp<ContrabandComponent>(uid, out var current);
            _contraband.CopyDetails(uid, contra, current);
        }
        else
        {
            RemComp<ContrabandComponent>(uid);
        }
    }

    private void OnVerb(Entity<ChameleonClothingComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || _lock.IsLocked(ent.Owner))
            return;

        if (!ent.Comp.ShowVerb)
            return;

        // Can't pass args from a ref event inside of lambdas
        var user = args.User;

        args.Verbs.Add(new InteractionVerb()
        {
            Text = Loc.GetString("chameleon-component-verb-text"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
            Act = () => UI.TryToggleUi(ent.Owner, ChameleonUiKey.Key, user)
        });
    }

    private void OnEmpPulse(EntityUid uid, ChameleonClothingComponent component, ref EmpPulseEvent args)
    {
        if (!component.AffectedByEmp)
            return;

        if (component.EmpContinuous)
            component.NextEmpChange = Timing.CurTime + TimeSpan.FromSeconds(1f / component.EmpChangeIntensity);

        if (_net.IsServer) // needs RandomPredicted
        {
            var pick = GetRandomValidPrototype(component.Slot, component.RequireTag);
            SetSelectedPrototype(uid, pick, component: component);
        }

        args.Affected = true;
        args.Disabled = true;
    }

    protected virtual void UpdateSprite(EntityUid uid, EntityPrototype proto) { }

    /// <summary>
    ///     Check if this entity prototype is valid target for chameleon item.
    /// </summary>
    public bool IsValidTarget(EntityPrototype proto, SlotFlags chameleonSlot = SlotFlags.NONE, string? requiredTag = null)
    {
        // check if entity is valid
        if (proto.Abstract || proto.HideSpawnMenu)
            return false;

        // check if it is marked as valid chameleon target
        proto.TryComp(out TagComponent? tag, Factory);
        proto.TryComp(out WhitelistChameleonComponent? whitelist, Factory);

        if (!(tag != null && _tag.HasTag(tag, WhitelistChameleonTag) || whitelist != null && whitelist.Valid))
            return false;

        if (requiredTag != null && !(tag != null && _tag.HasTag(tag, requiredTag) || whitelist != null && HasTag(whitelist, requiredTag)))
            return false;

        // check if it's valid clothing
        if (proto.TryComp(out ClothingComponent? clothing, Factory) && !clothing.Slots.HasFlag(chameleonSlot))
            return false;

        return true;
    }

    /// <summary>
    ///     Get a list of valid chameleon targets for these slots.
    /// </summary>
    public IEnumerable<EntProtoId> GetValidTargets(SlotFlags slot, string? tag = null)
    {
        var validTargets = new List<EntProtoId>();
        if (tag != null)
        {
            foreach (var proto in _data[slot])
            {
                if (IsValidTarget(ProtoMan.Index(proto), slot, tag))
                    validTargets.Add(proto);
            }
        }
        else
        {
            validTargets = _data[slot];
        }

        return validTargets;
    }

    /// <summary>
    ///     Get a random prototype for a given slot.
    /// </summary>
    public string GetRandomValidPrototype(SlotFlags slot, string? tag = null)
    {
        return _random.Pick(GetValidTargets(slot, tag).ToList());
    }

    protected void PrepareAllVariants()
    {
        _data.Clear();
        var prototypes = ProtoMan.EnumeratePrototypes<EntityPrototype>();

        foreach (var proto in prototypes)
        {
            // check if this is valid clothing
            if (!IsValidTarget(proto))
                continue;
            if (proto.TryComp(out ClothingComponent? item, Factory))
            {
                // sort item by their slot flags
                // one item can be placed in several buckets
                foreach (var slot in Slots)
                {
                    if (!item.Slots.HasFlag(slot))
                        continue;

                    if (!_data.ContainsKey(slot))
                    {
                        _data.Add(slot, new List<EntProtoId>());
                    }
                    _data[slot].Add(proto.ID);
                }
            }
            else
            {   // WhitelistChameleon tagged entities that are not clothing go into the NONE SlotFlag, additional tags will be needed to seperate them further.
                if (!_data.ContainsKey(SlotFlags.NONE))
                {
                    _data.Add(SlotFlags.NONE, new List<EntProtoId>());
                }
                _data[SlotFlags.NONE].Add(proto.ID);
            }
        }
    }

    public bool HasTag(WhitelistChameleonComponent component, [ForbidLiteral] ProtoId<TagPrototype> tag)
    {
        return component.Tags.Contains(tag);
    }

    // TODO: Predict and use component states for the UI
    /// <summary>
    /// Change chameleon items name, description and sprite to mimic other entity prototype.
    /// </summary>
    /// <param name="uid">The entity who's appearance to swap.</param>
    /// <param name="protoId">The target protoId of the target appearance.</param>
    /// <param name="forceUpdate">Whether to force update appearance, even if the same one was selected.</param>
    /// <param name="validate">Whether to validate if the target prototype is a valid chameleon target.</param>
    /// <param name="component">The <see cref="ChameleonClothingComponent"/> of the entity we are updating.</param>
    public virtual void SetSelectedPrototype(EntityUid uid, string? protoId, bool forceUpdate = false, bool validate = true,
        ChameleonClothingComponent? component = null)
    { }
}
