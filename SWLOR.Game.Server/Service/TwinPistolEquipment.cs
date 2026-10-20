using System.Collections.Generic;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service;

/// <summary>Validates the simulated companion without treating its native carrier as a melee weapon.</summary>
public static class TwinPistolEquipment
{
    public const string PairVariable = "TWIN_PISTOL_PAIR";
    public const string SpareVariable = "TWIN_PISTOL_SPARE";
    public const string MainWeaponVariable = "TWIN_PISTOL_MAIN";
    public const string OwnerVariable = "TWIN_PISTOL_OWNER";
    private const string DestroyingVariable = "TWIN_PISTOL_REMOVING";
    private const string CompanionVariable = "TWIN_PISTOL_COMPANION";
    private const string CompanionResref = "twp_companion";
    private static readonly Dictionary<uint, (uint Main, uint Spare)> Pairs = new();
    private static readonly HashSet<uint> Changing = new();

    public static bool IsChanging(uint creature) => Changing.Contains(creature);

    // A droid has no client; its controlling player gets the explanation.
    private static uint MessageRecipient(uint creature) => Droid.IsDroid(creature) ? GetMaster(creature) : creature;

    public static bool IsPairItem(uint item) => GetIsObjectValid(item) &&
        IsPairItem(GetBaseItemType(item), GetLocalInt(item, PairVariable) == 1);

    public static bool IsPairItem(BaseItem type, bool marked) =>
        marked && type is BaseItem.Pistol or BaseItem.Sling;

    public static bool IsGeneratedCompanion(uint item) => GetIsObjectValid(item) &&
        GetBaseItemType(item) == BaseItem.CompanionPistol && GetLocalInt(item, SpareVariable) == 1;

    public static bool IsDestroyingCompanion(uint item) =>
        IsGeneratedCompanion(item) && GetLocalInt(item, DestroyingVariable) == 1;

    public static bool IsLinkedPair(uint creature, uint main, uint off) =>
        IsPairItem(main) && IsGeneratedCompanion(off) &&
        GetLocalObject(off, MainWeaponVariable) == main && GetLocalObject(off, OwnerVariable) == creature &&
        GetItemPossessor(main) == creature && GetItemPossessor(off) == creature;

    /// <summary>Includes the required rank used by native weapon attribute-delta caps.</summary>
    public static bool IsMirroredProperty(ItemPropertyType type) => type is
        ItemPropertyType.DMG or ItemPropertyType.Delay or ItemPropertyType.Accuracy or
        ItemPropertyType.AccuracyStat or ItemPropertyType.DamageStat or ItemPropertyType.WeaponDamageType or
        ItemPropertyType.RequiresSkill;

