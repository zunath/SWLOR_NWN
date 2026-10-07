using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Updates legacy ship inventory artwork in place, including saved nested inventories.</summary>
public static class ShipItemIconMigration
{
    public static bool MigrateObject(uint obj)
    {
        var visited = new HashSet<uint>();
        bool Visit(uint current)
        {
            if (!GetIsObjectValid(current) || !visited.Add(current)) return false;
            var changed = false;
            if (GetObjectType(current) == ObjectType.Item)
            {
                var oldModel = GetItemAppearance(current, ItemAppearanceType.SimpleModel, 0);
                var newModel = ShipItemAppearance.GetUpdatedModel(GetResRef(current), (int)GetBaseItemType(current), oldModel);
                if (oldModel != newModel)
                {
                    ItemPlugin.SetItemAppearance(current, ItemAppearanceType.SimpleModel, 0, newModel);
                    changed = true;
                }
                changed |= MigrateDroidInventory(current);
            }
            if (GetObjectType(current) == ObjectType.Creature)
                for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                    changed |= Visit(GetItemInSlot((InventorySlot)slot, current));
            if (GetHasInventory(current))
                for (var item = GetFirstItemInInventory(current); GetIsObjectValid(item); item = GetNextItemInInventory(current))
                    changed |= Visit(item);
            return changed;
        }
        return Visit(obj);
    }

    public static bool MigrateSerializedObject(string serialized, out string updated)
    {
        updated = serialized;
        if (string.IsNullOrWhiteSpace(serialized)) return false;
        var obj = MigrationObject.Deserialize(serialized);
        if (!GetIsObjectValid(obj)) return false;
        try
        {
            if (!MigrateObject(obj)) return false;
            updated = MigrationObject.Serialize(obj, serialized);
            return true;
        }
        finally { MigrationObject.DestroyTemporaryObject(obj); }
    }

    private static bool MigrateDroidInventory(uint item)
    {
        const string variable = "CONSTRUCTED_DROID";
        var serialized = GetLocalString(item, variable);
        if (string.IsNullOrWhiteSpace(serialized)) return false;
        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
        if (droid == null) return false;
        var changed = false;
        bool Field(string value, Action<string> setter)
        {
            if (!MigrateSerializedObject(value, out var updated)) return false;
            setter(updated);
            return true;
        }
        changed |= Field(droid.SerializedCPU, value => droid.SerializedCPU = value);
        changed |= Field(droid.SerializedHead, value => droid.SerializedHead = value);
        changed |= Field(droid.SerializedBody, value => droid.SerializedBody = value);
        changed |= Field(droid.SerializedArms, value => droid.SerializedArms = value);
        changed |= Field(droid.SerializedLegs, value => droid.SerializedLegs = value);
        bool Inventory<TKey>(IDictionary<TKey, string> inventory)
        {
            if (inventory == null) return false;
            var inventoryChanged = false;
            foreach (var key in inventory.Keys.ToList())
                inventoryChanged |= Field(inventory[key], value => inventory[key] = value);
            return inventoryChanged;
        }
        changed |= Inventory(droid.Inventory);
        changed |= Inventory(droid.EquippedItems);
        if (changed) SetLocalString(item, variable, JsonConvert.SerializeObject(droid));
        return changed;
    }
}
