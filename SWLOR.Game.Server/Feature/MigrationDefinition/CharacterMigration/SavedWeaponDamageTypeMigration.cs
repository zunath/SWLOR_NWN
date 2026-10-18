using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using Live = SWLOR.Game.Server.Feature.MigrationDefinition.SerializedItemWeaponDamageTypeMigration;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="Live.MigrateObject"/>: converts legacy weapon damage and damage-type properties on the
/// character's worn and carried items, items in containers, and the parts, equipment and inventory of droids saved
/// on controller items. The weapon tables and damage-type rules are the live migration's.
/// </summary>
internal static class SavedWeaponDamageTypeMigration
{
    // Several workers can reach the blueprint choice at once, and the shared RNG is not thread safe.
    private static readonly object RandomLock = new();

    /// <summary>Saved-data <c>MigrateObject</c> on the character: every worn item, then every carried item and container content.</summary>
    internal static bool MigrateCharacter(SavedCharacter character)
    {
        var changed = false;
        foreach (var item in character.Equipped.SelectMany(Expand).Concat(character.CarriedRecursive).ToArray())
            changed |= MigrateItem(item);
        return changed;
    }

    private static IEnumerable<SavedItem> Expand(SavedItem item) =>
        new[] { item }.Concat(item.Contents.SelectMany(Expand));

    /// <summary>Saved-data <c>MigrateItem</c>: one item and the droid saved on it, not the items it contains.</summary>
    internal static bool MigrateItem(SavedItem item)
    {
        var wasMigrated = MigrateConstructedDroid(item);
        wasMigrated |= MigrateEnhancementItem(item);
        var baseItem = item.BaseItem;
        if (!Live.WeaponBaseItemTypes.Contains(baseItem))
            return wasMigrated;

        // NPC double weapons retain their explicit budgets: two ends share
        // twice the previous single-end interval, preserving damage per second.
        var isDoubleWeapon = Item.IsDoubleWeaponType(baseItem);
        if (isDoubleWeapon && _17_RemoveWeaponStatOverrides.IsEconomyRestricted(item))
            return wasMigrated;

        var hasTargetWeaponDelay = HasTargetWeaponDelay(item);
        var rebaseDoubleWeapon = isDoubleWeapon && !hasTargetWeaponDelay && HasCanonicalDamage(item);
        wasMigrated |= MigrateWeaponItem(item);
        if (rebaseDoubleWeapon)
            wasMigrated |= RebaseDoubleWeaponDamage(item);
        else if (!hasTargetWeaponDelay)
            wasMigrated |= MigrateWeaponDamageAmountItem(item);

        wasMigrated |= MigrateWeaponDelayItem(item);
        wasMigrated |= NormalizeBasicWeapon(item);
        return wasMigrated;
    }

    /// <summary>
    /// Converts legacy weapon damage properties to the current amount and damage-type representation.
    /// </summary>
    private static bool MigrateWeaponItem(SavedItem item)
    {
        var baseItem = item.BaseItem;
        var damageProperties = new List<(SavedItemProperty Property, int SubType, int Value)>();
        var damageTypeProperties = new List<(SavedItemProperty Property, int SubType)>();

        foreach (var property in item.Properties)
        {
            if (property.Type == ItemPropertyType.DMG)
                damageProperties.Add((property, property.Subtype, property.CostValue));
            else if (property.Type == ItemPropertyType.WeaponDamageType)
                damageTypeProperties.Add((property, property.Subtype));
        }

        if (damageProperties.Count <= 0)
            return NormalizeDamageTypePropertiesWithoutDmg(item, baseItem, damageTypeProperties);

        var damageType = ResolveDamageType(damageProperties, damageTypeProperties);
        if (!ShouldMigrate(baseItem, damageProperties, damageTypeProperties, damageType))
            return false;

        var damage = damageProperties.Sum(x => x.Value);

        foreach (var property in damageProperties)
            item.RemoveProperty(property.Property);
        foreach (var property in damageTypeProperties)
            item.RemoveProperty(property.Property);

        item.AddProperty(ItemPropertyType.DMG, -1, damage, AddItemPropertyPolicy.IgnoreExisting);

        if (!Live.ShouldRemoveWeaponDamageType(baseItem) && !damageType.IsPhysicalDamageType())
            item.AddProperty(ItemPropertyType.WeaponDamageType, (int)damageType, -1, AddItemPropertyPolicy.IgnoreExisting);

        return true;
    }

