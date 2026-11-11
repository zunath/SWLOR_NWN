using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="LegacySaberMigration.MigrateStoredObject"/>: recalibrates custom legacy sabers worn, carried,
/// in containers and in saved droids to current tier and crafting budgets.
/// </summary>
internal static class SavedLegacySaberMigration
{
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
    private const string SaberTierVariable = "SABER_TIER";
    private const string LegacyUpgradeVariable = "LIGHTSABER_UPGRADE_COUNT";
    private const int LightsaberSkillSubtype = 38;
    private const int SaberstaffSkillSubtype = 42;

    private static readonly HashSet<ItemPropertyType> NormalizedPropertyTypes = new()
    {
        ItemPropertyType.DMG, ItemPropertyType.Delay, ItemPropertyType.RequiresSkill, ItemPropertyType.WeaponDamageType,
        ItemPropertyType.EnhancementBonus, ItemPropertyType.DamageBonus, ItemPropertyType.AccuracyBonus, ItemPropertyType.Accuracy,
    };

    private static readonly HashSet<ItemPropertyDamageType> ElementalDamageTypes = new()
    {
        ItemPropertyDamageType.Acid, ItemPropertyDamageType.Cold, ItemPropertyDamageType.Electrical,
        ItemPropertyDamageType.Fire, ItemPropertyDamageType.Sonic,
    };

    public static void MigrateCharacter(SavedCharacter character)
    {
        var normalized = 0;
        foreach (var item in character.Equipped.Concat(character.Carried).ToArray())
            normalized += Normalize(item);
        if (normalized <= 0)
            return;
        var saberText = normalized == 1 ? "lightsaber has" : "lightsabers have";
        character.Notices.Add($"Your {saberText} been recalibrated to current crafting limits, retaining supported bonuses and previously applied Chiro upgrades.");
        Log.Write(LogGroup.Migration, $"Recalibrated {normalized} legacy saber(s) to current crafting limits for {character.Record.Name} ({character.Id}).");
    }

    /// <summary>Saved-data <c>NormalizeSabersOnObject</c> for an item: the saber itself, or its droid and contents.</summary>
    private static int Normalize(SavedItem item)
    {
        if (IsLegacySaber(item))
        {
            SavedEquipmentRequirementMigration.MigrateItem(item);
            SavedWeaponDamageTypeMigration.MigrateItem(item);
            NormalizeSaber(item);
            return 1;
        }
        var normalized = NormalizeDroid(item);
        foreach (var content in item.Contents.ToArray())
            normalized += Normalize(content);
        return normalized;
    }

    /// <summary>Saved-data <see cref="LegacySaberMigration.IsLegacySaber"/>.</summary>
    private static bool IsLegacySaber(SavedItem item)
    {
        if (item.BaseItem != BaseItem.Lightsaber && item.BaseItem != BaseItem.Saberstaff)
            return false;
        if (_17_RemoveWeaponStatOverrides.IsEconomyRestricted(item) || LegacySaberMigration.IsCraftableSaberResref(item.ResRef))
            return false;
        var tier = item.GetLocalInt(SaberTierVariable);
        return tier <= 0 || (tier == 5 && item.GetLocalInt(LegacyUpgradeVariable) > 0);
    }

