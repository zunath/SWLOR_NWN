using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition
{
    internal static class EquipmentRequirementMigration
    {
        private const int MaxStandardLegacyRequirementLevel = 5;
        private const int MaxDisabledLegacyRequirementLevel = 10;
        private const int MaxSkillRequirementRank = 50;
        private const int DisabledSkillRequirementRank = 100;
        private const int SpaceEquipmentLegacyRequirement = 116;

        private static readonly Dictionary<int, SkillType> _legacyRequirementMap = new()
        {
            { 6, SkillType.OneHanded },
            { 12, SkillType.OneHanded },
            { 18, SkillType.OneHanded },
            { 28, SkillType.TwoHanded },
            { 34, SkillType.TwoHanded },
            { 40, SkillType.TwoHanded },
            { 46, SkillType.TwoHanded },
            { 47, SkillType.TwoHanded },
            { 56, SkillType.MartialArts },
            { 62, SkillType.MartialArts },
            { 73, SkillType.Ranged },
            { 79, SkillType.Ranged },
            { 91, SkillType.Ranged },
            { 102, SkillType.Armor },
            { 103, SkillType.Armor },
            { 104, SkillType.Armor },
            { 105, SkillType.Armor },
            { 106, SkillType.Armor },
            { 107, SkillType.Armor },
            { 108, SkillType.Armor },
            { 109, SkillType.Armor },
            { 110, SkillType.Armor },
            { 111, SkillType.Armor },
            { 112, SkillType.Armor },
            { 113, SkillType.Armor },
            { 114, SkillType.Armor },
            { 220, SkillType.Smithery },
            { 221, SkillType.Engineering },
            { 222, SkillType.Fabrication },
            { 223, SkillType.Agriculture },
            { 257, SkillType.Agriculture }
        };

        private static readonly HashSet<string> _tierFivePointFiveResRefs = new(StringComparer.OrdinalIgnoreCase)
        {
            "alc_spear",
            "bol_rifle",
            "ch_armor",
            "ch_belt",
            "ch_bracer",
            "ch_cloak",
            "ch_helmet",
            "ch_leggings",
            "ch_necklace",
            "ch_ring",
            "ch_shield",
            "chi_electroblade",
            "chi_greatsword",
            "chi_katar",
            "chi_knife",
            "chi_longsword",
            "chi_pistol",
            "chi_rifle",
            "chi_shuriken",
            "chi_spear",
            "chi_staff",
            "chi_twinblade",
            "chi_twinelec",
            "dhar005c",
            "dhbe005c",
            "dhbr005c",
            "dhcl005c",
            "dhhl005c",
            "dhlg005c",
            "dhnk005c",
            "dhrg005c",
            "dlar005c",
            "dlbe005c",
            "dlbr005c",
            "dlcl005c",
            "dlhl005c",
            "dllg005c",
            "dlnk005c",
            "dlrg005c",
            "imm_belt",
            "imm_boots",
            "imm_cap",
            "imm_cloak",
            "imm_gloves",
            "imm_necklace",
            "imm_ring",
            "imm_tunic",
            "mag_belt",
            "mag_boots",
            "mag_cap",
            "mag_cloak",
            "mag_gloves",
            "mag_necklace",
            "mag_ring",
            "mag_tunic"
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
            var itemResRef = GetResRef(item);
            var legacyProperties = new List<ItemProperty>();
            var legacyRequirements = new Dictionary<SkillType, int>();
            var existingSkillRequirements = new Dictionary<SkillType, int>();

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var itemPropertyType = GetItemPropertyType(ip);
                if (itemPropertyType == ItemPropertyType.RequiresSkill)
                {
                    var skillType = (SkillType)GetItemPropertySubType(ip);
                    var requiredRank = GetItemPropertyCostTableValue(ip);
                    existingSkillRequirements[skillType] = Math.Max(
                        existingSkillRequirements.GetValueOrDefault(skillType),
                        requiredRank);
                }
                else if (itemPropertyType == ItemPropertyType.UseLimitationPerk)
                {
                    var legacyRequirement = GetItemPropertySubType(ip);
                    var requiredLevel = GetItemPropertyCostTableValue(ip);
                    if (!TryGetSkillRequirement(
                            legacyRequirement,
                            requiredLevel,
                            itemResRef,
                            out var skillType,
                            out var requiredRank))
                        continue;

                    legacyProperties.Add(ip);
                    legacyRequirements[skillType] = Math.Max(
                        legacyRequirements.GetValueOrDefault(skillType),
                        requiredRank);
                }
            }

            if (legacyProperties.Count <= 0)
                return false;

            foreach (var property in legacyProperties)
            {
                RemoveItemProperty(item, property);
            }

            foreach (var (skillType, requiredRank) in legacyRequirements)
            {
                var migratedRequiredRank = Math.Max(
                    requiredRank,
                    existingSkillRequirements.GetValueOrDefault(skillType));

                var skillRequirement = ItemPropertyCustom(
                    ItemPropertyType.RequiresSkill,
                    (int)skillType,
                    migratedRequiredRank);

                BiowareXP2.IPSafeAddItemProperty(
                    item,
                    skillRequirement,
                    0.0f,
                    AddItemPropertyPolicy.ReplaceExisting,
                    false,
                    false);
            }

            return true;
        }

        private static bool TryGetSkillRequirement(
            int legacyRequirement,
            int requiredLevel,
            string itemResRef,
            out SkillType skillType,
            out int requiredRank)
        {
            requiredRank = 0;

            if (!_legacyRequirementMap.TryGetValue(legacyRequirement, out skillType))
            {
                if (legacyRequirement == SpaceEquipmentLegacyRequirement)
                    return false;

                skillType = SkillType.Invalid;
                requiredRank = DisabledSkillRequirementRank;
                return true;
            }

            if (requiredLevel > MaxDisabledLegacyRequirementLevel)
                return false;

            if (requiredLevel > MaxStandardLegacyRequirementLevel)
            {
                requiredRank = DisabledSkillRequirementRank;
                return true;
            }

            requiredRank = requiredLevel <= 1
                ? 0
                : (requiredLevel - 1) * 10;

            if (requiredLevel == MaxStandardLegacyRequirementLevel &&
                _tierFivePointFiveResRefs.Contains(itemResRef))
            {
                requiredRank = MaxSkillRequirementRank;
            }

            return true;
        }
    }
}
