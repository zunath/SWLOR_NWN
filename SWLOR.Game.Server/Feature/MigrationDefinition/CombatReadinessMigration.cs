using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class CombatReadinessMigration
    {
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

        public static readonly IReadOnlyDictionary<string, string> CombatReadinessItemNamesByResref =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["aen_recast1"] = "Armor Enhancement - Combat Readiness I",
                ["aen_recast2"] = "Armor Enhancement - Combat Readiness II",
                ["aen_recast3"] = "Armor Enhancement - Combat Readiness III",
                ["aen_recast4"] = "Armor Enhancement - Combat Readiness IV",
                ["aen_recast5"] = "Armor Enhancement - Combat Readiness V",

                ["cen_recast1"] = "Cooking Enhancement - Combat Readiness I",
                ["cen_recast2"] = "Cooking Enhancement - Combat Readiness II",
                ["cen_recast3"] = "Cooking Enhancement - Combat Readiness III",
                ["cen_recast4"] = "Cooking Enhancement - Combat Readiness IV",
                ["cen_recast5"] = "Cooking Enhancement - Combat Readiness V",
            };

        public static bool TryGetCombatReadinessItemName(string resref, out string name)
        {
            name = string.Empty;
            return !string.IsNullOrWhiteSpace(resref) &&
                   CombatReadinessItemNamesByResref.TryGetValue(resref, out name);
        }

        public static bool MigrateSerializedObject(string serializedObject, out string migratedSerializedObject)
        {
            migratedSerializedObject = serializedObject;
            if (string.IsNullOrWhiteSpace(serializedObject))
                return false;

            var obj = ObjectPlugin.Deserialize(serializedObject);
            if (!GetIsObjectValid(obj))
                return false;

            var wasMigrated = MigrateObject(obj);
            if (wasMigrated)
                migratedSerializedObject = ObjectPlugin.Serialize(obj);

            DestroyObject(obj);
            return wasMigrated;
        }

        public static void ResetCombatReadiness(Player dbPlayer)
        {
            dbPlayer.CombatReadiness = 0;
        }

        public static void MigratePlayer(uint player)
        {
            MigrateObject(player);

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            ResetCombatReadiness(dbPlayer);
            dbPlayer.CombatReadiness = CalculateEquippedCombatReadiness(player);

            DB.Set(dbPlayer);
        }

        private static int CalculateEquippedCombatReadiness(uint creature)
        {
            var amount = 0;

            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                if (!GetIsObjectValid(item))
                    continue;

                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    if (GetItemPropertyType(ip) != ItemPropertyType.CombatReadiness)
                        continue;

                    amount += GetItemPropertyCostTableValue(ip);
                }
            }

            return amount;
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
            var wasMigrated = NormalizeCombatReadinessName(item);
            wasMigrated |= MigrateConstructedDroidLocalVariable(item);

            return wasMigrated;
        }

        private static bool NormalizeCombatReadinessName(uint item)
        {
            if (!TryGetCombatReadinessItemName(GetResRef(item), out var name) ||
                GetName(item) == name)
            {
                return false;
            }

            SetName(item, name);
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
