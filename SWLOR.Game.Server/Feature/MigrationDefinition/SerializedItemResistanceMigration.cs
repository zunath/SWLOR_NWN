using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class SerializedItemResistanceMigration
    {
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
        private const int LegacyFortitudePurity = 12;
        private const int LegacyReflexPurity = 13;
        private const int LegacyWillPurity = 14;
        private const int MindResistancePurity = 16;
        private const int MobilityResistancePurity = 17;
        private const int TraumaResistancePurity = 18;
        private const int LegacyMindResistance = 5;
        private const int LegacyMobilityResistance = 6;
        private const int LegacyTraumaResistance = 7;
        private const int LegacyDisruptionResistance = 8;

        private static readonly Dictionary<int, ResistanceType> LegacyElementalDefenseToResistance = new()
        {
            { (int)CombatDamageType.Fire, ResistanceType.Fire },
            { (int)CombatDamageType.Poison, ResistanceType.Poison },
            { (int)CombatDamageType.Electrical, ResistanceType.Electrical },
            { (int)CombatDamageType.Ice, ResistanceType.Ice },
        };

        private static readonly Dictionary<int, ResistanceType> LegacyStatusResistanceToResistance = new()
        {
            { LegacyMindResistance, ResistanceType.Mind },
            { LegacyMobilityResistance, ResistanceType.Mobility },
            { LegacyTraumaResistance, ResistanceType.Trauma },
            { LegacyDisruptionResistance, ResistanceType.Disruption },
        };

        private static readonly Dictionary<int, int> LegacyIncubationPurityToResistancePurity = new()
        {
            { LegacyFortitudePurity, TraumaResistancePurity },
            { LegacyReflexPurity, MobilityResistancePurity },
            { LegacyWillPurity, MindResistancePurity },
        };

        private static readonly Dictionary<int, ResistanceType> LegacySavingThrowToResistance = new()
        {
            { (int)SaveBaseType.Fortitude, ResistanceType.Trauma },
            { (int)SaveBaseType.Reflex, ResistanceType.Mobility },
            { (int)SaveBaseType.Will, ResistanceType.Mind },
        };

        private static readonly Dictionary<int, ResistanceType> LegacySpecificSavingThrowToResistance = new()
        {
            { (int)SaveVs.COLD, ResistanceType.Ice },
            { (int)SaveVs.DISEASE, ResistanceType.Poison },
            { (int)SaveVs.ELECTRICAL, ResistanceType.Electrical },
            { (int)SaveVs.FEAR, ResistanceType.Mind },
            { (int)SaveVs.FIRE, ResistanceType.Fire },
            { (int)SaveVs.MINDAFFECTING, ResistanceType.Mind },
            { (int)SaveVs.POISON, ResistanceType.Poison },
        };

        private static readonly DroidStatSubType[] DroidResistanceSubTypes =
        {
            DroidStatSubType.ResistanceFire,
            DroidStatSubType.ResistancePoison,
            DroidStatSubType.ResistanceElectrical,
            DroidStatSubType.ResistanceIce,
            DroidStatSubType.ResistanceMind,
            DroidStatSubType.ResistanceMobility,
            DroidStatSubType.ResistanceTrauma,
            DroidStatSubType.ResistanceDisruption,
        };

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
            var wasMigrated = MigrateConstructedDroidLocalVariable(item);
            wasMigrated |= ReplaceLegacyItemProperties(item);
            wasMigrated |= NormalizeDroidResistanceStats(item);

            return wasMigrated;
        }

        private static bool ReplaceLegacyItemProperties(uint item)
        {
            var properties = new List<(ItemProperty Property, ItemPropertyType Type, int SubType, int Value)>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                properties.Add((
                    ip,
                    GetItemPropertyType(ip),
                    GetItemPropertySubType(ip),
                    GetItemPropertyCostTableValue(ip)));
            }

            var replacements = new Dictionary<(ItemPropertyType Type, int SubType), int>();
            var propertiesToRemove = new List<ItemProperty>();

            foreach (var property in properties)
            {
                if (TryGetReplacement(property.Type, property.SubType, out var replacementType, out var replacementSubType))
                {
                    var key = (replacementType, replacementSubType);
                    replacements[key] = replacements.TryGetValue(key, out var value)
                        ? Math.Max(value, property.Value)
                        : property.Value;
                    propertiesToRemove.Add(property.Property);
                    continue;
                }

                if (ShouldRemoveLegacySavingThrowProperty(property.Type))
                    propertiesToRemove.Add(property.Property);
            }

            if (replacements.Count <= 0 &&
                propertiesToRemove.Count <= 0)
                return false;

            foreach (var property in properties)
            {
                var key = (property.Type, property.SubType);
                if (!replacements.ContainsKey(key))
                    continue;

                replacements[key] = Math.Max(replacements[key], property.Value);
                propertiesToRemove.Add(property.Property);
            }

            foreach (var property in propertiesToRemove)
            {
                RemoveItemProperty(item, property);
            }

            foreach (var ((type, subType), value) in replacements)
            {
                BiowareXP2.IPSafeAddItemProperty(
                    item,
                    ItemPropertyCustom(type, subType, value),
                    0.0f,
                    AddItemPropertyPolicy.ReplaceExisting,
                    false,
                    false);
            }

            return true;
        }

        private static bool TryGetReplacement(
            ItemPropertyType propertyType,
            int subType,
            out ItemPropertyType replacementType,
            out int replacementSubType)
        {
            replacementType = propertyType;
            replacementSubType = subType;

            if (propertyType == ItemPropertyType.Defense &&
                LegacyElementalDefenseToResistance.TryGetValue(subType, out var resistanceType))
            {
                replacementType = ItemPropertyType.Resistance;
                replacementSubType = (int)resistanceType;
                return true;
            }

            if (propertyType == ItemPropertyType.Resistance &&
                LegacyStatusResistanceToResistance.TryGetValue(subType, out var statusResistanceType))
            {
                replacementSubType = (int)statusResistanceType;
                return true;
            }

            if (propertyType == ItemPropertyType.Incubation &&
                LegacyIncubationPurityToResistancePurity.TryGetValue(subType, out var resistancePurity))
            {
                replacementSubType = resistancePurity;
                return true;
            }

            if (propertyType == ItemPropertyType.SavingThrowBonus &&
                LegacySavingThrowToResistance.TryGetValue(subType, out var savingThrowResistanceType))
            {
                replacementType = ItemPropertyType.Resistance;
                replacementSubType = (int)savingThrowResistanceType;
                return true;
            }

            if (propertyType == ItemPropertyType.SavingThrowBonusSpecific &&
                LegacySpecificSavingThrowToResistance.TryGetValue(subType, out var specificSavingThrowResistanceType))
            {
                replacementType = ItemPropertyType.Resistance;
                replacementSubType = (int)specificSavingThrowResistanceType;
                return true;
            }

            return false;
        }

        private static bool ShouldRemoveLegacySavingThrowProperty(ItemPropertyType propertyType)
        {
            return propertyType == ItemPropertyType.SavingThrowBonus ||
                   propertyType == ItemPropertyType.SavingThrowBonusSpecific ||
                   propertyType == ItemPropertyType.DecreasedSavingThrows ||
                   propertyType == ItemPropertyType.DecreasedSavingThrowsSpecific;
        }

        private static bool NormalizeDroidResistanceStats(uint item)
        {
            var hasDroidStats = false;
            var existingResistanceSubTypes = new HashSet<DroidStatSubType>();

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) != ItemPropertyType.DroidStat)
                    continue;

                hasDroidStats = true;
                var subType = (DroidStatSubType)GetItemPropertySubType(ip);
                if (DroidResistanceSubTypes.Contains(subType))
                    existingResistanceSubTypes.Add(subType);
            }

            if (!hasDroidStats)
                return false;

            var wasMigrated = false;
            foreach (var subType in DroidResistanceSubTypes)
            {
                if (existingResistanceSubTypes.Contains(subType))
                    continue;

                BiowareXP2.IPSafeAddItemProperty(
                    item,
                    ItemPropertyCustom(ItemPropertyType.DroidStat, (int)subType, 0),
                    0.0f,
                    AddItemPropertyPolicy.ReplaceExisting,
                    false,
                    false);
                wasMigrated = true;
            }

            return wasMigrated;
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

            foreach (var key in droid.EquippedItems.Keys.ToList())
            {
                if (!MigrateSerializedObject(droid.EquippedItems[key], out var migratedValue))
                    continue;

                droid.EquippedItems[key] = migratedValue;
                migrated = true;
            }

            foreach (var key in droid.Inventory.Keys.ToList())
            {
                if (!MigrateSerializedObject(droid.Inventory[key], out var migratedValue))
                    continue;

                droid.Inventory[key] = migratedValue;
                migrated = true;
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
