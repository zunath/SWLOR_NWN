using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Exchanges accidentally dropped saber templates for gear from the same loot tier.</summary>
public static class DungeonSaberMigration
{
    private static readonly Dictionary<string, string> Replacements = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sabstorm_l1"] = "sabstorm_l3", ["sabstorm_l2"] = "sabstorm_l4", ["sabstorm_w1"] = "sabstorm_w3",
        ["guardmst_l1"] = "guardmst_l3", ["guardmst_l2"] = "guardmst_l4", ["guardmst_w1"] = "guardmst_w3",
        ["sabcycl_l1"] = "sabcycl_l3", ["sabcycl_l2"] = "sabcycl_l4", ["sabcycl_w1"] = "sabcycl_w3",
        ["infconduit_l1"] = "infconduit_l3", ["infconduit_l2"] = "infconduit_l4", ["infconduit_w1"] = "infconduit_w3",
        ["lightstand_l1"] = "lightstand_l3", ["lightstand_l2"] = "lightstand_l4", ["lightstand_w1"] = "lightstand_w3",
        ["darkhung_l1"] = "darkhung_l3", ["darkhung_l2"] = "darkhung_l4", ["darkhung_w1"] = "darkhung_w3",
        ["eclipse_l1"] = "eclipse_l3", ["eclipse_l2"] = "eclipse_l4", ["eclipse_w1"] = "eclipse_w3"
    };

    public static bool TryGetReplacement(string resref, out string replacement)
    {
        replacement = null;
        return resref != null && Replacements.TryGetValue(resref, out replacement);
    }

    [NWNEventHandler(ScriptName.OnModuleAcquire)]
    public static void OnAcquire()
    {
        var owner = GetModuleItemAcquiredBy();
        if (GetIsPC(owner) && !GetIsDM(owner))
            MigrateObject(GetModuleItemAcquired());
    }

    public static bool MigrateObject(uint obj)
    {
        using var disposal = new MigrationItemDisposal();
        return MigrateObject(obj, disposal, new HashSet<uint>());
    }

    private static bool MigrateObject(uint obj, MigrationItemDisposal disposal, HashSet<uint> visited)
    {
        if (!GetIsObjectValid(obj) || !visited.Add(obj)) return false;
        var changed = false;
        if (GetObjectType(obj) == ObjectType.Item)
        {
            if (TryGetReplacement(GetResRef(obj), out var replacement))
            {
                ObsoleteItemMigration.ConvertItem(obj, replacement, disposal);
                return true;
            }
            var original = GetLocalString(obj, "CONSTRUCTED_DROID");
            var migrated = MigrateDroid(original);
            if (migrated != original)
            {
                SetLocalString(obj, "CONSTRUCTED_DROID", migrated);
                changed = true;
            }
        }
        else if (GetObjectType(obj) == ObjectType.Creature)
        {
            for (var slot = 0; slot < NumberOfInventorySlots; slot++)
                changed |= MigrateObject(GetItemInSlot((InventorySlot)slot, obj), disposal, visited);
        }

        if (GetHasInventory(obj))
        {
            var inventory = new List<uint>();
            for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                inventory.Add(item);
            foreach (var item in inventory)
                changed |= MigrateObject(item, disposal, visited);
        }
        return changed;
    }

    /// <summary>Preserves unrelated creature state and item identities when repairing archived inventory.</summary>
    public static bool MigrateSerializedObject(string original, out string migrated)
        => MigrateSerializedObject(original, out migrated, out _);

    private static bool MigrateSerializedObject(string original, out string migrated, out bool replacedRoot)
    {
        replacedRoot = false;
        migrated = original;
        if (string.IsNullOrWhiteSpace(original)) return false;
        using var disposal = new MigrationItemDisposal();
        var inventory = StoredObjectData.ReadInventory(original);
        var temporaryAppearance = StoredObjectData.IsCreatureData(original) ? (ushort?)6 : null;
        var obj = MigrationObject.Deserialize(inventory?.PrepareForNativeLoad(temporaryAppearance,
            resref => TryGetReplacement(resref, out _)) ?? original);
        try
        {
            if (GetObjectType(obj) == ObjectType.Item && TryGetReplacement(GetResRef(obj), out var resref))
            {
                var replacement = ObsoleteItemMigration.ConvertItem(obj, resref, disposal);
                try { migrated = MigrationObject.Serialize(replacement); }
                finally { MigrationObject.DestroyTemporaryObject(replacement); }
                replacedRoot = true;
                return true;
            }
            if (!MigrateObject(obj, disposal, new HashSet<uint>())) return false;
            migrated = MigrationObject.Serialize(obj, original);
            if (inventory != null) migrated = inventory.CopyMigratedInventory(migrated);
            return true;
        }
        finally { MigrationObject.DestroyTemporaryObject(obj); }
    }

    private static string MigrateDroid(string original)
    {
        if (string.IsNullOrWhiteSpace(original)) return original;
        var droid = JObject.Parse(original);
        var changed = false;
        foreach (var field in new[] { "SerializedCPU", "SerializedHead", "SerializedBody", "SerializedArms", "SerializedLegs" })
        {
            if (!MigrateSerializedObject((string)droid[field], out var migrated)) continue;
            droid[field] = migrated;
            changed = true;
        }
        foreach (var field in new[] { "EquippedItems", "Inventory" })
        {
            if (droid[field] is not JObject items) continue;
            foreach (var item in items.Properties().ToArray())
            {
                if (!MigrateSerializedObject((string)item.Value, out var migrated, out var replacedRoot)) continue;
                if (field == "EquippedItems" && replacedRoot)
                {
                    // A cowl, sash or vambrace cannot remain in a saved weapon slot.
                    var replacement = MigrationObject.Deserialize(migrated);
                    try
                    {
                        var id = GetLocalString(replacement, "DROID_ITEM_ID");
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            id = Guid.NewGuid().ToString();
                            SetLocalString(replacement, "DROID_ITEM_ID", id);
                            migrated = MigrationObject.Serialize(replacement, migrated);
                        }
                        var inventory = droid["Inventory"] as JObject ?? new JObject();
                        if (inventory.ContainsKey(id))
                            throw new InvalidOperationException("A converted droid item already has an inventory entry.");
                        inventory[id] = migrated;
                        droid["Inventory"] = inventory;
                        item.Remove();
                    }
                    finally { MigrationObject.DestroyTemporaryObject(replacement); }
                }
                else item.Value = migrated;
                changed = true;
            }
        }
        return changed ? droid.ToString(Newtonsoft.Json.Formatting.None) : original;
    }
}
