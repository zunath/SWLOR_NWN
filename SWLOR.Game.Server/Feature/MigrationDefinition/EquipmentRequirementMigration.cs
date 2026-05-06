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
        private static readonly Dictionary<int, SkillType> _weaponRequirementMap = new()
        {
            { 6, SkillType.OneHanded },
            { 12, SkillType.OneHanded },
            { 18, SkillType.OneHanded },
            { 28, SkillType.TwoHanded },
            { 34, SkillType.TwoHanded },
            { 40, SkillType.TwoHanded },
            { 46, SkillType.TwoHanded },
            { 56, SkillType.MartialArts },
            { 62, SkillType.MartialArts },
            { 73, SkillType.Ranged },
            { 91, SkillType.Ranged },
            { 79, SkillType.Ranged }
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
                    var perkType = GetItemPropertySubType(ip);
                    if (!_weaponRequirementMap.TryGetValue(perkType, out var skillType))
                        continue;

                    var requiredLevel = GetItemPropertyCostTableValue(ip);
                    var requiredRank = Math.Max(0, (requiredLevel - 1) * 10);

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
    }
}
