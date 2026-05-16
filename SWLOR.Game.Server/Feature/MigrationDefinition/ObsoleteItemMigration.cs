using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class ObsoleteItemMigration
    {
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

        private static readonly HashSet<string> ObsoleteItemResRefs = new(StringComparer.OrdinalIgnoreCase)
        {
            "id_tranqshot3",
        };

        public static bool IsObsoleteResRef(string resref)
        {
            return !string.IsNullOrWhiteSpace(resref) &&
                   ObsoleteItemResRefs.Contains(resref);
        }

        public static int RemoveObsoleteItemsFromObject(uint obj)
        {
            var removedCount = 0;
            RemoveObsoleteItemsFromObject(obj, ref removedCount);
            return removedCount;
        }

        private static void RemoveObsoleteItemsFromObject(uint obj, ref int removedCount)
        {
            if (!GetIsObjectValid(obj))
                return;

            var objectType = GetObjectType(obj);

            if (objectType == ObjectType.Item)
            {
                if (IsObsoleteResRef(GetResRef(obj)))
                {
                    DestroyObject(obj);
                    removedCount++;
                    return;
                }

                RemoveObsoleteItemsFromConstructedDroid(obj, ref removedCount);
            }
            else if (objectType == ObjectType.Creature)
            {
                for (var index = 0; index < NumberOfInventorySlots; index++)
                {
                    RemoveObsoleteItemsFromObject(GetItemInSlot((InventorySlot)index, obj), ref removedCount);
                }
            }

            if (!GetIsObjectValid(obj) || !GetHasInventory(obj))
                return;

            for (var item = GetFirstItemInInventory(obj); GetIsObjectValid(item);)
            {
                var nextItem = GetNextItemInInventory(obj);
                RemoveObsoleteItemsFromObject(item, ref removedCount);
                item = nextItem;
            }
        }

        public static bool RemoveObsoleteItemsFromSerializedObject(
            string serializedObject,
            out string migratedSerializedObject,
            out bool removedRoot,
            out int removedCount)
        {
            migratedSerializedObject = serializedObject;
            removedRoot = false;
            removedCount = 0;

            if (string.IsNullOrWhiteSpace(serializedObject))
                return false;

            var obj = ObjectPlugin.Deserialize(serializedObject);
            if (!GetIsObjectValid(obj))
                return false;

            if (GetObjectType(obj) == ObjectType.Item && IsObsoleteResRef(GetResRef(obj)))
            {
                DestroyObject(obj);
                removedRoot = true;
                removedCount = 1;
                return true;
            }

            removedCount = RemoveObsoleteItemsFromObject(obj);
            if (removedCount <= 0)
            {
                DestroyObject(obj);
                return false;
            }

            migratedSerializedObject = ObjectPlugin.Serialize(obj);
            DestroyObject(obj);
            return true;
        }

        private static void RemoveObsoleteItemsFromConstructedDroid(uint item, ref int removedCount)
        {
            var serialized = GetLocalString(item, ConstructedDroidVariable);
            if (string.IsNullOrWhiteSpace(serialized))
                return;

            var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
            if (droid == null)
                return;

            var changed = false;
            changed |= RemoveObsoleteItemsFromDroidField(droid.SerializedCPU, value => droid.SerializedCPU = value, ref removedCount);
            changed |= RemoveObsoleteItemsFromDroidField(droid.SerializedHead, value => droid.SerializedHead = value, ref removedCount);
            changed |= RemoveObsoleteItemsFromDroidField(droid.SerializedBody, value => droid.SerializedBody = value, ref removedCount);
            changed |= RemoveObsoleteItemsFromDroidField(droid.SerializedArms, value => droid.SerializedArms = value, ref removedCount);
            changed |= RemoveObsoleteItemsFromDroidField(droid.SerializedLegs, value => droid.SerializedLegs = value, ref removedCount);
            changed |= RemoveObsoleteItemsFromDroidDictionary(droid.EquippedItems, ref removedCount);
            changed |= RemoveObsoleteItemsFromDroidDictionary(droid.Inventory, ref removedCount);

            if (!changed)
                return;

            SetLocalString(item, ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        }

        private static bool RemoveObsoleteItemsFromDroidField(
            string serializedObject,
            Action<string> setSerializedObject,
            ref int removedCount)
        {
            if (!RemoveObsoleteItemsFromSerializedObject(
                    serializedObject,
                    out var migratedSerializedObject,
                    out var removedRoot,
                    out var serializedRemovedCount))
                return false;

            setSerializedObject(removedRoot ? string.Empty : migratedSerializedObject);
            removedCount += serializedRemovedCount;
            return true;
        }

        private static bool RemoveObsoleteItemsFromDroidDictionary<TKey>(
            Dictionary<TKey, string> serializedObjects,
            ref int removedCount)
        {
            if (serializedObjects == null || serializedObjects.Count <= 0)
                return false;

            var changed = false;
            foreach (var key in serializedObjects.Keys.ToList())
            {
                if (!RemoveObsoleteItemsFromSerializedObject(
                        serializedObjects[key],
                        out var migratedSerializedObject,
                        out var removedRoot,
                        out var serializedRemovedCount))
                    continue;

                if (removedRoot)
                    serializedObjects.Remove(key);
                else
                    serializedObjects[key] = migratedSerializedObject;

                removedCount += serializedRemovedCount;
                changed = true;
            }

            return changed;
        }
    }
}
