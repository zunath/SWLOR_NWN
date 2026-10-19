using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="SerializedItemResistanceMigration.MigrateObject(uint)"/>: replaces retired defense,
/// saving throw and incubation purity properties with resistances, rebalances resistance enhancement items to
/// their rank amounts, and adds the zero droid resistance stats that droid parts lack. It covers the character's
/// worn and carried items, containers, and the droids saved on controller items.
/// </summary>
internal static class SavedItemResistanceMigration
{
    private static IReadOnlyDictionary<DroidStatSubType, bool> _zeroDroidResistances;
    private static IReadOnlyDictionary<(ItemPropertyType Type, int SubType, int Amount), bool> _enhancements;

    /// <summary>
    /// Main thread only: whether the item properties this migration adds with fixed values can be constructed,
    /// as <c>MigrationObject.AddProperty</c> checks with <c>GetIsItemPropertyValid</c>.
    /// </summary>
    internal static void Load()
    {
        var zeroResistances = new Dictionary<DroidStatSubType, bool>();
        foreach (var subType in SerializedItemResistanceMigration.DroidResistanceSubTypes)
            zeroResistances[subType] = GetIsItemPropertyValid(ItemPropertyCustom(ItemPropertyType.DroidStat, (int)subType, 0));

        var enhancements = new Dictionary<(ItemPropertyType, int, int), bool>();
        foreach (var (_, details) in SerializedItemResistanceMigration.ResistanceEnhancementByResrefPrefix)
        {
            foreach (var amount in details.Amounts.Values)
                enhancements[(details.Type, details.SubType, amount)] =
                    GetIsItemPropertyValid(ItemPropertyCustom(details.Type, details.SubType, amount));
        }

        _zeroDroidResistances = zeroResistances;
        _enhancements = enhancements;
    }

    /// <summary>Worn items, then carried items, each with the items inside its containers.</summary>
    internal static bool MigrateCharacter(SavedCharacter character)
    {
        var changed = false;
        foreach (var item in character.Equipped.SelectMany(Expand).Concat(character.CarriedRecursive).ToArray())
            changed |= MigrateItem(item);
        return changed;
    }

    internal static bool MigrateItem(SavedItem item)
    {
        var changed = MigrateConstructedDroid(item);
        changed |= ReplaceLegacyItemProperties(item);
        changed |= RebalanceResistanceEnhancementItem(item);
        changed |= NormalizeDroidResistanceStats(item);
        return changed;
    }

    private static IEnumerable<SavedItem> Expand(SavedItem item) =>
        new[] { item }.Concat(item.Contents.SelectMany(Expand));

    private static bool RebalanceResistanceEnhancementItem(SavedItem item)
    {
        if (!SerializedItemResistanceMigration.TryGetResistanceEnhancementBalance(
                item.ResRef,
                out var propertyType,
                out var subType,
                out var amount))
        {
            return false;
        }

        var matchingProperties = item.Properties
            .Where(property => property.Type == propertyType && property.Subtype == subType)
            .ToList();
        if (matchingProperties.Count == 1 &&
            matchingProperties[0].CostValue == amount)
        {
            return false;
        }

        EnsureLoaded();
        if (!_enhancements.TryGetValue((propertyType, subType, amount), out var valid) || !valid)
            throw new InvalidOperationException("Could not construct a replacement migration item property.");

        foreach (var property in matchingProperties)
        {
            item.RemoveProperty(property);
        }

        item.AddProperty(propertyType, subType, amount, AddItemPropertyPolicy.ReplaceExisting);
        return true;
    }

