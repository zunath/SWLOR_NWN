using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Combat;

/// <summary>
/// Validates the simulated companion without treating its native carrier as a melee weapon. The logic that used to be
/// static in <c>Service.TwinPistolEquipment</c> is moved here unchanged; the cached pairs and the re-entrancy guards are
/// this instance's own. The item service is injected; calls into the unconverted <c>Droid</c>, <c>Skill</c> and
/// equipment storage code sit behind one-line <c>protected virtual</c> methods whose bodies are the original calls.
/// </summary>
public class TwinPistolEquipmentService : ITwinPistolEquipmentService
{
    public const string PairVariable = "TWIN_PISTOL_PAIR";
    public const string SpareVariable = "TWIN_PISTOL_SPARE";
    public const string MainWeaponVariable = "TWIN_PISTOL_MAIN";
    public const string OwnerVariable = "TWIN_PISTOL_OWNER";
    private const string DestroyingVariable = "TWIN_PISTOL_REMOVING";
    private const string CompanionVariable = "TWIN_PISTOL_COMPANION";
    private const string CompanionResref = "twp_companion";
    private readonly Dictionary<uint, (uint Main, uint Spare)> _pairs = new();
    private readonly HashSet<uint> _changing = new();
    private readonly HashSet<uint> _refreshingAppearance = new();
    private readonly HashSet<uint> _revalidating = new();
    private readonly Dictionary<uint, (uint Item, InventorySlot Slot)> _failedReturns = new();

    private readonly IItemService _item;
    private readonly Lazy<ISkillService> _skill;

    private readonly Lazy<IDroidService> _droid;

    public TwinPistolEquipmentService(IItemService item, Lazy<ISkillService> skill, Lazy<IDroidService> droid)
    {
        _droid = droid;
        _item = item;
        _skill = skill;
    }

    public bool IsChanging(uint creature) => _changing.Contains(creature);

    public bool IsRevalidating(uint creature) => _revalidating.Contains(creature);

    public bool TryBeginRevalidation(uint creature) => _revalidating.Add(creature);

    public void EndRevalidation(uint creature) => _revalidating.Remove(creature);

    public bool TryGetFailedReturn(uint creature, out uint item, out InventorySlot slot)
    {
        if (_failedReturns.TryGetValue(creature, out var failure))
        {
            item = failure.Item;
            slot = failure.Slot;
            return true;
        }

        item = OBJECT_INVALID;
        slot = default;
        return false;
    }

    public void SetFailedReturn(uint creature, uint item, InventorySlot slot) => _failedReturns[creature] = (item, slot);

    public void ClearFailedReturn(uint creature) => _failedReturns.Remove(creature);

    // A droid has no client; its controlling player gets the explanation.
    private uint MessageRecipient(uint creature) => IsDroid(creature) ? GetMaster(creature) : creature;

    public bool IsPairItem(uint item) => GetIsObjectValid(item) &&
        IsPairItem(GetBaseItemType(item), GetLocalInt(item, PairVariable) == 1);

    public bool IsPairItem(BaseItem type, bool marked) =>
        marked && type is BaseItem.Pistol or BaseItem.Sling;

    public bool IsGeneratedCompanion(uint item) => GetIsObjectValid(item) &&
        GetBaseItemType(item) == BaseItem.CompanionPistol && GetLocalInt(item, SpareVariable) == 1;

    public bool IsDestroyingCompanion(uint item) =>
        IsGeneratedCompanion(item) && GetLocalInt(item, DestroyingVariable) == 1;

    public bool IsLinkedPair(uint creature, uint main, uint off) =>
        IsPairItem(main) && IsGeneratedCompanion(off) &&
        GetLocalObject(off, MainWeaponVariable) == main && GetLocalObject(off, OwnerVariable) == creature &&
        GetItemPossessor(main) == creature && GetItemPossessor(off) == creature;

    /// <summary>Includes the required rank used by native weapon attribute-delta caps.</summary>
    public bool IsMirroredProperty(ItemPropertyType type) => type is
        ItemPropertyType.DMG or ItemPropertyType.Delay or ItemPropertyType.Accuracy or
        ItemPropertyType.AccuracyStat or ItemPropertyType.DamageStat or ItemPropertyType.WeaponDamageType or
        ItemPropertyType.RequiresSkill;