    private static bool HasCanonicalDamage(SavedItem item)
    {
        var foundDamage = false;
        foreach (var property in item.Properties)
        {
            if (property.Type != ItemPropertyType.DMG)
                continue;
            var subType = property.Subtype;
            if (subType != -1 && subType != 0 && subType != ushort.MaxValue)
                return false;
            foundDamage = true;
        }
        return foundDamage;
    }

    private static bool RebaseDoubleWeaponDamage(SavedItem item)
    {
        var tier = item.GetLocalInt("SABER_TIER");
        var properties = new List<SavedItemProperty>();
        var damage = 0;
        foreach (var property in item.Properties)
        {
            if (property.Type == ItemPropertyType.DMG)
            {
                properties.Add(property);
                damage += property.CostValue;
            }
            else if (tier <= 0 && property.Type == ItemPropertyType.RequiresSkill)
                tier = property.CostValue / 10 + 1;
        }
        if (properties.Count == 0)
            return false;
        var targetDamage = DoubleWeaponRecalibration.CalculateDamage(item.ResRef, tier, damage);
        if (properties.Count == 1 && damage == targetDamage)
            return false;
        foreach (var property in properties)
            item.RemoveProperty(property);
        item.AddProperty(ItemPropertyType.DMG, -1, targetDamage, AddItemPropertyPolicy.ReplaceExisting);
        return true;
    }

    private static bool MigrateWeaponDamageAmountItem(SavedItem item)
    {
        if (!Live.TryGetWeaponDamageScale(item.BaseItem, item.ResRef, out var scale))
            return false;

        var damageProperties = item.Properties
            .Where(property => property.Type == ItemPropertyType.DMG)
            .Select(property => (Property: property, Value: property.CostValue))
            .ToList();
        if (damageProperties.Count <= 0)
            return false;

        var currentDamage = damageProperties.Sum(x => x.Value);
        var targetDamage = Live.CalculateScaledWeaponDamage(currentDamage, scale.OldDamage, scale.NewDamage);
        if (damageProperties.Count == 1 && damageProperties[0].Value == targetDamage)
            return false;

        foreach (var property in damageProperties)
            item.RemoveProperty(property.Property);

        item.AddProperty(ItemPropertyType.DMG, -1, targetDamage, AddItemPropertyPolicy.ReplaceExisting);
        return true;
    }

    private static bool MigrateWeaponDelayItem(SavedItem item)
    {
        var targetDelayCost = GetTargetDelayCost(item);
        if (!targetDelayCost.HasValue)
            return false;

        var delayProperties = item.Properties
            .Where(property => property.Type == ItemPropertyType.Delay)
            .Select(property => (Property: property, Value: property.CostValue))
            .ToList();
        if (delayProperties.Count == 1 && delayProperties[0].Value == (int)targetDelayCost.Value)
            return false;

        foreach (var property in delayProperties)
            item.RemoveProperty(property.Property);

        item.AddProperty(ItemPropertyType.Delay, -1, (int)targetDelayCost.Value, AddItemPropertyPolicy.ReplaceExisting);
        return true;
    }

    private static bool HasTargetWeaponDelay(SavedItem item)
    {
        var targetDelayCost = GetTargetDelayCost(item);
        if (!targetDelayCost.HasValue)
            return false;

        var delayPropertyCount = 0;
        var hasTargetDelay = false;
        foreach (var property in item.Properties)
        {
            if (property.Type != ItemPropertyType.Delay)
                continue;

            delayPropertyCount++;
            hasTargetDelay |= property.CostValue == (int)targetDelayCost.Value;
        }

        return delayPropertyCount == 1 && hasTargetDelay;
    }