    private static bool ReplaceLegacyItemProperties(SavedItem item)
    {
        var properties = item.Properties
            .Select(property => (
                Property: property,
                property.Type,
                SubType: property.Subtype,
                Value: SerializedItemResistanceMigration.GetMigrationPropertyValue(property.Type, property.CostValue)))
            .ToList();

        var replacements = new Dictionary<(ItemPropertyType Type, int SubType), int>();
        var propertiesToRemove = new List<SavedItemProperty>();

        foreach (var property in properties)
        {
            if (SerializedItemResistanceMigration.TryGetReplacement(
                    property.Type, property.SubType, out var replacementType, out var replacementSubType))
            {
                var key = (replacementType, replacementSubType);
                replacements[key] = replacements.TryGetValue(key, out var value)
                    ? Math.Max(value, property.Value)
                    : property.Value;
                propertiesToRemove.Add(property.Property);
                continue;
            }

            if (SerializedItemResistanceMigration.ShouldRemoveLegacySavingThrowProperty(property.Type))
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

        // A property can be listed twice; a saved property can only be removed once.
        foreach (var property in propertiesToRemove.Distinct())
        {
            item.RemoveProperty(property);
        }

        foreach (var ((type, subType), value) in replacements)
        {
            var costTableValue = type == ItemPropertyType.Resistance
                ? Resistance.EncodeItemPropertyCostTableValue(value)
                : value;

            item.AddProperty(type, subType, costTableValue, AddItemPropertyPolicy.ReplaceExisting);
        }

        return true;
    }

    private static bool NormalizeDroidResistanceStats(SavedItem item)
    {
        var droidStats = item.Properties.Where(property => property.Type == ItemPropertyType.DroidStat).ToList();
        if (droidStats.Count <= 0)
            return false;

        var existingResistanceSubTypes = droidStats
            .Select(property => (DroidStatSubType)property.Subtype)
            .Where(subType => SerializedItemResistanceMigration.DroidResistanceSubTypes.Contains(subType))
            .ToHashSet();

        EnsureLoaded();
        var wasMigrated = false;
        foreach (var subType in SerializedItemResistanceMigration.DroidResistanceSubTypes)
        {
            if (existingResistanceSubTypes.Contains(subType))
                continue;

            // An absent droid resistance is already zero. Some cost tables do
            // not expose a constructible zero row, so keep the default absent.
            if (!_zeroDroidResistances[subType])
                continue;

            item.AddProperty(ItemPropertyType.DroidStat, (int)subType, 0, AddItemPropertyPolicy.ReplaceExisting);
            wasMigrated = true;
        }

        return wasMigrated;
    }

    /// <summary>
    /// Migrates the droid saved on a controller item: its parts, equipped items and inventory, each saved as an
    /// item of their own. Creature data and empty values are left alone.
    /// </summary>
    private static bool MigrateConstructedDroid(SavedItem item)
    {
        var serialized = item.GetLocalString(SerializedItemResistanceMigration.ConstructedDroidVariable);
        if (string.IsNullOrWhiteSpace(serialized))
            return false;

        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
        if (droid == null)
            return false;

        var migrated = false;
        migrated |= MigrateSerializedItemField(droid.SerializedCPU, value => droid.SerializedCPU = value);
        migrated |= MigrateSerializedItemField(droid.SerializedHead, value => droid.SerializedHead = value);
        migrated |= MigrateSerializedItemField(droid.SerializedBody, value => droid.SerializedBody = value);
        migrated |= MigrateSerializedItemField(droid.SerializedArms, value => droid.SerializedArms = value);
        migrated |= MigrateSerializedItemField(droid.SerializedLegs, value => droid.SerializedLegs = value);

        if (droid.EquippedItems != null)
        {
            foreach (var key in droid.EquippedItems.Keys.ToList())
            {
                if (!MigrateSerializedItem(droid.EquippedItems[key], out var migratedValue))
                    continue;

                droid.EquippedItems[key] = migratedValue;
                migrated = true;
            }
        }

        if (droid.Inventory != null)
        {
            foreach (var key in droid.Inventory.Keys.ToList())
            {
                if (!MigrateSerializedItem(droid.Inventory[key], out var migratedValue))
                    continue;

                droid.Inventory[key] = migratedValue;
                migrated = true;
            }
        }

        if (!migrated)
            return false;

        item.SetLocalString(SerializedItemResistanceMigration.ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        return true;
    }

    private static bool MigrateSerializedItemField(string serializedItem, Action<string> setSerializedItem)
    {
        if (!MigrateSerializedItem(serializedItem, out var migratedSerializedItem))
            return false;

        setSerializedItem(migratedSerializedItem);
        return true;
    }

    /// <summary>A saved item and the items inside it; unchanged data comes back as it was given.</summary>
    private static bool MigrateSerializedItem(string serializedItem, out string migratedSerializedItem)
    {
        return SavedItemDocument.Migrate(serializedItem, document =>
        {
            var changed = false;
            foreach (var item in document.AllItems.ToArray())
                changed |= MigrateItem(item);
            return changed;
        }, out migratedSerializedItem);
    }

    private static void EnsureLoaded()
    {
        if (_zeroDroidResistances == null || _enhancements == null)
            throw new InvalidOperationException("Saved item resistance data is not loaded.");
    }
}
