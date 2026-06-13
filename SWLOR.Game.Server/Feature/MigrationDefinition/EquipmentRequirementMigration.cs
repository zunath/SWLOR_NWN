using System.Collections.Generic;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
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
        private const int LegacyWeaponSkillId1 = 1;
        private const int LegacyWeaponSkillId2 = 2;
        private const int LegacyWeaponSkillId3 = 3;
        private const int LegacyWeaponSkillId4 = 4;

        private static readonly Dictionary<int, int> _legacyRequirementMap = new()
        {
            { 6, LegacyWeaponSkillId1 },
            { 12, LegacyWeaponSkillId1 },
            { 18, LegacyWeaponSkillId1 },
            { 28, LegacyWeaponSkillId2 },
            { 34, LegacyWeaponSkillId2 },
            { 40, LegacyWeaponSkillId2 },
            { 46, LegacyWeaponSkillId2 },
            { 47, LegacyWeaponSkillId2 },
            { 56, LegacyWeaponSkillId3 },
            { 62, LegacyWeaponSkillId3 },
            { 73, LegacyWeaponSkillId4 },
            { 79, LegacyWeaponSkillId4 },
            { 91, LegacyWeaponSkillId4 },
            { 102, (int)SkillType.Armor },
            { 103, (int)SkillType.Armor },
            { 104, (int)SkillType.Armor },
            { 105, (int)SkillType.Armor },
            { 106, (int)SkillType.Armor },
            { 107, (int)SkillType.Armor },
            { 108, (int)SkillType.Armor },
            { 109, (int)SkillType.Armor },
            { 110, (int)SkillType.Armor },
            { 111, (int)SkillType.Armor },
            { 112, (int)SkillType.Armor },
            { 113, (int)SkillType.Armor },
            { 114, (int)SkillType.Armor },
            { 220, (int)SkillType.Smithery },
            { 221, (int)SkillType.Engineering },
            { 222, (int)SkillType.Fabrication },
            { 223, (int)SkillType.Agriculture },
            { 257, (int)SkillType.Agriculture }
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
            var baseItem = GetBaseItemType(item);
            var legacyProperties = new List<ItemProperty>();
            var legacyRequirements = new Dictionary<SkillType, int>();
            var existingSkillRequirements = new Dictionary<SkillType, int>();

            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var itemPropertyType = GetItemPropertyType(ip);
                if (itemPropertyType == ItemPropertyType.RequiresSkill)
                {
                    var skillId = GetItemPropertySubType(ip);
                    var requiredRank = GetItemPropertyCostTableValue(ip);

                    if (TryRemapLegacyWeaponSkill(baseItem, ref skillId, ref requiredRank))
                    {
                        var skillType = (SkillType)skillId;
                        legacyProperties.Add(ip);
                        legacyRequirements[skillType] = Math.Max(
                            legacyRequirements.GetValueOrDefault(skillType),
                            requiredRank);

                        continue;
                    }

                    var existingSkillType = (SkillType)skillId;
                    existingSkillRequirements[existingSkillType] = Math.Max(
                        existingSkillRequirements.GetValueOrDefault(existingSkillType),
                        requiredRank);
                }
                else if (itemPropertyType == ItemPropertyType.UseLimitationPerk)
                {
                    var legacyRequirement = GetItemPropertySubType(ip);
                    var requiredLevel = GetItemPropertyCostTableValue(ip);
                    if (!TryGetSkillRequirement(
                            legacyRequirement,
                            requiredLevel,
                            baseItem,
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

        private static bool TryRemapLegacyWeaponSkill(
            BaseItem baseItem,
            ref int skillId,
            ref int requiredRank)
        {
            if (!IsLegacyWeaponSkill(skillId))
                return false;

            var skillType = Skill.GetSkillTypeByBaseItem(baseItem);
            skillId = (int)skillType;
            if (skillId == (int)SkillType.Invalid)
                requiredRank = DisabledSkillRequirementRank;

            return true;
        }

        private static bool IsLegacyWeaponSkill(int skillId)
        {
            return skillId is LegacyWeaponSkillId1
                or LegacyWeaponSkillId2
                or LegacyWeaponSkillId3
                or LegacyWeaponSkillId4;
        }

        private static bool TryGetSkillRequirement(
            int legacyRequirement,
            int requiredLevel,
            BaseItem baseItem,
            string itemResRef,
            out SkillType skillType,
            out int requiredRank)
        {
            skillType = SkillType.Invalid;
            requiredRank = 0;

            if (!_legacyRequirementMap.TryGetValue(legacyRequirement, out var skillId))
            {
                if (legacyRequirement == SpaceEquipmentLegacyRequirement)
                    return false;

                skillType = SkillType.Invalid;
                requiredRank = DisabledSkillRequirementRank;
                return true;
            }

            if (IsLegacyWeaponSkill(skillId))
            {
                TryRemapLegacyWeaponSkill(baseItem, ref skillId, ref requiredRank);
                if (skillId == (int)SkillType.Invalid)
                {
                    skillType = SkillType.Invalid;
                    return true;
                }
            }

            skillType = (SkillType)skillId;

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
