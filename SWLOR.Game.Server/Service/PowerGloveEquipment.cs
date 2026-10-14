using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using ItemProperty = SWLOR.NWN.API.Engine.ItemProperty;

namespace SWLOR.Game.Server.Service;

/// <summary>One owned Power Glove supplies the weapon; its other hand is disposable appearance only.</summary>
public static class PowerGloveEquipment
{
    public const string SpareVariable = "POWER_GLOVE_SPARE";
    public const string MainWeaponVariable = "POWER_GLOVE_MAIN";
    public const string OwnerVariable = "POWER_GLOVE_OWNER";
    public const string CompanionVariable = "POWER_GLOVE_COMPANION";
    private const string DestroyingVariable = "POWER_GLOVE_REMOVING";
    private const string CompanionResref = "pg_companion";
    private static readonly Dictionary<uint, (uint Main, uint Companion)> Pairs = new();
    private static readonly HashSet<uint> Changing = new();
    private static readonly HashSet<uint> RefreshingAppearance = new();
    internal static bool IsCreatingCompanion { get; private set; }
    private static readonly ObjectVisualTransformDataScopeType[] TransformScopes =
    {
        ObjectVisualTransformDataScopeType.Base,
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart1,
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart2,
        (ObjectVisualTransformDataScopeType)ObjectVisualTransformDataScopeItemType.ItemPart3
    };

    public static bool IsChanging(uint creature) => Changing.Contains(creature);

    public static bool IsPairItem(uint item) => GetIsObjectValid(item) &&
        GetObjectType(item) == ObjectType.Item && GetBaseItemType(item) == BaseItem.PowerGlove;

    // The dedicated internal type remains disposable even when saved local references are stale.
    public static bool IsGeneratedCompanion(uint item) => GetIsObjectValid(item) &&
        GetObjectType(item) == ObjectType.Item && GetBaseItemType(item) == BaseItem.CompanionPowerGlove;

    public static bool IsDestroyingCompanion(uint item) =>
        IsGeneratedCompanion(item) && GetLocalInt(item, DestroyingVariable) == 1;

    public static bool IsLinkedPair(uint creature, uint main, uint companion) =>
        GetIsObjectValid(creature) && IsPairItem(main) && IsGeneratedCompanion(companion) &&
        !IsDestroyingCompanion(companion) &&
        GetLocalObject(main, CompanionVariable) == companion &&
        GetLocalObject(companion, MainWeaponVariable) == main &&
        GetLocalObject(companion, OwnerVariable) == creature &&
        GetItemPossessor(main) == creature && GetItemPossessor(companion) == creature;

    internal static bool IsEquippedCompanion(uint companion)
    {
        if (!IsGeneratedCompanion(companion)) return false;
        var owner = GetLocalObject(companion, OwnerVariable);
        var main = GetLocalObject(companion, MainWeaponVariable);
        return IsLinkedPair(owner, main, companion) &&
               GetItemInSlot(InventorySlot.RightHand, owner) == main &&
               GetItemInSlot(InventorySlot.LeftHand, owner) == companion;
    }

    public static bool IsActivePair(uint creature)
    {
        if (!GetIsObjectValid(creature) || Droid.IsDroid(creature)) return false;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var companion = GetItemInSlot(InventorySlot.LeftHand, creature);
        return IsLinkedPair(creature, main, companion) &&
               !GetIsItemPropertyValid(GetFirstItemProperty(companion)) &&
               string.IsNullOrWhiteSpace(Item.GetCreatureItemUseError(creature, main));
    }

    public static uint GetCompanion(uint creature) => IsActivePair(creature)
        ? GetItemInSlot(InventorySlot.LeftHand, creature) : OBJECT_INVALID;

    public static uint ResolveAppearanceOwner(uint creature, uint item)
    {
        if (!IsGeneratedCompanion(item)) return item;
        var main = GetLocalObject(item, MainWeaponVariable);
        return IsLinkedPair(creature, main, item) ? main : OBJECT_INVALID;
    }