    /// <summary>Creates the disposable visual/weapon profile for one owned pair item.</summary>
    public void EnsurePair(uint creature)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!IsPairItem(main)) return;
        if (IsLinkedPair(creature, main, off))
        {
            _pairs[creature] = (main, off);
            return;
        }
        if (GetIsObjectValid(off) || !string.IsNullOrEmpty(_item.GetCreatureItemUseError(creature, main)))
        {
            RemovePair(creature, main);
            return;
        }

        _changing.Add(creature);
        var spare = OBJECT_INVALID;
        try
        {
            // The equipped owner normally frees the same inventory footprint used by the spare.
            // Stage first so a failed transfer can never leave a usable gun on the ground.
            var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
            if (!GetIsObjectValid(storage)) throw new InvalidOperationException("Temporary item storage is unavailable.");
            spare = CreateItemOnObject(CompanionResref, storage);
            if (!GetIsObjectValid(spare)) throw new InvalidOperationException("The temporary off-hand pistol could not be created.");
            SetLocalInt(spare, SpareVariable, 1);
            SetLocalInt(spare, Domain.Items.ItemService.NoEconomyVariable, 1);
            SetLocalObject(spare, MainWeaponVariable, main);
            SetLocalObject(spare, OwnerVariable, creature);
            SetName(spare, GetName(main) + " (Off Hand)");
            SetPlotFlag(spare, true);
            SetDroppableFlag(spare, false);
            SetPickpocketableFlag(spare, false);
            for (var ip = GetFirstItemProperty(main); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(main))
                if (IsMirroredProperty(GetItemPropertyType(ip))) AddItemProperty(DurationType.Permanent, ip, spare);
            CopyAppearance(main, spare);

            _pairs[creature] = (main, spare);
            SetLocalObject(main, CompanionVariable, spare);
            if (!MoveSpareIntoOwnedStorage(creature, spare, creature) || GetItemPossessor(spare) != creature)
                throw new InvalidOperationException("Make room in your inventory before equipping Twin Pistols.");
            // The engine adds an item's weight when it is equipped and subtracts its weight at removal,
            // and property and storage changes restore the base item's weight. Zero it last, before equipping,
            // and never change it while equipped, so both sides of the wearer's weight total match.
            ItemPlugin.SetWeight(spare, 0);
            if (!CreaturePlugin.RunEquip(creature, spare, InventorySlot.LeftHand) ||
                GetItemInSlot(InventorySlot.LeftHand, creature) != spare)
                throw new InvalidOperationException("Make room in your inventory before equipping Twin Pistols.");
            // Native PC acquisition/equip normalizes loot flags, so apply the temporary flags afterward.
            SetDroppableFlag(spare, false);
            SetPickpocketableFlag(spare, false);
            SetPlotFlag(spare, true);
        }
        catch (InvalidOperationException ex)
        {
            DestroySpare(spare);
            _pairs.Remove(creature);
            DeleteLocalObject(main, CompanionVariable);
            SendMessageToPC(MessageRecipient(creature), ColorToken.Red(ex.Message));
        }
        finally { _changing.Remove(creature); }

        // Failed generation must not leave the owned pair operating as a single pistol.
        if (!IsActivePair(creature)) RemovePair(creature, main);
    }

    /// <summary>The generated gun has no appearance of its own; edits go to the owned pistol it mirrors.</summary>
    public uint ResolveAppearanceOwner(uint creature, uint item)
    {
        if (!IsGeneratedCompanion(item)) return item;
        var main = GetLocalObject(item, MainWeaponVariable);
        return IsLinkedPair(creature, main, item) ? main : OBJECT_INVALID;
    }

    /// <summary>Copies the owned pistol's model parts and colors onto its equipped generated gun.</summary>
    public void RefreshAppearance(uint creature)
    {
        if (!GetIsObjectValid(creature) || _refreshingAppearance.Contains(creature)) return;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var spare = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!IsLinkedPair(creature, main, spare) || IsDestroyingCompanion(spare)) return;
        _refreshingAppearance.Add(creature);
        try
        {
            // Invalidate observer caches for the same-ID held item; the nested tint pass cannot re-enter.
            if (CopyAppearance(main, spare)) RefreshEquippedItemAppearance(creature, spare);
        }
        finally { _refreshingAppearance.Remove(creature); }
    }

    private bool CopyAppearance(uint main, uint spare)
    {
        var changed = false;
        foreach (var part in new[] { AppearanceWeapon.Bottom, AppearanceWeapon.Middle, AppearanceWeapon.Top })
        foreach (var type in new[] { ItemAppearanceType.WeaponColor, ItemAppearanceType.WeaponModel })
        {
            var value = GetItemAppearance(main, type, (int)part);
            if (GetItemAppearance(spare, type, (int)part) == value) continue;
            ItemPlugin.SetItemAppearance(spare, type, (int)part, value, updateCreatureAppearance: false);
            changed = true;
        }
        return changed;
    }

    /// <summary>Safely returns the owned item and destroys only its generated second gun.</summary>
    public bool RemovePair(uint creature, uint removedItem = OBJECT_INVALID)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return true;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!_pairs.TryGetValue(creature, out var pair))
        {
            if (!IsPairItem(main) && !IsGeneratedCompanion(off)) return true;
            pair = (IsPairItem(main) ? main : GetLocalObject(off, MainWeaponVariable), off);
        }
        if (GetIsObjectValid(removedItem) && removedItem != pair.Main && removedItem != pair.Spare) return true;

        _changing.Add(creature);
        try
        {
            // Reserve space for the real item before changing either hand. Failure retains the pair.
            if (GetIsObjectValid(pair.Main) && GetItemInSlot(InventorySlot.RightHand, creature) == pair.Main)
            {
                // Every owner, NPCs included, keeps the real item equipped when storage is full.
                if (!TryUnequip(creature, pair.Main, InventorySlot.RightHand, allowRecoveryBag: false))
                    throw new InvalidOperationException("No owned storage can hold the Twin Pistols item.");
            }
            DestroySpare(pair.Spare);
            if (GetIsObjectValid(pair.Main)) DeleteLocalObject(pair.Main, CompanionVariable);
            _pairs.Remove(creature);
            return true;
        }
        catch (InvalidOperationException)
        {
            SendMessageToPC(MessageRecipient(creature), ColorToken.Red("Free inventory space or make room in a carried bag before unequipping Twin Pistols."));
            return false;
        }
        finally { _changing.Remove(creature); }
    }

    /// <summary>Saved spare objects are disposable; rebuild exactly one from the equipped owner.</summary>
    public void RestorePair(uint creature)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return;
        ReleaseCompanions(creature);
        DelayCommand(0.1f, () => EnsurePair(creature));
    }

    /// <summary>Exit cleanup retains the owned equipped item for character saving.</summary>
    public void ReleaseCompanions(uint creature)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return;
        _changing.Add(creature);
        try
        {
            var spares = new HashSet<uint>();
            var off = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (IsGeneratedCompanion(off)) spares.Add(off);
            CollectSpares(creature, spares);
            foreach (var spare in spares) DestroySpare(spare);
            _pairs.Remove(creature);
        }
        finally { _changing.Remove(creature); }
    }

    /// <summary>Destroyed NPC owners never run module-exit cleanup; drop their cached pair.</summary>
    public void ReleaseDestroyedOwner(uint creature)
    {
        _changing.Remove(creature);
        if (!_pairs.Remove(creature, out var pair)) return;
        if (IsPairItem(pair.Main) && GetLocalObject(pair.Main, CompanionVariable) == pair.Spare)
            DeleteLocalObject(pair.Main, CompanionVariable);
        if (IsGeneratedCompanion(pair.Spare) && GetLocalObject(pair.Spare, OwnerVariable) == creature)
            DestroySpare(pair.Spare);
    }

    public void DestroySpare(uint spare)
    {
        if (!IsGeneratedCompanion(spare)) return;
        // DestroyObject completes after this script; its later loss event is cleanup, not a new unequip request.
        SetLocalInt(spare, DestroyingVariable, 1);
        SetPlotFlag(spare, false);
        DestroyObject(spare);
    }

    private void CollectSpares(uint container, HashSet<uint> spares)
    {
        var children = new List<uint>();
        for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            children.Add(item);
        foreach (var item in children)
        {
            if (IsGeneratedCompanion(item)) spares.Add(item);
            else if (GetHasInventory(item)) CollectSpares(item, spares);
        }
    }

    private bool MoveSpareIntoOwnedStorage(uint creature, uint spare, uint container)
    {
        if (ItemPlugin.MoveTo(spare, container, true)) return true;
        var bags = new List<uint>();
        for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            if (GetHasInventory(item) && GetItemPossessor(item) == creature) bags.Add(item);
        foreach (var bag in bags)
            if (MoveSpareIntoOwnedStorage(creature, spare, bag)) return true;
        return false;
    }

    public bool IsPair(BaseItem? mainHandType, BaseItem? offHandType) =>
        (mainHandType is BaseItem.Pistol or BaseItem.Sling) &&
        offHandType == BaseItem.CompanionPistol;

    public string GetPairCompatibilityError(BaseItem? mainHandType, BaseItem? offHandType) =>
        mainHandType == BaseItem.LegacyPistol && offHandType == BaseItem.CompanionPistol
            ? "Legacy pistols cannot be paired with a companion pistol." : string.Empty;

    public (uint MainHand, uint OffHand) GetProspectiveHands(uint creature, uint item, InventorySlot slot)
    {
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (slot == InventorySlot.RightHand) main = item;
        if (slot == InventorySlot.LeftHand) off = item;
        return (main, off);
    }

    public bool IsActivePair(uint creature)
    {
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!GetIsObjectValid(main) || !GetIsObjectValid(off) ||
            !IsLinkedPair(creature, main, off))
            return false;

        if (!string.IsNullOrEmpty(GetCompanionPropertyError(off))) return false;
        return string.IsNullOrEmpty(_item.GetCreatureItemUseError(creature, main, SkillType.TwinPistol)) &&
               string.IsNullOrEmpty(_item.GetCreatureItemUseError(creature, off));
    }

    /// <summary>The simulated hand, available only when both equipped items satisfy the pair's requirements.</summary>
    public uint GetCompanionWeapon(uint creature) => IsActivePair(creature)
        ? GetItemInSlot(InventorySlot.LeftHand, creature)
        : OBJECT_INVALID;

    /// <summary>Only a paired main-hand pistol's stored Pistol requirement is reinterpreted.</summary>
    public SkillType ResolveRequirementSkill(BaseItem itemType, SkillType requirement, bool paired) =>
        paired && requirement == SkillType.Pistol && IsPair(itemType, BaseItem.CompanionPistol)
            ? SkillType.TwinPistol
            : requirement;

    /// <summary>Pure classification for an already validated arrangement; explicit ability identities win.</summary>
    public SkillType ResolveSkill(BaseItem weaponType, BaseItem? mainHandType, BaseItem? offHandType,
        SkillType? explicitOverride = null)
    {
        if (explicitOverride.HasValue && explicitOverride != SkillType.Invalid) return explicitOverride.Value;
        if (IsPair(mainHandType, offHandType) &&
            (weaponType == mainHandType || weaponType == BaseItem.CompanionPistol)) return SkillType.TwinPistol;
        return weaponType == BaseItem.CompanionPistol ? SkillType.Invalid : GetSkillTypeByBaseItem(weaponType);
    }

    /// <summary>Companion properties are profile data only; no native or creature-wide equipped bonuses.</summary>
    public bool IsSupportedCompanionProperty(ItemPropertyType type) => type is
        ItemPropertyType.DMG or ItemPropertyType.Delay or ItemPropertyType.Accuracy or
        ItemPropertyType.AccuracyStat or ItemPropertyType.DamageStat or ItemPropertyType.WeaponDamageType or
        ItemPropertyType.RequiresSkill or ItemPropertyType.RequiresStat or ItemPropertyType.UseLimitationPerk or
        ItemPropertyType.WeaponEnhancement or ItemPropertyType.EnhancementLevel or
        ItemPropertyType.IncreasedPrice or ItemPropertyType.ProgressPenalty or ItemPropertyType.Blueprint;

    public string GetCompanionPropertyError(uint item)
    {
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            if (!IsSupportedCompanionProperty(GetItemPropertyType(ip)))
                return "Companion pistols cannot use native or creature-wide equipment bonuses.";
        return string.Empty;
    }

    /// <summary>The unconverted <c>Droid</c> service.</summary>
    protected virtual bool IsDroid(uint creature) => _droid.Value.IsDroid(creature);

    /// <summary>The equipment storage that returns an item to the owner's inventory.</summary>
    protected virtual bool TryUnequip(uint creature, uint item, InventorySlot slot, bool allowRecoveryBag) =>
        PlayerEquipmentStorage.TryUnequip(creature, item, slot, allowRecoveryBag: allowRecoveryBag);

    /// <summary>The held-item appearance refresh for observers.</summary>
    protected virtual void RefreshEquippedItemAppearance(uint creature, uint item) =>
        EquippedItemAppearance.Refresh(creature, item);

    /// <summary>The unconverted <c>Skill</c> service.</summary>
    protected virtual SkillType GetSkillTypeByBaseItem(BaseItem baseItem) => _skill.Value.GetSkillTypeByBaseItem(baseItem);
}
