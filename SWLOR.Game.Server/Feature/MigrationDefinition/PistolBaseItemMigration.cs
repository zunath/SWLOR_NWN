using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    /// <summary>
    /// Permanently converts pre-update pistols and blaster ammunition everywhere
    /// serialized item data can be stored. This includes equipped items, nested
    /// containers, and the separately serialized inventory inside droid controllers.
    /// </summary>
    internal static class PistolBaseItemMigration
    {
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
        private const string DroidItemIdVariable = "DROID_ITEM_ID";

        public static void MigratePlayer(uint player)
        {
            NormalizeItemsOnObject(player);
        }

        public static bool MigrateStoredObject(uint obj)
        {
            return NormalizeItemsOnObject(obj) > 0;
        }

        private static int NormalizeItemsOnObject(uint obj)
        {
            if (!GetIsObjectValid(obj))
                return 0;

            var migratedItems = 0;
            var objectType = GetObjectType(obj);

            if (objectType == ObjectType.Item)
            {
                if (PistolBaseItemCompatibility.Normalize(obj))
                    migratedItems++;

                migratedItems += NormalizeConstructedDroid(obj);
            }
            else if (objectType == ObjectType.Creature)
            {
                migratedItems += NormalizeCreatureEquipment(obj);
            }

            if (!GetHasInventory(obj))
                return migratedItems;

            for (var item = GetFirstItemInInventory(obj);
                 GetIsObjectValid(item);
                 item = GetNextItemInInventory(obj))
            {
                migratedItems += NormalizeItemsOnObject(item);
            }

            return migratedItems;
        }

        private static int NormalizeCreatureEquipment(uint creature)
        {
            var migratedItems = 0;
            var legacyAmmo = GetItemInSlot(InventorySlot.Arrows, creature);

            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                if (!GetIsObjectValid(item) || item == legacyAmmo)
                    continue;

                migratedItems += NormalizeItemsOnObject(item);
            }

            if (!GetIsObjectValid(legacyAmmo))
                return migratedItems;

            var originalBaseItem = GetBaseItemType(legacyAmmo);
            var canonicalSlot = originalBaseItem == BaseItem.Bullet
                ? InventorySlot.Bullets
                : PistolBaseItemCompatibility.GetCanonicalInventorySlot(
                    originalBaseItem,
                    InventorySlot.Arrows);

            if (canonicalSlot != InventorySlot.Bullets)
            {
                migratedItems += NormalizeItemsOnObject(legacyAmmo);
                return migratedItems;
            }

            if (TryNormalizeEquippedAmmo(creature, legacyAmmo, originalBaseItem))
                migratedItems++;

            return migratedItems;
        }

        private static bool TryNormalizeEquippedAmmo(
            uint creature,
            uint legacyAmmo,
            BaseItem originalBaseItem)
        {
            var existingBulletAmmo = GetItemInSlot(InventorySlot.Bullets, creature);
            if (!CreaturePlugin.RunUnequip(creature, legacyAmmo))
                return false;

            var baseItemChanged = PistolBaseItemCompatibility.Normalize(legacyAmmo);
            var existingBulletUnequipped = false;

            if (GetIsObjectValid(existingBulletAmmo) && existingBulletAmmo != legacyAmmo)
            {
                existingBulletUnequipped = CreaturePlugin.RunUnequip(creature, existingBulletAmmo);
                if (!existingBulletUnequipped)
                {
                    RestoreEquippedAmmo(
                        creature,
                        legacyAmmo,
                        originalBaseItem,
                        baseItemChanged,
                        existingBulletAmmo,
                        false);
                    return false;
                }
            }

            if (CreaturePlugin.RunEquip(creature, legacyAmmo, InventorySlot.Bullets))
                return true;

            RestoreEquippedAmmo(
                creature,
                legacyAmmo,
                originalBaseItem,
                baseItemChanged,
                existingBulletAmmo,
                existingBulletUnequipped);
            return false;
        }

        private static void RestoreEquippedAmmo(
            uint creature,
            uint legacyAmmo,
            BaseItem originalBaseItem,
            bool baseItemChanged,
            uint existingBulletAmmo,
            bool existingBulletUnequipped)
        {
            if (baseItemChanged)
                ItemPlugin.SetBaseItemType(legacyAmmo, originalBaseItem);

            CreaturePlugin.RunEquip(creature, legacyAmmo, InventorySlot.Arrows);

            if (existingBulletUnequipped && GetIsObjectValid(existingBulletAmmo))
                CreaturePlugin.RunEquip(creature, existingBulletAmmo, InventorySlot.Bullets);
        }

        private static int NormalizeConstructedDroid(uint controllerItem)
        {
            var serialized = GetLocalString(controllerItem, ConstructedDroidVariable);
            if (string.IsNullOrWhiteSpace(serialized))
                return 0;

            var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
            if (droid == null)
                return 0;

            var migratedItems = 0;
            var changed = false;

            changed |= NormalizeSerializedMember(
                droid.SerializedCPU,
                value => droid.SerializedCPU = value,
                ref migratedItems);
            changed |= NormalizeSerializedMember(
                droid.SerializedHead,
                value => droid.SerializedHead = value,
                ref migratedItems);
            changed |= NormalizeSerializedMember(
                droid.SerializedBody,
                value => droid.SerializedBody = value,
                ref migratedItems);
            changed |= NormalizeSerializedMember(
                droid.SerializedArms,
                value => droid.SerializedArms = value,
                ref migratedItems);
            changed |= NormalizeSerializedMember(
                droid.SerializedLegs,
                value => droid.SerializedLegs = value,
                ref migratedItems);

            if (droid.Inventory != null)
            {
                foreach (var key in droid.Inventory.Keys.ToList())
                {
                    var current = droid.Inventory[key];
                    changed |= NormalizeSerializedMember(
                        current,
                        value => droid.Inventory[key] = value,
                        ref migratedItems);
                }
            }

            if (droid.EquippedItems != null)
            {
                var moveLegacyAmmo = false;
                foreach (var slot in droid.EquippedItems.Keys.ToList())
                {
                    var current = droid.EquippedItems[slot];
                    if (!TryNormalizeSerializedItem(
                            current,
                            out var migrated,
                            out var normalizedCount,
                            out var canonicalBaseItem))
                    {
                        continue;
                    }

                    if (normalizedCount > 0)
                    {
                        droid.EquippedItems[slot] = migrated;
                        migratedItems += normalizedCount;
                        changed = true;
                    }

                    if (slot == InventorySlot.Arrows && canonicalBaseItem == BaseItem.Bullet)
                        moveLegacyAmmo = true;
                }

                if (moveLegacyAmmo)
                {
                    var migratedAmmo = droid.EquippedItems[InventorySlot.Arrows];
                    if (droid.EquippedItems.TryGetValue(
                            InventorySlot.Bullets,
                            out var existingBulletAmmo))
                    {
                        MoveEquippedItemToDroidInventory(droid, existingBulletAmmo);
                    }

                    droid.EquippedItems.Remove(InventorySlot.Arrows);
                    droid.EquippedItems[InventorySlot.Bullets] = migratedAmmo;
                    if (!changed)
                        migratedItems++;
                    changed = true;
                }
            }

            if (!changed)
                return 0;

            SetLocalString(
                controllerItem,
                ConstructedDroidVariable,
                JsonConvert.SerializeObject(droid));
            return migratedItems;
        }

        private static bool NormalizeSerializedMember(
            string serialized,
            Action<string> setSerialized,
            ref int migratedItems)
        {
            if (!TryNormalizeSerializedItem(
                    serialized,
                    out var migrated,
                    out var normalizedCount,
                    out _)
                || normalizedCount <= 0)
            {
                return false;
            }

            setSerialized(migrated);
            migratedItems += normalizedCount;
            return true;
        }

        private static bool TryNormalizeSerializedItem(
            string serialized,
            out string migrated,
            out int normalizedCount,
            out BaseItem canonicalBaseItem)
        {
            migrated = serialized;
            normalizedCount = 0;
            canonicalBaseItem = BaseItem.Invalid;

            if (string.IsNullOrWhiteSpace(serialized))
                return false;

            var obj = ObjectPlugin.Deserialize(serialized);
            if (!GetIsObjectValid(obj))
                return false;

            normalizedCount = NormalizeItemsOnObject(obj);
            canonicalBaseItem = GetObjectType(obj) == ObjectType.Item
                ? GetBaseItemType(obj)
                : BaseItem.Invalid;

            if (normalizedCount > 0)
                migrated = ObjectPlugin.Serialize(obj);

            DestroyObject(obj);
            return true;
        }

        private static void MoveEquippedItemToDroidInventory(
            ConstructedDroid droid,
            string serializedItem)
        {
            droid.Inventory ??= new Dictionary<string, string>();

            var itemId = string.Empty;
            var item = ObjectPlugin.Deserialize(serializedItem);
            if (GetIsObjectValid(item))
            {
                itemId = GetLocalString(item, DroidItemIdVariable);
                if (string.IsNullOrWhiteSpace(itemId) || droid.Inventory.ContainsKey(itemId))
                {
                    itemId = Guid.NewGuid().ToString();
                    SetLocalString(item, DroidItemIdVariable, itemId);
                    serializedItem = ObjectPlugin.Serialize(item);
                }

                DestroyObject(item);
            }

            if (string.IsNullOrWhiteSpace(itemId))
                itemId = Guid.NewGuid().ToString();

            droid.Inventory[itemId] = serializedItem;
        }
    }
}
