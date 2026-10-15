using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Moves loot created before the loot stat revision onto the revised blueprint values. Each revised
/// property shifts by the blueprint difference, so crafted enhancements merged into it are kept.
/// Revised blueprints carry the revision local, which also marks every migrated item. Droids are covered
/// both as saved controller data and as summoned creatures, whose items are saved back onto the controller.
/// </summary>
public static class LootStatRevisionMigration
{
    public const string RevisionVariable = "ITEM_STATS_REVISION";
    public const int CurrentRevision = 1;
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire()
    {
        var owner = GetModuleItemAcquiredBy();
        var item = GetModuleItemAcquired();
        if (GetIsPC(owner) && !GetIsDM(owner) && !GetIsDMPossessed(owner))
            MigrateObject(item);
        else if (Droid.IsDroid(owner) && MigrateObject(item))
            Droid.SaveItemSnapshot(owner);
    }

    /// <summary>
    /// Revises a player's equipped and carried items and their summoned droid, a summoned droid,
    /// an item, or a container and its contents.
    /// </summary>
    public static bool MigrateObject(uint obj)
    {
        if (!GetIsObjectValid(obj))
            return false;

        var objectType = GetObjectType(obj);
        if (objectType == ObjectType.Creature)
        {
            if (Droid.IsDroid(obj))
                return MigrateSummonedDroid(obj);
            // Stored DM creatures, DM-possessed creatures and live NPCs keep their authored gear.
            if (!GetIsPC(obj) || GetIsDM(obj) || GetIsDMPossessed(obj))
                return false;
            return MigratePlayer(obj);
        }

        var changed = false;
        if (objectType == ObjectType.Item)
        {
            changed |= MigrateItem(obj);
            changed |= MigrateConstructedDroid(obj);
        }
        changed |= MigrateContents(obj);
        return changed;
    }

    private static bool MigratePlayer(uint player)
    {
        var pools = CapturePools(player);
        var changed = false;
        for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            changed |= MigrateObject(GetItemInSlot((InventorySlot)slot, player));
        changed |= MigrateSummonedDroid(Droid.GetDroid(player));
        changed |= MigrateContents(player);
        if (changed)
            RestorePools(player, pools);
        return changed;
    }

    private static bool MigrateContents(uint obj)
    {
        var changed = false;
        if (GetHasInventory(obj))
            for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                changed |= MigrateObject(item);
        return changed;
    }

    /// <summary>
    /// Records current HP, FP and STM before equipped items are revised. Each item's difference is applied
    /// on its own, so an item that lowers a maximum can clamp a pool that a later item raises again.
    /// </summary>
    private static (int HP, int FP, int Stamina) CapturePools(uint creature)
    {
        var dbPlayer = GetIsPC(creature) ? DB.Get<Player>(GetObjectUUID(creature)) : null;
        return (ObjectPlugin.GetCurrentHitPoints(creature), dbPlayer?.FP ?? 0, dbPlayer?.Stamina ?? 0);
    }

    /// <summary>Gives back any HP, FP or STM lost to an intermediate cap, never above the final maximum.</summary>
    private static void RestorePools(uint creature, (int HP, int FP, int Stamina) pools)
    {
        if (ObjectPlugin.GetCurrentHitPoints(creature) < pools.HP)
            SetCurrentHitPoints(creature, Math.Min(pools.HP, GetMaxHitPoints(creature)));

        if (!GetIsPC(creature))
            return;
        var dbPlayer = DB.Get<Player>(GetObjectUUID(creature));
        if (dbPlayer == null)
            return;
        if (dbPlayer.FP < pools.FP)
            dbPlayer.FP = Math.Min(pools.FP, Stat.GetMaxFP(creature, dbPlayer));
        if (dbPlayer.Stamina < pools.Stamina)
            dbPlayer.Stamina = Math.Min(pools.Stamina, Stat.GetMaxStamina(creature, dbPlayer));
        DB.Set(dbPlayer);
    }

