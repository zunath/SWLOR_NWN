using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Restores weapon-category stat scaling on player equipment without changing its other bonuses.</summary>
public static class WeaponStatOverrideMigration
{
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire()
    {
        var owner = GetModuleItemAcquiredBy();
        if (GetIsPC(owner) && !GetIsDM(owner))
            MigrateObject(GetModuleItemAcquired());
    }

    /// <summary>Removes overrides from equipped and carried player weapons, including nested containers.</summary>
    public static bool MigrateObject(uint obj)
    {
        if (!GetIsObjectValid(obj))
            return false;

        var changed = false;
        var objectType = GetObjectType(obj);
        if (objectType == ObjectType.Creature)
        {
            // Stored DM creatures and live NPCs retain authored combat stats.
            if (!GetIsPC(obj) || GetIsDM(obj))
                return false;
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                changed |= MigrateObject(GetItemInSlot((InventorySlot)slot, obj));
        }
        else if (objectType == ObjectType.Item)
        {
            if (Item.IsAttackWeaponType(GetBaseItemType(obj)) && !Item.IsEconomyRestricted(obj))
            {
                var remove = new List<ItemProperty>();
                for (var ip = GetFirstItemProperty(obj); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(obj))
                    if (GetItemPropertyType(ip) is ItemPropertyType.AccuracyStat or ItemPropertyType.DamageStat)
                        remove.Add(ip);
                foreach (var ip in remove)
                    MigrationObject.RemoveProperty(obj, ip);
                changed |= remove.Count > 0;
            }
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
