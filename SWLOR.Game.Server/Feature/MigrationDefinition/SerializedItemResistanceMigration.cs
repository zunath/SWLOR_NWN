using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
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

        private static readonly Dictionary<int, int> ArmorAndFoodResistanceAmountByRank = new()
        {
            [1] = 3,
            [2] = 6,
            [3] = 9,
            [4] = 12,
            [5] = 15,
        };

        private static readonly Dictionary<int, int> DroidResistanceAmountByRank = new()
        {
            [1] = 8,
            [2] = 15,
        };

        private static readonly Dictionary<string, (ItemPropertyType Type, int SubType, IReadOnlyDictionary<int, int> Amounts)>
            ResistanceEnhancementByResrefPrefix = new()
            {
                ["aen_def_fir"] = (ItemPropertyType.ArmorEnhancement, (int)EnhancementSubType.ResistanceFire, ArmorAndFoodResistanceAmountByRank),
                ["aen_def_psn"] = (ItemPropertyType.ArmorEnhancement, (int)EnhancementSubType.ResistancePoison, ArmorAndFoodResistanceAmountByRank),
                ["aen_def_elec"] = (ItemPropertyType.ArmorEnhancement, (int)EnhancementSubType.ResistanceElectrical, ArmorAndFoodResistanceAmountByRank),
                ["aen_def_ice"] = (ItemPropertyType.ArmorEnhancement, (int)EnhancementSubType.ResistanceIce, ArmorAndFoodResistanceAmountByRank),
                ["aen_res_mnd"] = (ItemPropertyType.ArmorEnhancement, (int)EnhancementSubType.ResistanceMind, ArmorAndFoodResistanceAmountByRank),
                ["aen_res_mob"] = (ItemPropertyType.ArmorEnhancement, (int)EnhancementSubType.ResistanceMobility, ArmorAndFoodResistanceAmountByRank),
                ["aen_res_tra"] = (ItemPropertyType.ArmorEnhancement, (int)EnhancementSubType.ResistanceTrauma, ArmorAndFoodResistanceAmountByRank),
                ["aen_res_dis"] = (ItemPropertyType.ArmorEnhancement, (int)EnhancementSubType.ResistanceDisruption, ArmorAndFoodResistanceAmountByRank),

                ["cen_res_fir"] = (ItemPropertyType.FoodEnhancement, (int)EnhancementSubType.FoodBonusFireResistance, ArmorAndFoodResistanceAmountByRank),
                ["cen_res_psn"] = (ItemPropertyType.FoodEnhancement, (int)EnhancementSubType.FoodBonusPoisonResistance, ArmorAndFoodResistanceAmountByRank),
                ["cen_res_elec"] = (ItemPropertyType.FoodEnhancement, (int)EnhancementSubType.FoodBonusElectricalResistance, ArmorAndFoodResistanceAmountByRank),
                ["cen_res_ice"] = (ItemPropertyType.FoodEnhancement, (int)EnhancementSubType.FoodBonusIceResistance, ArmorAndFoodResistanceAmountByRank),
                ["cen_res_mnd"] = (ItemPropertyType.FoodEnhancement, (int)EnhancementSubType.FoodBonusMindResistance, ArmorAndFoodResistanceAmountByRank),
                ["cen_res_mob"] = (ItemPropertyType.FoodEnhancement, (int)EnhancementSubType.FoodBonusMobilityResistance, ArmorAndFoodResistanceAmountByRank),
                ["cen_res_tra"] = (ItemPropertyType.FoodEnhancement, (int)EnhancementSubType.FoodBonusTraumaResistance, ArmorAndFoodResistanceAmountByRank),
                ["cen_res_dis"] = (ItemPropertyType.FoodEnhancement, (int)EnhancementSubType.FoodBonusDisruptionResistance, ArmorAndFoodResistanceAmountByRank),

                ["de_res_fir"] = (ItemPropertyType.DroidEnhancement, (int)EnhancementSubType.DroidResistanceFire, DroidResistanceAmountByRank),
                ["de_res_psn"] = (ItemPropertyType.DroidEnhancement, (int)EnhancementSubType.DroidResistancePoison, DroidResistanceAmountByRank),
                ["de_res_elec"] = (ItemPropertyType.DroidEnhancement, (int)EnhancementSubType.DroidResistanceElectrical, DroidResistanceAmountByRank),
                ["de_res_ice"] = (ItemPropertyType.DroidEnhancement, (int)EnhancementSubType.DroidResistanceIce, DroidResistanceAmountByRank),
                ["de_res_mnd"] = (ItemPropertyType.DroidEnhancement, (int)EnhancementSubType.DroidResistanceMind, DroidResistanceAmountByRank),
                ["de_res_mob"] = (ItemPropertyType.DroidEnhancement, (int)EnhancementSubType.DroidResistanceMobility, DroidResistanceAmountByRank),
                ["de_res_tra"] = (ItemPropertyType.DroidEnhancement, (int)EnhancementSubType.DroidResistanceTrauma, DroidResistanceAmountByRank),
                ["de_res_dis"] = (ItemPropertyType.DroidEnhancement, (int)EnhancementSubType.DroidResistanceDisruption, DroidResistanceAmountByRank),
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
            wasMigrated |= RebalanceResistanceEnhancementItem(item);
            wasMigrated |= NormalizeDroidResistanceStats(item);

            return wasMigrated;
        }

        private static bool RebalanceResistanceEnhancementItem(uint item)
        {
            if (!TryGetResistanceEnhancementBalance(
                    GetResRef(item),
                    out var propertyType,
                    out var subType,
                    out var amount))
            {
                return false;
            }

            var matchingProperties = new List<(ItemProperty Property, int Value)>();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(ip) == propertyType &&
                    GetItemPropertySubType(ip) == subType)
                {
                    matchingProperties.Add((ip, GetItemPropertyCostTableValue(ip)));
                }
            }

            if (matchingProperties.Count == 1 &&
                matchingProperties[0].Value == amount)
            {
                return false;
            }

            foreach (var property in matchingProperties)
            {
                RemoveItemProperty(item, property.Property);
            }

            BiowareXP2.IPSafeAddItemProperty(
                item,
                ItemPropertyCustom(propertyType, subType, amount),
                0.0f,
                AddItemPropertyPolicy.ReplaceExisting,
                false,
                false);

            return true;
        }

        private static bool TryGetResistanceEnhancementBalance(
            string resref,
            out ItemPropertyType propertyType,
            out int subType,
            out int amount)
        {
            propertyType = default;
            subType = 0;
            amount = 0;

            if (string.IsNullOrWhiteSpace(resref))
                return false;

            foreach (var (prefix, details) in ResistanceEnhancementByResrefPrefix)
            {
                if (!resref.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !int.TryParse(resref[prefix.Length..], out var rank) ||
                    !details.Amounts.TryGetValue(rank, out amount))
                {
                    continue;
                }

                propertyType = details.Type;
                subType = details.SubType;
                return true;
            }

            return false;
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