    private static ItemPropertyAttackDelay? GetTargetDelayCost(SavedItem item)
    {
        var resref = item.ResRef;
        if (!string.IsNullOrWhiteSpace(resref) && Live.DelayByResref.TryGetValue(resref, out var resrefDelay))
            return resrefDelay;

        return WeaponDelay.GetWeaponDelay(item.BaseItem);
    }

    /// <summary>
    /// Normalizes weapon enhancement properties while preserving supported nonphysical damage types.
    /// </summary>
    private static bool MigrateEnhancementItem(SavedItem item)
    {
        var damageEnhancements = new List<(SavedItemProperty Property, int SubType, int Value, int Index)>();
        var damageTypeProperties = new List<(SavedItemProperty Property, int SubType, int Index)>();
        var isBlueprint = item.GetLocalInt(Live.BlueprintRecipeIdVariable) > 0;
        var index = 0;

        foreach (var property in item.Properties)
        {
            var type = property.Type;
            if (type == ItemPropertyType.WeaponEnhancement)
            {
                var subType = property.Subtype;
                if (Live.IsWeaponDamageEnhancementSubType(subType))
                    damageEnhancements.Add((property, subType, property.CostValue, index));
            }
            else if (type == ItemPropertyType.WeaponDamageType)
            {
                damageTypeProperties.Add((property, property.Subtype, index));
            }
            else if (type == ItemPropertyType.Blueprint)
            {
                isBlueprint = true;
            }

            index++;
        }

        if (damageEnhancements.Count <= 0)
            return false;

        var selectedEnhancements = damageEnhancements
            .OrderBy(x => x.Index)
            .Select(x => new Live.EnhancementDamageProperty(
                x.Value,
                x.Index,
                ResolveEnhancementDamageType(x, damageTypeProperties)))
            .ToList();

        if (isBlueprint)
        {
            lock (RandomLock)
                selectedEnhancements = Live.SelectBlueprintDamageEnhancements(selectedEnhancements);
        }

        // Retain canonical pairs without forcing the containing inventory to
        // be saved again. Native reloads can normalize unrelated item fields.
        var targets = selectedEnhancements.Select(property => property with
        {
            Value = property.DamageType.IsPhysicalDamageType()
                ? Live.RawDamageEnhancementAmountsByResref.GetValueOrDefault(item.ResRef, property.Value)
                : property.Value
        }).ToList();
        if (damageEnhancements.Count == targets.Count &&
            damageTypeProperties.Count == targets.Count(x => !x.DamageType.IsPhysicalDamageType()) &&
            damageEnhancements.Zip(targets).All(pair =>
                pair.First.SubType == (int)EnhancementSubType.DMG &&
                pair.First.Value == pair.Second.Value &&
                (pair.Second.DamageType.IsPhysicalDamageType() ||
                 damageTypeProperties.Any(type => type.Index == pair.First.Index + 1 &&
                     type.SubType == (int)pair.Second.DamageType))))
        {
            return false;
        }

        foreach (var property in damageEnhancements)
            item.RemoveProperty(property.Property);
        foreach (var property in damageTypeProperties)
            item.RemoveProperty(property.Property);

        foreach (var property in targets)
        {
            item.AddProperty(ItemPropertyType.WeaponEnhancement, (int)EnhancementSubType.DMG, property.Value, AddItemPropertyPolicy.IgnoreExisting);

            if (!property.DamageType.IsPhysicalDamageType())
                item.AddProperty(ItemPropertyType.WeaponDamageType, (int)property.DamageType, -1, AddItemPropertyPolicy.IgnoreExisting);
        }

        return true;
    }