    /// <summary>Saved-data <c>LegacySaberMigration.NormalizeSaber</c>: replaces the damage profile with a bounded current one.</summary>
    private static void NormalizeSaber(SavedItem item)
    {
        var isSaberstaff = item.BaseItem == BaseItem.Saberstaff;
        var damage = 0;
        var accuracy = 0;
        var remove = new List<SavedItemProperty>();
        var (elementalType, elementalAmount) = FindStrongestElementalBonus(item);
        var elementalConsumed = false;
        foreach (var property in item.Properties)
        {
            var type = property.Type;
            if (type == ItemPropertyType.DMG)
                damage += property.CostValue;
            else if (type == ItemPropertyType.DamageBonus)
            {
                // The carried-over elemental bonus is re-added as-is, not folded into physical damage.
                if (!elementalConsumed && elementalType.HasValue &&
                    (ItemPropertyDamageType)property.Subtype == elementalType.Value && property.CostValue == elementalAmount)
                {
                    elementalConsumed = true;
                    remove.Add(property);
                    continue;
                }
                if (SavedCharacterData.TryGetDamageCost(property.CostValue, out var cost))
                    damage += SaberRecalibration.CalculateLegacyDamageBonus(cost.Dice, cost.Die);
            }
            else if (type == ItemPropertyType.Accuracy || AccuracyItemPropertyMigration.IsLegacyAccuracyProperty(type))
                accuracy += property.CostValue;
            if (NormalizedPropertyTypes.Contains(type))
                remove.Add(property);
        }

        var upgraded = item.GetLocalInt(LegacyUpgradeVariable) > 0;
        // A previous sweep erased the original bonuses; only the saved upgrade marker survives.
        if (item.GetLocalInt(SaberTierVariable) == 5 && upgraded)
            damage += 3;
        var profile = SaberRecalibration.CalculateProfile(isSaberstaff, damage, accuracy, upgraded);
        foreach (var property in remove)
            item.RemoveProperty(property);
        if (elementalType.HasValue)
            item.AddProperty(ItemPropertyType.DamageBonus, (int)elementalType.Value, elementalAmount, AddItemPropertyPolicy.ReplaceExisting);
        var delay = (int)ItemPropertyAttackDelay.Delay240;
        item.AddProperty(ItemPropertyType.DMG, -1, profile.Damage, AddItemPropertyPolicy.ReplaceExisting);
        item.AddProperty(ItemPropertyType.Delay, -1, delay, AddItemPropertyPolicy.ReplaceExisting);
        item.AddProperty(ItemPropertyType.RequiresSkill, isSaberstaff ? SaberstaffSkillSubtype : LightsaberSkillSubtype,
            profile.RequiredSkill, AddItemPropertyPolicy.ReplaceExisting);
        if (profile.Accuracy > 0)
            item.AddProperty(ItemPropertyType.Accuracy, -1, profile.Accuracy, AddItemPropertyPolicy.ReplaceExisting);
        item.SetLocalInt(SaberTierVariable, profile.Tier);
    }

    /// <summary>The strongest elemental Damage Bonus on the item; the first one found wins ties.</summary>
    private static (ItemPropertyDamageType? Type, int Amount) FindStrongestElementalBonus(SavedItem item)
    {
        ItemPropertyDamageType? bestType = null;
        var bestAmount = 0;
        foreach (var property in item.Properties.Where(property => property.Type == ItemPropertyType.DamageBonus))
        {
            var damageType = (ItemPropertyDamageType)property.Subtype;
            if (ElementalDamageTypes.Contains(damageType) && property.CostValue > bestAmount)
            {
                bestType = damageType;
                bestAmount = property.CostValue;
            }
        }
        return (bestType, bestAmount);
    }

    /// <summary>Saved-data <c>NormalizeSabersInConstructedDroid</c>: the droid's worn and carried items.</summary>
    private static int NormalizeDroid(SavedItem controller)
    {
        var serialized = controller.GetLocalString(ConstructedDroidVariable);
        if (string.IsNullOrWhiteSpace(serialized))
            return 0;
        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
        if (droid == null)
            return 0;
        var normalized = 0;
        void Migrate<TKey>(Dictionary<TKey, string> items)
        {
            if (items == null)
                return;
            foreach (var key in items.Keys.ToList())
            {
                var count = 0;
                if (!SavedItemDocument.Migrate(items[key], document => (count = Normalize(document.Root)) > 0, out var migrated))
                    continue;
                items[key] = migrated;
                // Each saved item that held a legacy saber counts once, as the live sweep counts them.
                normalized++;
            }
        }
        Migrate(droid.EquippedItems);
        Migrate(droid.Inventory);
        if (normalized <= 0)
            return 0;
        controller.SetLocalString(ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        return normalized;
    }
}
