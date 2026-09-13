using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NWNXLib = NWN.Native.API.NWNXLib;
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

        /// <summary>
        /// Converts equipped items and moves historical arrow-slot ammunition into its canonical bullet slot.
        /// </summary>
        private static int NormalizeCreatureEquipment(uint creature)
        {
            var migratedItems = 0;
            var legacyAmmo = GetEquippedItem(creature, InventorySlot.Arrows);

            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetEquippedItem(creature, (InventorySlot)index);
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

            NormalizeEquippedAmmo(creature, legacyAmmo, originalBaseItem);
            migratedItems++;

            return migratedItems;
        }

        /// <summary>
        /// Converts ammunition without losing the stack when its target slot is occupied or cannot be equipped.
        /// </summary>
        private static void NormalizeEquippedAmmo(
            uint creature,
            uint legacyAmmo,
            BaseItem originalBaseItem)
        {
            var existingBulletAmmo = GetEquippedItem(creature, InventorySlot.Bullets);
            var nativeCreature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSCreature();
            var generatedBullets = nativeCreature.m_bMagicalBulletsEquipped != 0;
            // Acquisition can normalize the base while the item still occupies
            // the old slot. Native unequip then returns failure after removing it;
            // verify the equipment state instead of trusting that return value.
            CreaturePlugin.RunUnequip(creature, legacyAmmo);
            if (GetEquippedItem(creature, InventorySlot.Arrows) == legacyAmmo)
                throw new InvalidOperationException("Unable to unequip legacy ammunition for migration.");

            PistolBaseItemCompatibility.Normalize(legacyAmmo);
            // The engine reserves this slot for ammunition supplied by the
            // weapon. Keep the converted stack in inventory instead of trying
            // to replace an engine-owned stack that cannot be unequipped.
            if (generatedBullets)
            {
                var nativeAmmo = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(legacyAmmo)?.AsNWSItem();
                if (nativeAmmo == null || GetItemPossessor(legacyAmmo) != creature ||
                    nativeCreature.m_pcItemRepository.GetItemInRepository(nativeAmmo) == 0)
                    throw new InvalidOperationException("Converted ammunition could not be preserved in inventory.");
                return;
            }
            var existingBulletUnequipped = false;

            if (GetIsObjectValid(existingBulletAmmo) && existingBulletAmmo != legacyAmmo)
            {
                CreaturePlugin.RunUnequip(creature, existingBulletAmmo);
                existingBulletUnequipped = GetEquippedItem(creature, InventorySlot.Bullets) != existingBulletAmmo;
                if (!existingBulletUnequipped)
                {
                    RestoreEquippedAmmo(
                        creature,
                        legacyAmmo,
                        originalBaseItem,
                        existingBulletAmmo,
                        false);
                    throw new InvalidOperationException("Unable to clear the ammunition slot for migration.");
                }
            }

            CreaturePlugin.RunEquip(creature, legacyAmmo, InventorySlot.Bullets);
            if (GetEquippedItem(creature, InventorySlot.Bullets) == legacyAmmo)
                return;

            RestoreEquippedAmmo(
                creature,
                legacyAmmo,
                originalBaseItem,
                existingBulletAmmo,
                existingBulletUnequipped);
            throw new InvalidOperationException("Unable to equip migrated ammunition in its canonical slot.");
        }

        /// <summary>
        /// Restores a saved ammunition stack to the requested native slot after compatibility conversion.
        /// </summary>
        private static void RestoreEquippedAmmo(
            uint creature,
            uint legacyAmmo,
            BaseItem originalBaseItem,
            uint existingBulletAmmo,
            bool existingBulletUnequipped)
        {
            // The original base may already be Bullet after acquisition. Restore
            // the historical slot using Arrow, then restore the exact original base.
            ItemPlugin.SetBaseItemType(legacyAmmo, BaseItem.Arrow);
            try { CreaturePlugin.RunEquip(creature, legacyAmmo, InventorySlot.Arrows); }
            finally { ItemPlugin.SetBaseItemType(legacyAmmo, originalBaseItem); }

            if (existingBulletUnequipped && GetIsObjectValid(existingBulletAmmo))
                CreaturePlugin.RunEquip(creature, existingBulletAmmo, InventorySlot.Bullets);

            if (GetEquippedItem(creature, InventorySlot.Arrows) != legacyAmmo ||
                (GetIsObjectValid(existingBulletAmmo) && GetEquippedItem(creature, InventorySlot.Bullets) != existingBulletAmmo))
                throw new InvalidOperationException("Unable to restore equipped ammunition after a failed migration.");
        }

        /// <summary>
        /// Reads real equipment slots without treating engine-generated magical ammunition as a saved item.
        /// </summary>
        private static uint GetEquippedItem(uint creature, InventorySlot slot)
        {
            // Unlimited-ammunition weapons create engine-owned stacks in these
            // slots. They are excluded from creature saves and cannot be unequipped.
            var nativeCreature = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature)?.AsNWSCreature();
            if (nativeCreature == null ||
                (slot == InventorySlot.Arrows && nativeCreature.m_bMagicalArrowsEquipped != 0) ||
                (slot == InventorySlot.Bullets && nativeCreature.m_bMagicalBulletsEquipped != 0) ||
                (slot == InventorySlot.Bolts && nativeCreature.m_bMagicalBoltsEquipped != 0))
                return OBJECT_INVALID;
            return nativeCreature?.m_pInventory.GetItemInSlot(1u << (int)slot)?.m_idSelf ?? OBJECT_INVALID;
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

        /// <summary>
        /// Converts a serialized pistol or ammunition item, reports its canonical base type, and always releases the native load.
        /// </summary>
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

            var obj = MigrationObject.Deserialize(serialized);
            if (!GetIsObjectValid(obj))
                return false;

            try
            {
                normalizedCount = NormalizeItemsOnObject(obj);
                canonicalBaseItem = GetObjectType(obj) == ObjectType.Item
                    ? GetBaseItemType(obj)
                    : BaseItem.Invalid;

                if (normalizedCount > 0)
                    migrated = MigrationObject.Serialize(obj, serialized);

                return true;
            }
            finally
            {
                MigrationObject.DestroyTemporaryObject(obj);
            }
        }

        /// <summary>
        /// Stows displaced droid ammunition under a collision-free inventory key while preserving the saved item identity.
        /// </summary>
        private static void MoveEquippedItemToDroidInventory(
            ConstructedDroid droid,
            string serializedItem)
        {
            droid.Inventory ??= new Dictionary<string, string>();

            var itemId = string.Empty;
            var item = MigrationObject.Deserialize(serializedItem);
            if (GetIsObjectValid(item))
            {
                try
                {
                    itemId = GetLocalString(item, DroidItemIdVariable);
                    if (string.IsNullOrWhiteSpace(itemId) || droid.Inventory.ContainsKey(itemId))
                    {
                        itemId = Guid.NewGuid().ToString();
                        SetLocalString(item, DroidItemIdVariable, itemId);
                        serializedItem = MigrationObject.Serialize(item, serializedItem);
                    }
                }
                finally
                {
                    MigrationObject.DestroyTemporaryObject(item);
                }
            }

            if (string.IsNullOrWhiteSpace(itemId))
                itemId = Guid.NewGuid().ToString();

            droid.Inventory[itemId] = serializedItem;
        }
    }
}