    private static CombatDamageType ResolveEnhancementDamageType(
        (SavedItemProperty Property, int SubType, int Value, int Index) enhancement,
        List<(SavedItemProperty Property, int SubType, int Index)> damageTypeProperties)
    {
        if (Live.LegacyEnhancementDamageTypesBySubType.TryGetValue(enhancement.SubType, out var legacyDamageType))
            return Live.ToWeaponDamageType(legacyDamageType);

        // No paired property leaves the default tuple, whose subtype 0 is not a character damage type.
        var pairedDamageType = damageTypeProperties.FirstOrDefault(x => x.Index == enhancement.Index + 1);
        if (Live.TryGetCharacterDamageType(pairedDamageType.SubType, out var damageType))
            return Live.ToWeaponDamageType(damageType);

        return CombatDamageType.Physical;
    }

    /// <summary>
    /// Converts standalone legacy damage-type properties even when no damage-amount property accompanies them.
    /// </summary>
    private static bool NormalizeDamageTypePropertiesWithoutDmg(
        SavedItem item,
        BaseItem baseItem,
        List<(SavedItemProperty Property, int SubType)> damageTypeProperties)
    {
        if (Live.ShouldRemoveWeaponDamageType(baseItem) && damageTypeProperties.Count > 0)
        {
            foreach (var property in damageTypeProperties)
                item.RemoveProperty(property.Property);

            return true;
        }

        if (damageTypeProperties.Count <= 1 &&
            damageTypeProperties.All(x => Live.TryGetCharacterDamageType(x.SubType, out var damageType) && damageType.IsElementalDamageType()))
        {
            return false;
        }

        var resolved = ResolveDamageType(Array.Empty<(SavedItemProperty Property, int SubType, int Value)>(), damageTypeProperties);

        foreach (var property in damageTypeProperties)
            item.RemoveProperty(property.Property);

        if (resolved.IsPhysicalDamageType())
            return damageTypeProperties.Count > 0;

        item.AddProperty(ItemPropertyType.WeaponDamageType, (int)resolved, -1, AddItemPropertyPolicy.IgnoreExisting);
        return true;
    }

    /// <summary>
    /// Distinguishes legacy damage-type encodings from canonical properties so retries remain unchanged.
    /// </summary>
    private static bool ShouldMigrate(
        BaseItem baseItem,
        List<(SavedItemProperty Property, int SubType, int Value)> damageProperties,
        List<(SavedItemProperty Property, int SubType)> damageTypeProperties,
        CombatDamageType damageType)
    {
        if (Live.ShouldRemoveWeaponDamageType(baseItem) && damageTypeProperties.Count > 0)
            return true;

        if (damageProperties.Count != 1 ||
            damageProperties.Any(x => x.SubType != -1 && x.SubType != 0 && x.SubType != ushort.MaxValue))
            return true;

        if (damageType.IsPhysicalDamageType())
            return damageTypeProperties.Count > 0;

        return damageTypeProperties.Count != 1 ||
               !Live.TryGetCharacterDamageType(damageTypeProperties[0].SubType, out var existingDamageType) ||
               existingDamageType != damageType;
    }

    private static CombatDamageType ResolveDamageType(
        IEnumerable<(SavedItemProperty Property, int SubType, int Value)> damageProperties,
        IEnumerable<(SavedItemProperty Property, int SubType)> damageTypeProperties)
    {
        var typedDamageAmounts = new Dictionary<CombatDamageType, int>();
        foreach (var property in damageProperties)
        {
            if (!Live.TryGetCharacterDamageType(property.SubType, out var damageType))
                continue;

            typedDamageAmounts[damageType] = typedDamageAmounts.TryGetValue(damageType, out var amount)
                ? amount + property.Value
                : property.Value;
        }

        if (typedDamageAmounts.Count > 0)
        {
            var elementalDamageType = typedDamageAmounts
                .Where(x => x.Key.IsElementalDamageType())
                .OrderByDescending(x => x.Value)
                .Select(x => x.Key)
                .FirstOrDefault();

            if (elementalDamageType != CombatDamageType.Invalid)
                return elementalDamageType;
        }

        foreach (var property in damageTypeProperties)
        {
            if (Live.TryGetCharacterDamageType(property.SubType, out var damageType) && damageType.IsElementalDamageType())
                return damageType;
        }

        return CombatDamageType.Physical;
    }