    /// <summary>Preserves saved identities and unchanged payloads, releasing every temporary native object.</summary>
    public static bool MigrateSerializedObject(string original, out string migrated)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original))
            return false;
        if (StoredObjectData.IsCreatureData(original))
            return false;
        var inventory = StoredObjectData.ReadInventory(original);
        var obj = MigrationObject.Deserialize(inventory?.PrepareForNativeLoad(null) ?? original);
        try
        {
            if (!MigrateObject(obj))
                return false;
            migrated = MigrationObject.Serialize(obj, original);
            if (inventory != null)
                migrated = inventory.CopyMigratedInventory(migrated);
            return true;
        }
        finally
        {
            MigrationObject.DestroyTemporaryObject(obj);
        }
    }

    private static bool MigrateItem(uint item)
    {
        if (!LootStatRevisionCatalog.Changes.TryGetValue(GetResRef(item), out var changes) ||
            GetLocalInt(item, RevisionVariable) >= CurrentRevision)
            return false;

        // An equipped item's stats are already counted on its wearer (a player's record or a droid's
        // skin). Apply only each stat's difference there: removing every old stat first would clamp
        // current HP, FP and STM to a temporarily lower maximum.
        var wearer = GetWearer(item);
        foreach (var change in changes)
        {
            var difference = ApplyChange(item, change);
            if (difference != 0 && GetIsObjectValid(wearer))
                EquipmentStats.ApplyStatAmount(wearer, item, change.Type, PropertySubtype(change),
                    Math.Abs(difference), difference > 0);
        }
        SetLocalInt(item, RevisionVariable, CurrentRevision);

        if (GetIsObjectValid(wearer))
            Stat.RefreshCombatReadinessEquipment(wearer);
        return true;
    }

    /// <summary>Shifts one property by the blueprint difference and returns how much the item's total changed.</summary>
    private static int ApplyChange(uint item, LootStatChange change)
    {
        // Collect first: removing a property mid-iteration skips the next one.
        var matches = new List<ItemProperty>();
        var total = 0;
        for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
        {
            if (GetItemPropertyType(ip) != change.Type ||
                GetItemPropertyDurationType(ip) != DurationType.Permanent ||
                change.Type == ItemPropertyType.Defense && GetItemPropertySubType(ip) != change.Subtype)
                continue;
            total += GetItemPropertyCostTableValue(ip);
            matches.Add(ip);
        }

        var revised = total - change.Previous + change.Current;
        if (revised < 0)
            revised = 0;
        if (revised == total && matches.Count <= 1)
            return 0;

        foreach (var ip in matches)
            MigrationObject.RemoveProperty(item, ip);
        if (revised > 0)
            MigrationObject.AddProperty(item, ItemPropertyCustom(change.Type, PropertySubtype(change), revised), AddItemPropertyPolicy.IgnoreExisting);
        return revised - total;
    }

    private static int PropertySubtype(LootStatChange change) =>
        change.Type == ItemPropertyType.Defense ? change.Subtype : -1;

    private static uint GetWearer(uint item)
    {
        var possessor = GetItemPossessor(item);
        var isPlayer = GetIsPC(possessor) && !GetIsDM(possessor) && !GetIsDMPossessed(possessor);
        if (!isPlayer && !Droid.IsDroid(possessor))
            return OBJECT_INVALID;
        for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            if (GetItemInSlot((InventorySlot)slot, possessor) == item)
                return possessor;
        return OBJECT_INVALID;
    }

    /// <summary>
    /// Revises a summoned droid's equipped and carried items, then saves them onto its controller.
    /// The droid's next snapshot rebuilds the controller's item lists from what it holds, so the
    /// live items must be revised rather than only the saved copies.
    /// </summary>
    private static bool MigrateSummonedDroid(uint droid)
    {
        if (!GetIsObjectValid(droid) || !Droid.IsDroid(droid))
            return false;

        var pools = CapturePools(droid);
        var changed = false;
        for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            changed |= MigrateItem(GetItemInSlot((InventorySlot)slot, droid));
        changed |= MigrateContents(droid);
        if (!changed)
            return false;
        RestorePools(droid, pools);
        Droid.SaveItemSnapshot(droid);
        return true;
    }

    private static bool MigrateConstructedDroid(uint controller)
    {
        if (string.IsNullOrWhiteSpace(GetLocalString(controller, ConstructedDroidVariable)))
            return false;

        // A summoned droid owns the live copies of the controller's items.
        var changed = MigrateSummonedDroid(GetSummonedDroid(controller));
        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(GetLocalString(controller, ConstructedDroidVariable));
        if (droid == null)
            return changed;

        var savedChanged = MigrateField(droid.SerializedCPU, value => droid.SerializedCPU = value);
        savedChanged |= MigrateField(droid.SerializedHead, value => droid.SerializedHead = value);
        savedChanged |= MigrateField(droid.SerializedBody, value => droid.SerializedBody = value);
        savedChanged |= MigrateField(droid.SerializedArms, value => droid.SerializedArms = value);
        savedChanged |= MigrateField(droid.SerializedLegs, value => droid.SerializedLegs = value);
        savedChanged |= MigrateDictionary(droid.EquippedItems);
        savedChanged |= MigrateDictionary(droid.Inventory);
        if (savedChanged)
            SetLocalString(controller, ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        return changed || savedChanged;
    }

    private static uint GetSummonedDroid(uint controller)
    {
        var droid = Droid.GetDroid(GetItemPossessor(controller));
        return GetIsObjectValid(droid) && Droid.GetControllerItem(droid) == controller ? droid : OBJECT_INVALID;
    }

    private static bool MigrateField(string data, Action<string> setData)
    {
        if (!MigrateSerializedObject(data, out var migrated))
            return false;
        setData(migrated);
        return true;
    }

    private static bool MigrateDictionary<TKey>(Dictionary<TKey, string> items)
    {
        if (items == null)
            return false;
        var changed = false;
        foreach (var key in items.Keys.ToList())
        {
            if (!MigrateSerializedObject(items[key], out var migrated))
                continue;
            items[key] = migrated;
            changed = true;
        }
        return changed;
    }
}
