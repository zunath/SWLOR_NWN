using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    /// <summary>
    /// Converts the native Accuracy Bonus and Enhancement Bonus item properties into the custom
    /// Accuracy property everywhere serialized item data can be stored. This includes equipped
    /// items, nested containers, and the separately serialized items inside droid controllers.
    /// </summary>
    public static class AccuracyItemPropertyMigration
    {
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

        /// <summary>
        /// Highest value the Accuracy cost table (iprp_enhancenum) can hold.
        /// </summary>
        public const int MaximumAccuracy = 100;

        /// <summary>
        /// Native properties whose value was previously counted as accuracy.
        /// </summary>
        public static bool IsLegacyAccuracyProperty(ItemPropertyType type)
        {
            return type == ItemPropertyType.AccuracyBonus ||
                   type == ItemPropertyType.EnhancementBonus;
        }

        /// <summary>
        /// Combines the legacy accuracy amounts found on one item into a single Accuracy value.
        /// </summary>
        public static int CombineLegacyAccuracy(IEnumerable<int> amounts)
        {
            return Math.Clamp(amounts.Sum(), 0, MaximumAccuracy);
        }

        /// <summary>
        /// Migrates a saved object and releases its temporary native load on success or failure; unchanged payloads remain intact.
        /// </summary>
        public static bool MigrateSerializedObject(string serializedObject, out string migratedSerializedObject)
        {
            migratedSerializedObject = serializedObject;
            if (string.IsNullOrWhiteSpace(serializedObject))
                return false;

            var obj = MigrationObject.Deserialize(serializedObject);
            if (!GetIsObjectValid(obj))
                return false;

            try
            {
                var wasMigrated = MigrateObject(obj);
                if (wasMigrated)
                    migratedSerializedObject = MigrationObject.Serialize(obj, serializedObject);

                return wasMigrated;
            }
            finally
            {
                MigrationObject.DestroyTemporaryObject(obj);
            }
        }

        public static bool MigrateObject(uint obj)
        {
            if (!GetIsObjectValid(obj))
                return false;

            var wasMigrated = false;
            var objectType = GetObjectType(obj);

            if (objectType == ObjectType.Item)
                wasMigrated |= MigrateItem(obj);
            else if (objectType == ObjectType.Creature)
                wasMigrated |= MigrateEquippedItems(obj);

            if (GetHasInventory(obj))
            {
                for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item); item = GetNextItemInInventory(obj))
                {
                    wasMigrated |= MigrateObject(item);
                }
            }

            return wasMigrated;
        }

        private static bool MigrateEquippedItems(uint creature)
        {
            var wasMigrated = false;

            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                wasMigrated |= MigrateObject(item);
            }

            return wasMigrated;
        }

        private static bool MigrateItem(uint item)
        {
            // Native loading converts retired rows before the engine can discard them.
            // Include that conversion in the existing migration's persistence decision.
            var wasMigrated = GetLocalInt(item, Native.LegacyItemProperties.ConvertedVariable) > 0;
            if (wasMigrated)
                DeleteLocalInt(item, Native.LegacyItemProperties.ConvertedVariable);
            wasMigrated |= ConvertLegacyAccuracyProperties(item);
            wasMigrated |= MigrateConstructedDroidLocalVariable(item);

            return wasMigrated;
        }

        private static bool ConvertLegacyAccuracyProperties(uint item)
        {
            // Collect first: removing a property mid-iteration skips the next one.
            var legacyProperties = new List<ItemProperty>();
            var amounts = new List<int>();

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (!IsLegacyAccuracyProperty(GetItemPropertyType(ip)) ||
                    GetItemPropertyDurationType(ip) != DurationType.Permanent)
                    continue;

                legacyProperties.Add(ip);
                amounts.Add(GetItemPropertyCostTableValue(ip));
            }

            if (legacyProperties.Count <= 0)
                return false;

            foreach (var ip in legacyProperties)
            {
                RemoveItemProperty(item, ip);
            }

            var accuracy = CombineLegacyAccuracy(amounts);
            if (accuracy > 0)
            {
                AddItemProperty(DurationType.Permanent, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, accuracy), item);
            }

            return true;
        }

        private static bool MigrateConstructedDroidLocalVariable(uint item)
        {
            var serialized = GetLocalString(item, ConstructedDroidVariable);
            if (string.IsNullOrWhiteSpace(serialized))
                return false;

            var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
            if (droid == null)
                return false;

            var migrated = false;
            migrated |= MigrateSerializedObjectField(droid.SerializedCPU, value => droid.SerializedCPU = value);
            migrated |= MigrateSerializedObjectField(droid.SerializedHead, value => droid.SerializedHead = value);
            migrated |= MigrateSerializedObjectField(droid.SerializedBody, value => droid.SerializedBody = value);
            migrated |= MigrateSerializedObjectField(droid.SerializedArms, value => droid.SerializedArms = value);
            migrated |= MigrateSerializedObjectField(droid.SerializedLegs, value => droid.SerializedLegs = value);

            if (droid.EquippedItems != null)
            {
                foreach (var key in droid.EquippedItems.Keys.ToList())
                {
                    if (!MigrateSerializedObject(droid.EquippedItems[key], out var migratedValue))
                        continue;

                    droid.EquippedItems[key] = migratedValue;
                    migrated = true;
                }
            }

            if (droid.Inventory != null)
            {
                foreach (var key in droid.Inventory.Keys.ToList())
                {
                    if (!MigrateSerializedObject(droid.Inventory[key], out var migratedValue))
                        continue;

                    droid.Inventory[key] = migratedValue;
                    migrated = true;
                }
            }

            if (!migrated)
                return false;

            SetLocalString(item, ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
            return true;
        }

        private static bool MigrateSerializedObjectField(string serializedObject, Action<string> setSerializedObject)
        {
            if (!MigrateSerializedObject(serializedObject, out var migratedSerializedObject))
                return false;

            setSerializedObject(migratedSerializedObject);
            return true;
        }
    }
}
