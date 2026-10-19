using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="EquipmentRequirementMigration.MigrateObject(uint)"/>: replaces legacy weapon-skill and
/// perk-based equipment requirements with Requires Skill properties on equipped and carried items, items inside
/// carried containers, and saved items kept on their own (such as droid parts and droid inventories).
/// </summary>
internal static class SavedEquipmentRequirementMigration
{
    /// <summary>Migrates every equipped and carried item, including container contents, once.</summary>
    internal static bool MigrateCharacter(SavedCharacter character)
    {
        var changed = false;
        foreach (var item in character.Equipped.Concat(character.CarriedRecursive).ToArray())
            changed |= MigrateItem(item);
        return changed;
    }

    /// <summary>Migrates a saved item and the items inside its containers, as the live rule does for a loaded item.</summary>
    internal static bool MigrateDocument(SavedItemDocument document)
    {
        var changed = false;
        foreach (var item in document.AllItems.ToArray())
            changed |= MigrateItem(item);
        return changed;
    }

    /// <summary>
    /// Migrates one item, not its contents. Legacy requirements are removed, then one Requires Skill property per
    /// skill is added with the highest rank among the legacy requirements and any Requires Skill already on the item.
    /// </summary>
    internal static bool MigrateItem(SavedItem item)
    {
        var itemResRef = item.ResRef;
        var baseItem = item.BaseItem;
        var legacyProperties = new List<SavedItemProperty>();
        var legacyRequirements = new Dictionary<SkillType, int>();
        var existingSkillRequirements = new Dictionary<SkillType, int>();

        foreach (var property in item.Properties)
        {
            if (property.Type == ItemPropertyType.RequiresSkill)
            {
                var skillId = property.Subtype;
                var requiredRank = property.CostValue;

                if (EquipmentRequirementMigration.TryRemapLegacyWeaponSkill(baseItem, ref skillId, ref requiredRank))
                {
                    var skillType = (SkillType)skillId;
                    legacyProperties.Add(property);
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
            else if (property.Type == ItemPropertyType.UseLimitationPerk)
            {
                if (!EquipmentRequirementMigration.TryGetSkillRequirement(
                        property.Subtype,
                        property.CostValue,
                        baseItem,
                        itemResRef,
                        out var skillType,
                        out var requiredRank))
                    continue;

                legacyProperties.Add(property);
                legacyRequirements[skillType] = Math.Max(
                    legacyRequirements.GetValueOrDefault(skillType),
                    requiredRank);
            }
        }

        if (legacyProperties.Count <= 0)
            return false;

        foreach (var property in legacyProperties)
        {
            item.RemoveProperty(property);
        }

        foreach (var (skillType, requiredRank) in legacyRequirements)
        {
            var migratedRequiredRank = Math.Max(
                requiredRank,
                existingSkillRequirements.GetValueOrDefault(skillType));

            item.AddProperty(
                ItemPropertyType.RequiresSkill,
                (int)skillType,
                migratedRequiredRank,
                AddItemPropertyPolicy.ReplaceExisting);
        }

        return true;
    }
}