    /// <summary>
    /// Saved-data <see cref="BasicVibrobladeCompatibility.Normalize"/>: gives the retired basic weapon templates
    /// their default name and any missing damage, delay and skill requirement.
    /// </summary>
    private static bool NormalizeBasicWeapon(SavedItem item)
    {
        if (!BasicVibrobladeCompatibility.TryGetProfile(item.BaseItem, item.ResRef, out var profile))
            return false;
        var changed = false;
        // Only replace the default name. Preserve all deliberate player/DM custom names.
        if (profile.DefaultName != null && StoredObjectData.ReadLocalizedName(item.Node) == profile.DefaultName)
        {
            SetName(item, profile.DisplayName);
            changed = true;
        }
        var properties = item.Properties.Select(property => property.Type).ToHashSet();
        // Retired basic templates can survive without SWLOR DMG. Restore missing
        // fields while preserving existing properties, appearance, identity and local data.
        foreach (var property in BasicVibrobladeCompatibility.GetMissingProperties(item.BaseItem, item.ResRef, properties))
        {
            item.AddProperty(property.Type, property.Subtype, property.Value, AddItemPropertyPolicy.IgnoreExisting);
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// SetName on a saved item: the localized name becomes the one string, with no string reference, in the
    /// language and gender of the name it replaces.
    /// </summary>
    private static void SetName(SavedItem item, string name)
    {
        var field = item.Node.Fields.SingleOrDefault(x => x.Name == "LocalizedName" && x.Type == 12);
        var language = field != null && field.Data.Length >= 20 && BinaryPrimitives.ReadUInt32LittleEndian(field.Data.AsSpan(8)) > 0
            ? BinaryPrimitives.ReadUInt32LittleEndian(field.Data.AsSpan(12))
            : 0u;
        var text = Encoding.UTF8.GetBytes(name);
        var data = new byte[20 + text.Length];
        BinaryPrimitives.WriteUInt32LittleEndian(data, (uint)(data.Length - 4));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), uint.MaxValue);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12), language);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16), (uint)text.Length);
        text.CopyTo(data, 20);
        if (field == null)
            item.Node.Fields.Add(StoredObjectData.MakeField(12, "LocalizedName", data));
        else
            field.Data = data;
    }

    private static bool MigrateConstructedDroid(SavedItem item)
    {
        var serialized = item.GetLocalString(Live.ConstructedDroidVariable);
        if (string.IsNullOrWhiteSpace(serialized))
            return false;

        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
        if (droid == null)
            return false;

        var migrated = false;
        migrated |= MigrateSavedItem(droid.SerializedCPU, value => droid.SerializedCPU = value);
        migrated |= MigrateSavedItem(droid.SerializedHead, value => droid.SerializedHead = value);
        migrated |= MigrateSavedItem(droid.SerializedBody, value => droid.SerializedBody = value);
        migrated |= MigrateSavedItem(droid.SerializedArms, value => droid.SerializedArms = value);
        migrated |= MigrateSavedItem(droid.SerializedLegs, value => droid.SerializedLegs = value);
        migrated |= MigrateSavedItems(droid.EquippedItems);
        migrated |= MigrateSavedItems(droid.Inventory);

        if (!migrated)
            return false;

        item.SetLocalString(Live.ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        return true;
    }

    private static bool MigrateSavedItem(string data, Action<string> setData)
    {
        if (!SavedItemDocument.Migrate(data, MigrateDocument, out var migrated))
            return false;

        setData(migrated);
        return true;
    }

    private static bool MigrateSavedItems<TKey>(Dictionary<TKey, string> items)
    {
        if (items == null)
            return false;

        var changed = false;
        foreach (var key in items.Keys.ToList())
            changed |= MigrateSavedItem(items[key], value => items[key] = value);
        return changed;
    }

    private static bool MigrateDocument(SavedItemDocument document)
    {
        var changed = false;
        foreach (var item in document.AllItems.ToArray())
            changed |= MigrateItem(item);
        return changed;
    }
}