    /// <summary>Creates the disposable visual/weapon profile for one owned pair item.</summary>
    public static void EnsurePair(uint creature)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!IsPairItem(main)) return;
        if (IsLinkedPair(creature, main, off))
        {
            Pairs[creature] = (main, off);
            return;
        }
        if (GetIsObjectValid(off) || !string.IsNullOrEmpty(Item.GetCreatureItemUseError(creature, main)))
        {
            RemovePair(creature, main);
            return;
        }

        Changing.Add(creature);
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
            SetLocalInt(spare, Item.NoEconomyVariable, 1);
            SetLocalObject(spare, MainWeaponVariable, main);
            SetLocalObject(spare, OwnerVariable, creature);
            SetName(spare, GetName(main) + " (Off Hand)");
            SetPlotFlag(spare, true);
            SetDroppableFlag(spare, false);
            SetPickpocketableFlag(spare, false);
            ItemPlugin.SetWeight(spare, 0);
            for (var ip = GetFirstItemProperty(main); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(main))
                if (IsMirroredProperty(GetItemPropertyType(ip))) AddItemProperty(DurationType.Permanent, ip, spare);

            Pairs[creature] = (main, spare);
            SetLocalObject(main, CompanionVariable, spare);
            if (!MoveSpareIntoOwnedStorage(creature, spare, creature) || GetItemPossessor(spare) != creature ||
                !CreaturePlugin.RunEquip(creature, spare, InventorySlot.LeftHand) ||
                GetItemInSlot(InventorySlot.LeftHand, creature) != spare)
                throw new InvalidOperationException("Make room in your inventory before equipping Twin Pistols.");
            // Native PC acquisition/equip normalizes loot flags, so apply the temporary flags afterward.
            SetDroppableFlag(spare, false);
            SetPickpocketableFlag(spare, false);
            SetPlotFlag(spare, true);
            ItemPlugin.SetWeight(spare, 0);
        }
        catch (InvalidOperationException ex)
        {
            DestroySpare(spare);
            Pairs.Remove(creature);
            DeleteLocalObject(main, CompanionVariable);
            SendMessageToPC(MessageRecipient(creature), ColorToken.Red(ex.Message));
        }
        finally { Changing.Remove(creature); }

        // Failed generation must not leave the owned pair operating as a single pistol.
        if (!IsActivePair(creature)) RemovePair(creature, main);
    }

    /// <summary>Safely returns the owned item and destroys only its generated second gun.</summary>
    public static bool RemovePair(uint creature, uint removedItem = OBJECT_INVALID)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return true;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!Pairs.TryGetValue(creature, out var pair))
        {
            if (!IsPairItem(main) && !IsGeneratedCompanion(off)) return true;
            pair = (IsPairItem(main) ? main : GetLocalObject(off, MainWeaponVariable), off);
        }
        if (GetIsObjectValid(removedItem) && removedItem != pair.Main && removedItem != pair.Spare) return true;

        Changing.Add(creature);
        try
        {
            // Reserve space for the real item before changing either hand. Failure retains the pair.
            if (GetIsObjectValid(pair.Main) && GetItemInSlot(InventorySlot.RightHand, creature) == pair.Main)
            {
                // Every owner, NPCs included, keeps the real item equipped when storage is full.
                if (!PlayerEquipmentStorage.TryUnequip(creature, pair.Main, InventorySlot.RightHand, allowRecoveryBag: false))
                    throw new InvalidOperationException("No owned storage can hold the Twin Pistols item.");
            }
            DestroySpare(pair.Spare);
            if (GetIsObjectValid(pair.Main)) DeleteLocalObject(pair.Main, CompanionVariable);
            Pairs.Remove(creature);
            return true;
        }
        catch (InvalidOperationException)
        {
            SendMessageToPC(MessageRecipient(creature), ColorToken.Red("Free inventory space or make room in a carried bag before unequipping Twin Pistols."));
            return false;
        }
        finally { Changing.Remove(creature); }
    }

    /// <summary>Saved spare objects are disposable; rebuild exactly one from the equipped owner.</summary>
    public static void RestorePair(uint creature)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return;
        ReleaseCompanions(creature);
        DelayCommand(0.1f, () => EnsurePair(creature));
    }

    /// <summary>Exit cleanup retains the owned equipped item for character saving.</summary>
    public static void ReleaseCompanions(uint creature)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return;
        Changing.Add(creature);
        try
        {
            var spares = new HashSet<uint>();
            var off = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (IsGeneratedCompanion(off)) spares.Add(off);
            CollectSpares(creature, spares);
            foreach (var spare in spares) DestroySpare(spare);
            Pairs.Remove(creature);
        }
        finally { Changing.Remove(creature); }
    }

    /// <summary>Destroyed NPC owners never run module-exit cleanup; drop their cached pair.</summary>
    public static void ReleaseDestroyedOwner(uint creature)
    {
        Changing.Remove(creature);
        if (!Pairs.Remove(creature, out var pair)) return;
        if (IsPairItem(pair.Main) && GetLocalObject(pair.Main, CompanionVariable) == pair.Spare)
            DeleteLocalObject(pair.Main, CompanionVariable);
        if (IsGeneratedCompanion(pair.Spare) && GetLocalObject(pair.Spare, OwnerVariable) == creature)
            DestroySpare(pair.Spare);
    }

    public static void DestroySpare(uint spare)
    {
        if (!IsGeneratedCompanion(spare)) return;
        // DestroyObject completes after this script; its later loss event is cleanup, not a new unequip request.
        SetLocalInt(spare, DestroyingVariable, 1);
        SetPlotFlag(spare, false);
        DestroyObject(spare);
    }

    private static void CollectSpares(uint container, HashSet<uint> spares)
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

    private static bool MoveSpareIntoOwnedStorage(uint creature, uint spare, uint container)
    {
        if (ItemPlugin.MoveTo(spare, container, true)) return true;
        var bags = new List<uint>();
        for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            if (GetHasInventory(item) && GetItemPossessor(item) == creature) bags.Add(item);
        foreach (var bag in bags)
            if (MoveSpareIntoOwnedStorage(creature, spare, bag)) return true;
        return false;
    }

    public static bool IsPair(BaseItem? mainHandType, BaseItem? offHandType) =>
        (mainHandType is BaseItem.Pistol or BaseItem.Sling) &&
        offHandType == BaseItem.CompanionPistol;

    public static string GetPairCompatibilityError(BaseItem? mainHandType, BaseItem? offHandType) =>
        mainHandType == BaseItem.LegacyPistol && offHandType == BaseItem.CompanionPistol
            ? "Legacy pistols cannot be paired with a companion pistol." : string.Empty;

    public static (uint MainHand, uint OffHand) GetProspectiveHands(uint creature, uint item, InventorySlot slot)
    {
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (slot == InventorySlot.RightHand) main = item;
        if (slot == InventorySlot.LeftHand) off = item;
        return (main, off);
    }

    public static bool IsActivePair(uint creature)
    {
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var off = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!GetIsObjectValid(main) || !GetIsObjectValid(off) ||
            !IsLinkedPair(creature, main, off))
            return false;

        if (!string.IsNullOrEmpty(GetCompanionPropertyError(off))) return false;
        return string.IsNullOrEmpty(Item.GetCreatureItemUseError(creature, main, SkillType.TwinPistol)) &&
               string.IsNullOrEmpty(Item.GetCreatureItemUseError(creature, off));
    }

    /// <summary>The simulated hand, available only when both equipped items satisfy the pair's requirements.</summary>
    public static uint GetCompanionWeapon(uint creature) => IsActivePair(creature)
        ? GetItemInSlot(InventorySlot.LeftHand, creature)
        : OBJECT_INVALID;

    /// <summary>Only a paired main-hand pistol's stored Pistol requirement is reinterpreted.</summary>
    public static SkillType ResolveRequirementSkill(BaseItem itemType, SkillType requirement, bool paired) =>
        paired && requirement == SkillType.Pistol && IsPair(itemType, BaseItem.CompanionPistol)
            ? SkillType.TwinPistol
            : requirement;

    /// <summary>Pure classification for an already validated arrangement; explicit ability identities win.</summary>
    public static SkillType ResolveSkill(BaseItem weaponType, BaseItem? mainHandType, BaseItem? offHandType,
        SkillType? explicitOverride = null)
    {
        if (explicitOverride.HasValue && explicitOverride != SkillType.Invalid) return explicitOverride.Value;
        if (IsPair(mainHandType, offHandType) &&
            (weaponType == mainHandType || weaponType == BaseItem.CompanionPistol)) return SkillType.TwinPistol;
        return weaponType == BaseItem.CompanionPistol ? SkillType.Invalid : Skill.GetSkillTypeByBaseItem(weaponType);
    }

    /// <summary>Companion properties are profile data only; no native or creature-wide equipped bonuses.</summary>
    public static bool IsSupportedCompanionProperty(ItemPropertyType type) => type is
        ItemPropertyType.DMG or ItemPropertyType.Delay or ItemPropertyType.Accuracy or
        ItemPropertyType.AccuracyStat or ItemPropertyType.DamageStat or ItemPropertyType.WeaponDamageType or
        ItemPropertyType.RequiresSkill or ItemPropertyType.RequiresStat or ItemPropertyType.UseLimitationPerk or
        ItemPropertyType.WeaponEnhancement or ItemPropertyType.EnhancementLevel or
        ItemPropertyType.IncreasedPrice or ItemPropertyType.ProgressPenalty or ItemPropertyType.Blueprint;

    public static string GetCompanionPropertyError(uint item)
    {
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            if (!IsSupportedCompanionProperty(GetItemPropertyType(ip)))
                return "Companion pistols cannot use native or creature-wide equipment bonuses.";
        return string.Empty;
    }
}
