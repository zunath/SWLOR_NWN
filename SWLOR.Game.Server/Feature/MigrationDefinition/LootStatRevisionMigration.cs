using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Moves loot created before the loot stat revision onto the revised blueprint values. Each revised
/// property shifts by the blueprint difference, so crafted enhancements merged into it are kept.
/// Revised blueprints carry the revision local, which also marks every migrated item.
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
        if (GetIsPC(owner) && !GetIsDM(owner))
            MigrateObject(GetModuleItemAcquired());
    }

    /// <summary>Revises a player's equipped and carried items, an item, or a container and its contents.</summary>
    public static bool MigrateObject(uint obj)
    {
        if (!GetIsObjectValid(obj))
            return false;

        var changed = false;
        var objectType = GetObjectType(obj);
        if (objectType == ObjectType.Creature)
        {
            // Stored DM creatures and live NPCs keep their authored gear.
            if (!GetIsPC(obj) || GetIsDM(obj))
                return false;
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                changed |= MigrateObject(GetItemInSlot((InventorySlot)slot, obj));
        }
        else if (objectType == ObjectType.Item)
        {
            changed |= MigrateItem(obj);
            changed |= MigrateConstructedDroid(obj);
        }

        if (GetHasInventory(obj))
            for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                changed |= MigrateObject(item);
        return changed;
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

        // An equipped item's stats are already counted on its wearer. Remove them first and add
        // the revised values back afterwards, as an unequip and re-equip would.
        var wearer = GetPlayerWearer(item);
        if (GetIsObjectValid(wearer))
            EquipmentStats.ApplyItemStats(wearer, item, false);

        foreach (var change in changes)
            ApplyChange(item, change);
        SetLocalInt(item, RevisionVariable, CurrentRevision);

        if (GetIsObjectValid(wearer))
        {
            EquipmentStats.ApplyItemStats(wearer, item, true);
            Stat.RefreshCombatReadinessEquipment(wearer);
        }
        return true;
    }

    private static void ApplyChange(uint item, LootStatChange change)
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
            return;

        foreach (var ip in matches)
            MigrationObject.RemoveProperty(item, ip);
        if (revised > 0)
        {
            var subtype = change.Type == ItemPropertyType.Defense ? change.Subtype : -1;
            MigrationObject.AddProperty(item, ItemPropertyCustom(change.Type, subtype, revised), AddItemPropertyPolicy.IgnoreExisting);
        }
    }

    private static uint GetPlayerWearer(uint item)
    {
        var possessor = GetItemPossessor(item);
        if (!GetIsPC(possessor) || GetIsDM(possessor) || GetIsDMPossessed(possessor))
            return OBJECT_INVALID;
        for (var slot = 0; slot < NumberOfInventorySlots; slot++)
            if (GetItemInSlot((InventorySlot)slot, possessor) == item)
                return possessor;
        return OBJECT_INVALID;
    }

    private static bool MigrateConstructedDroid(uint item)
    {
        var original = GetLocalString(item, ConstructedDroidVariable);
        if (string.IsNullOrWhiteSpace(original))
            return false;
        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(original);
        if (droid == null)
            return false;
        var changed = MigrateDictionary(droid.EquippedItems);
        changed |= MigrateDictionary(droid.Inventory);
        if (changed)
            SetLocalString(item, ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        return changed;
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