    /// <summary>Reconciles completed native equips, including failures, without cloning the real item.</summary>
    public static void EnsurePair(uint creature)
    {
        if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature || IsChanging(creature))
            return;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var companion = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!IsPairItem(main))
        {
            if (IsGeneratedCompanion(companion) || Pairs.ContainsKey(creature))
                RemovePair(creature);
            return;
        }
        if (Droid.IsDroid(creature) || !string.IsNullOrWhiteSpace(Item.GetCreatureItemUseError(creature, main)))
        {
            RemovePair(creature, main);
            return;
        }
        if (IsLinkedPair(creature, main, companion))
        {
            Pairs[creature] = (main, companion);
            SanitizeCompanion(companion);
            RefreshAppearance(creature);
            return;
        }
        if (GetIsObjectValid(companion))
        {
            RemovePair(creature, main);
            return;
        }

        // Dispose of a detached old cosmetic before generating its replacement.
        if (Pairs.Remove(creature, out var previous)) DestroyCompanion(previous.Companion);
        DeleteLocalObject(main, CompanionVariable);
        Changing.Add(creature);
        companion = OBJECT_INVALID;
        try
        {
            var storage = GetObjectByTag("TEMP_ITEM_STORAGE");
            if (!GetIsObjectValid(storage))
                throw new InvalidOperationException("Temporary item storage is unavailable.");
            IsCreatingCompanion = true;
            try { companion = CreateItemOnObject(CompanionResref, storage); }
            finally { IsCreatingCompanion = false; }
            if (!IsGeneratedCompanion(companion))
                throw new InvalidOperationException("The Power Glove cosmetic could not be created.");
            SetLocalObject(companion, MainWeaponVariable, main);
            SetLocalObject(companion, OwnerVariable, creature);
            SetLocalObject(main, CompanionVariable, companion);
            SetName(companion, GetName(main) + " (Other Hand)");
            SanitizeCompanion(companion);
            Pairs[creature] = (main, companion);
            if (!MoveIntoOwnedStorage(creature, companion, creature, new HashSet<uint>()))
                throw new InvalidOperationException("The other hand could not be stored safely.");
            CreaturePlugin.RunEquip(creature, companion, InventorySlot.LeftHand);
            if (GetItemInSlot(InventorySlot.LeftHand, creature) != companion ||
                GetItemPossessor(companion) != creature)
                throw new InvalidOperationException("The other hand could not be equipped safely.");
            // Acquiring an item can normalize its native loot flags.
            SanitizeCompanion(companion);
            RefreshAppearance(creature);
        }
        catch (InvalidOperationException)
        {
            DestroyCompanion(companion);
            Pairs.Remove(creature);
            DeleteLocalObject(main, CompanionVariable);
            SendMessageToPC(creature, ColorToken.Red("The Power Glove could not equip both hands. Free inventory or bag space and try again."));
        }
        finally { Changing.Remove(creature); }

        // A failed generated-hand equip must not leave a functioning lone weapon.
        if (!IsActivePair(creature)) RemovePair(creature, main);
    }

    /// <summary>Reserves space for the real item before either hand is removed.</summary>
    public static bool RemovePair(uint creature, uint removedItem = OBJECT_INVALID)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return false;
        if (!Pairs.TryGetValue(creature, out var pair))
        {
            var main = GetItemInSlot(InventorySlot.RightHand, creature);
            var companion = GetItemInSlot(InventorySlot.LeftHand, creature);
            if (!IsPairItem(main) && IsGeneratedCompanion(companion))
                main = GetLocalObject(companion, MainWeaponVariable);
            pair = (main, companion);
        }
        if (GetIsObjectValid(removedItem) && removedItem != pair.Main && removedItem != pair.Companion)
            return false;
        Changing.Add(creature);
        try
        {
            if (IsPairItem(pair.Main) && GetItemInSlot(InventorySlot.RightHand, creature) == pair.Main)
                UnequipOwnedItem(creature, pair.Main, InventorySlot.RightHand);
            DestroyCompanion(pair.Companion);
            if (IsPairItem(pair.Main)) DeleteLocalObject(pair.Main, CompanionVariable);
            Pairs.Remove(creature);
            return true;
        }
        catch (InvalidOperationException)
        {
            SendMessageToPC(creature, ColorToken.Red("Free inventory or bag space before removing the Power Glove."));
            return false;
        }
        finally { Changing.Remove(creature); }
    }

    /// <summary>Saved cosmetic objects are discarded; the owned weapon and its upgrades are retained.</summary>
    public static void RestorePair(uint creature)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return;
        ReleaseCompanions(creature);
        DelayCommand(.1f, () =>
        {
            if (!GetIsObjectValid(creature)) return;
            var main = GetItemInSlot(InventorySlot.RightHand, creature);
            var left = GetItemInSlot(InventorySlot.LeftHand, creature);
            // Native saves can contain an invalid hand combination. Preserve that real item too.
            if (IsPairItem(main) && GetIsObjectValid(left))
            {
                Changing.Add(creature);
                try { UnequipOwnedItem(creature, left, InventorySlot.LeftHand); }
                catch (InvalidOperationException)
                {
                    SendMessageToPC(creature, ColorToken.Red("Free inventory or bag space before equipping the Power Glove."));
                }
                finally { Changing.Remove(creature); }
            }
            EnsurePair(creature);
        });
    }

    public static void ReleaseCompanions(uint creature)
    {
        if (!GetIsObjectValid(creature) || IsChanging(creature)) return;
        Changing.Add(creature);
        try
        {
            var companions = new HashSet<uint>();
            if (Pairs.Remove(creature, out var pair))
            {
                companions.Add(pair.Companion);
                if (IsPairItem(pair.Main)) DeleteLocalObject(pair.Main, CompanionVariable);
            }
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                CollectCompanions(GetItemInSlot((InventorySlot)slot, creature), companions, new HashSet<uint>());
            CollectCompanions(creature, companions, new HashSet<uint>());
            foreach (var companion in companions)
            {
                var main = GetLocalObject(companion, MainWeaponVariable);
                if (IsPairItem(main) && GetItemPossessor(main) == creature)
                    DeleteLocalObject(main, CompanionVariable);
                DestroyCompanion(companion);
            }
        }
        finally { Changing.Remove(creature); }
    }

    public static void DestroyCompanion(uint companion)
    {
        if (!IsGeneratedCompanion(companion) || IsDestroyingCompanion(companion)) return;
        SetLocalInt(companion, DestroyingVariable, 1);
        SetPlotFlag(companion, false);
        DestroyObject(companion);
    }

    /// <summary>Copies only appearance state; the companion never receives gameplay properties.</summary>
    public static void RefreshAppearance(uint creature)
    {
        if (!GetIsObjectValid(creature) || RefreshingAppearance.Contains(creature)) return;
        var main = GetItemInSlot(InventorySlot.RightHand, creature);
        var companion = GetItemInSlot(InventorySlot.LeftHand, creature);
        if (!IsLinkedPair(creature, main, companion)) return;
        RefreshingAppearance.Add(creature);
        try
        {
            var changedAppearance = false;
            foreach (var part in new[] { AppearanceWeapon.Bottom, AppearanceWeapon.Middle, AppearanceWeapon.Top })
            foreach (var type in new[] { ItemAppearanceType.WeaponColor, ItemAppearanceType.WeaponModel })
            {
                var value = GetItemAppearance(main, type, (int)part);
                if (GetItemAppearance(companion, type, (int)part) == value) continue;
                ItemPlugin.SetItemAppearance(companion, type, (int)part, value, updateCreatureAppearance: false);
                changedAppearance = true;
            }
            if (GetHiddenWhenEquipped(companion) != GetHiddenWhenEquipped(main))
            {
                SetHiddenWhenEquipped(companion, GetHiddenWhenEquipped(main) != 0);
                changedAppearance = true;
            }
            foreach (var scope in TransformScopes)
            foreach (var transform in Enum.GetValues<ObjectVisualTransform>())
            {
                var value = GetObjectVisualTransform(main, transform, nScope: scope);
                if (Math.Abs(GetObjectVisualTransform(companion, transform, nScope: scope) - value) > .0001f)
                    SetObjectVisualTransform(companion, transform, value, nScope: scope);
            }
            // Invalidate this same-ID item's observer caches after changing its held model.
            // TintMap's nested fit pass may run normally, but cannot enter this sync twice.
            if (changedAppearance) EquippedItemAppearance.Refresh(creature, companion);
        }
        finally { RefreshingAppearance.Remove(creature); }
    }

    public static void SanitizeCompanion(uint companion)
    {
        if (!IsGeneratedCompanion(companion) || IsDestroyingCompanion(companion)) return;
        SetLocalInt(companion, SpareVariable, 1);
        SetLocalInt(companion, Item.NoEconomyVariable, 1);
        SetPlotFlag(companion, true);
        SetDroppableFlag(companion, false);
        SetPickpocketableFlag(companion, false);
        ItemPlugin.SetWeight(companion, 0);
        ItemPlugin.SetBaseGoldPieceValue(companion, 0);
        ItemPlugin.SetAddGoldPieceValue(companion, 0);
        var properties = new List<ItemProperty>();
        for (var ip = GetFirstItemProperty(companion); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(companion))
            properties.Add(ip);
        foreach (var property in properties) RemoveItemProperty(companion, property);
    }

    internal static void CollectCompanions(uint container, HashSet<uint> companions, HashSet<uint> visited)
    {
        if (!GetIsObjectValid(container) || !visited.Add(container)) return;
        if (IsGeneratedCompanion(container)) companions.Add(container);
        if (!GetHasInventory(container)) return;
        var contents = new List<uint>();
        for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            contents.Add(item);
        foreach (var item in contents) CollectCompanions(item, companions, visited);
    }

    private static bool MoveIntoOwnedStorage(uint creature, uint companion, uint container, HashSet<uint> visited)
    {
        if (!visited.Add(container)) return false;
        if (ItemPlugin.MoveTo(companion, container, true) && GetItemPossessor(companion) == creature) return true;
        var contents = new List<uint>();
        for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            if (GetHasInventory(item) && GetItemPossessor(item) == creature) contents.Add(item);
        foreach (var bag in contents)
            if (MoveIntoOwnedStorage(creature, companion, bag, visited)) return true;
        return false;
    }

    private static void UnequipOwnedItem(uint creature, uint item, InventorySlot slot)
    {
        if (GetIsPC(creature)) PlayerEquipmentStorage.Unequip(creature, item, slot);
        else
        {
            CreaturePlugin.RunUnequip(creature, item);
            if (GetItemInSlot(slot, creature) == item || GetItemPossessor(item) != creature)
                throw new InvalidOperationException("The real equipment could not be preserved.");
        }
    }
}
