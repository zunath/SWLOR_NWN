using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>Repairs the chest-slot headgear shipped before the blueprint correction, without replacing items or revising their stats.</summary>
public static class HeadgearSlotMigration
{
    // 8382511cf changed these exact crafted resrefs from Armor to Helmet. The
    // glove corrected in that commit is deliberately outside this migration.
    public static IReadOnlyDictionary<string, int> LegacyHeadgear { get; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["beaconmarkscr"] = 223, ["charm_cowl"] = 33, ["chemslingercr"] = 223,
            ["dunedeadeyecr"] = 223, ["enclavesentlcr"] = 223, ["flameweavercr"] = 223,
            ["markahungercr"] = 223, ["precision_optic"] = 223, ["quillstalkercr"] = 223,
            ["rite_crown"] = 11, ["ruptorvanecr"] = 223, ["siege_optics"] = 223,
            ["thermlancercr"] = 223,
        };

    public static bool IsLegacyHeadgear(string resref, BaseItem baseItem) =>
        baseItem == BaseItem.Armor && resref != null && LegacyHeadgear.ContainsKey(resref);

    /// <summary>Traverses equipped, carried and nested items, plus controller-backed and summoned droid inventories.</summary>
    public static bool MigrateObject(uint obj) => LootStatRevisionMigration.MigrateHeadgear(obj);

    private static bool IsPlayer(uint obj) => GetIsPC(obj) && !GetIsDM(obj) && !GetIsDMPossessed(obj);

    internal static bool ConvertItem(uint item)
    {
        if (!IsLegacyHeadgear(GetResRef(item), GetBaseItemType(item))) return false;
        var owner = GetItemPossessor(item);
        var equipped = GetObjectType(owner) == ObjectType.Creature && GetItemInSlot(InventorySlot.Chest, owner) == item;
        var record = IsPlayer(owner) ? DB.Get<Player>(GetObjectUUID(owner)) : null;
        var hp = equipped ? ObjectPlugin.GetCurrentHitPoints(owner) : 0;
        var fp = equipped ? Stat.GetCurrentFP(owner, record) : 0;
        var stamina = equipped ? Stat.GetCurrentStamina(owner, record) : 0;
        try
        {
            // Native unequip fires the normal stat-removal event once, while the
            // item still has its old base and properties. Reserve space first.
            if (equipped) PlayerEquipmentStorage.Unequip(owner, item, InventorySlot.Chest);
            ItemPlugin.SetBaseItemType(item, BaseItem.Helmet);
            if (GetItemAppearance(item, ItemAppearanceType.SimpleModel, 0) <= 0)
                ItemPlugin.SetItemAppearance(item, ItemAppearanceType.SimpleModel, 0, LegacyHeadgear[GetResRef(item)]);

            // Never swap out the player's existing helmet. A failed validation
            // leaves this same object in its reserved owned inventory location.
            if (equipped && !GetIsObjectValid(GetItemInSlot(InventorySlot.Head, owner)))
                CreaturePlugin.RunEquip(owner, item, InventorySlot.Head);
            if (equipped && (GetItemInSlot(InventorySlot.Chest, owner) == item || GetItemPossessor(item) != owner))
                throw new InvalidOperationException("Converted headgear was not retained by its owner.");
        }
        finally
        {
            if (equipped)
            {
                // Unequip/re-equip may briefly clamp a pool. Keep the prior
                // current amount, bounded by the final legitimate maximum.
                SetCurrentHitPoints(owner, Math.Min(hp, GetMaxHitPoints(owner)));
                if (record != null)
                {
                    record = DB.Get<Player>(GetObjectUUID(owner));
                    record.FP = Math.Min(fp, Stat.GetMaxFP(owner, record));
                    record.Stamina = Math.Min(stamina, Stat.GetMaxStamina(owner, record));
                    DB.Set(record);
                }
                else
                {
                    SetLocalInt(owner, "FP", Math.Min(fp, Stat.GetMaxFP(owner)));
                    SetLocalInt(owner, "STAMINA", Math.Min(stamina, Stat.GetMaxStamina(owner)));
                }
                Stat.RefreshCombatReadinessEquipment(owner);
            }
        }
        return true;
    }

    /// <summary>Changes only saved slot/type/model fields; UUIDs and every unrelated GFF payload stay intact.</summary>
    public static bool MigrateSerializedObject(string original, out string migrated)
    {
        migrated = original;
        // As with loot stat revision, saved NPC creatures retain authored gear.
        // Player BIC equipment must be repaired on login, with its DB record
        // loaded, so native unequip can remove the counted contribution once.
        if (string.IsNullOrWhiteSpace(original) || StoredObjectData.IsCreatureData(original)) return false;
        migrated = StoredObjectData.RepairHeadgearSlots(original);
        return migrated != original;
    }

    internal static string MigrateDroid(string original, Func<string, string> migrateItem = null, bool repairHeadgear = true)
    {
        if (string.IsNullOrWhiteSpace(original)) return original;
        migrateItem ??= data => MigrateSerializedObject(data, out var migrated) ? migrated : data;
        var droid = JObject.Parse(original);
        var changed = false;
        var movedInventoryIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in new[] { "SerializedCPU", "SerializedHead", "SerializedBody", "SerializedArms", "SerializedLegs" })
        {
            var data = (string)droid[field];
            var migrated = migrateItem(data);
            if (data == migrated) continue;
            droid[field] = migrated;
            changed = true;
        }
        foreach (var field in new[] { "EquippedItems", "Inventory" })
        {
            if (droid[field] is not JObject items) continue;
            foreach (var entry in items.Properties().ToArray())
            {
                // A chest item stowed earlier in this pass is already migrated.
                if (field == "Inventory" && movedInventoryIds.Contains(entry.Name)) continue;
                // Capture the legacy root before conversion; nested gear must
                // not move its containing bag out of an equipment slot.
                var originalItem = (string)entry.Value;
                var moveChest = repairHeadgear && field == "EquippedItems" &&
                    (entry.Name == "Chest" || entry.Name == ((int)InventorySlot.Chest).ToString()) &&
                    StoredObjectData.IsLegacyHeadgearRoot(originalItem);
                var migrated = migrateItem(originalItem);
                if (originalItem == migrated) continue;
                if (moveChest)
                {
                    // The existing pending-restore path tries Head on summon;
                    // occupied slots or validation failure leave it carried.
                    if (droid["Inventory"] is not JObject inventory)
                    {
                        inventory = new JObject();
                        droid["Inventory"] = inventory;
                    }
                    var id = StoredObjectData.ReadRootString(migrated, "DROID_ITEM_ID");
                    if (string.IsNullOrWhiteSpace(id) || inventory.Property(id) != null ||
                        inventory.Property(Droid.GetPendingItemRestoreKey(InventorySlot.Head, id)) != null)
                    {
                        id = Guid.NewGuid().ToString();
                        migrated = StoredObjectData.SetRootString(migrated, "DROID_ITEM_ID", id);
                    }
                    var restoreKey = Droid.GetPendingItemRestoreKey(InventorySlot.Head, id);
                    inventory[restoreKey] = migrated;
                    movedInventoryIds.Add(restoreKey);
                    entry.Remove();
                }
                else entry.Value = migrated;
                changed = true;
            }
        }
        return changed ? droid.ToString(Formatting.None) : original;
    }
}
