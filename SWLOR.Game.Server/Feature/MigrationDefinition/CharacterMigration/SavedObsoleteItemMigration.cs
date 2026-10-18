using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="ObsoleteItemMigration.RemoveObsoleteItemsFromObject(uint)"/>: removes retired items (old
/// instruction discs and recipes), converts retired upgrade kits to their replacement, and reconciles the
/// instructions of droids saved on controller items.
/// </summary>
internal static class SavedObsoleteItemMigration
{
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
    private const string DroidItemIdVariable = "DROID_ITEM_ID";

    /// <summary>Every blueprint a conversion creates, for <see cref="SavedCharacterData"/> to load.</summary>
    internal static IEnumerable<string> ReplacementBlueprints => ObsoleteItemMigration.ConversionResRefs;

    public static void MigrateCharacter(SavedCharacter character)
    {
        foreach (var item in character.Equipped.ToArray())
            Visit(character, item);
        foreach (var item in character.Carried.ToArray())
            Visit(character, item);
    }

    private static void Visit(SavedCharacter character, SavedItem item)
    {
        if (ObsoleteItemMigration.TryGetConversionResRef(item.ResRef, out var replacement))
        {
            character.Replace(item, replacement);
            return;
        }
        if (ObsoleteItemMigration.IsObsoleteResRef(item.ResRef))
        {
            character.Destroy(item);
            if (item.Container != null)
                StoredObjectData.NumberListElements(item.List);
            return;
        }
        MigrateDroid(item);
        foreach (var content in item.Contents.ToArray())
            Visit(character, content);
    }

    /// <summary>
    /// A saved item outside the character (a droid part or droid item): replaced when it converts, removed when it
    /// is retired, otherwise with its retired contents removed. Returns whether anything changed.
    /// </summary>
    private static bool MigrateSerialized(string data, out string migrated, out bool removedRoot)
    {
        migrated = data;
        removedRoot = false;
        if (string.IsNullOrWhiteSpace(data) || StoredObjectData.IsCreatureData(data))
            return false;
        var document = new SavedItemDocument(data);
        var root = document.Root;
        if (ObsoleteItemMigration.TryGetConversionResRef(root.ResRef, out var replacement))
        {
            migrated = StoredObjectData.ItemToUti(CreateReplacement(root, replacement));
            return true;
        }
        if (ObsoleteItemMigration.IsObsoleteResRef(root.ResRef))
        {
            removedRoot = true;
            return true;
        }
        var changed = MigrateDroid(root) | MigrateContents(root);
        if (changed)
            migrated = document.Serialize();
        return changed;
    }

    private static bool MigrateContents(SavedItem container)
    {
        var changed = false;
        foreach (var content in container.Contents.ToArray())
        {
            if (ObsoleteItemMigration.TryGetConversionResRef(content.ResRef, out var replacement))
            {
                var created = CreateReplacement(content, replacement);
                if (!SavedCharacter.Place(content.List, SavedCharacterData.ContainerWidth, SavedCharacterData.ContainerHeight, created))
                    throw new InvalidOperationException($"A saved container has no room for replacement '{replacement}'.");
                content.List.Children.Remove(content.Node);
                StoredObjectData.NumberListElements(content.List);
                changed = true;
                continue;
            }
            if (ObsoleteItemMigration.IsObsoleteResRef(content.ResRef))
            {
                content.List.Children.Remove(content.Node);
                StoredObjectData.NumberListElements(content.List);
                changed = true;
                continue;
            }
            changed |= MigrateDroid(content) | MigrateContents(content);
        }
        return changed;
    }

    /// <summary>A new item from the replacement blueprint with the old stack size and droid item identity.</summary>
    private static StoredObjectData.Node CreateReplacement(SavedItem item, string replacement)
    {
        var created = StoredObjectData.ParseItem(SavedCharacterData.GetBlueprint(replacement));
        var saved = new SavedItem(created, null, null) { StackSize = Math.Max(1, item.StackSize) };
        var droidItemId = item.GetLocalString(DroidItemIdVariable);
        if (!string.IsNullOrWhiteSpace(droidItemId))
            saved.SetLocalString(DroidItemIdVariable, droidItemId);
        return created;
    }

    /// <summary>
    /// Saved-data <c>RemoveObsoleteItemsFromConstructedDroid</c>: retires droid parts and items, drops historical
    /// instructions whose IDs now mean other abilities, merges the controller's instruction properties, and
    /// normalizes the droid's instructions to its tier and AI slots.
    /// </summary>
    private static bool MigrateDroid(SavedItem controller)
    {
        var serialized = controller.GetLocalString(ConstructedDroidVariable);
        if (string.IsNullOrWhiteSpace(serialized))
            return false;
        var droid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
        if (droid == null)
            return false;

        var changed = false;
        string Part(string data)
        {
            if (!MigrateSerialized(data, out var migrated, out var removedRoot))
                return data;
            changed = true;
            return removedRoot ? string.Empty : migrated;
        }
        droid.SerializedCPU = Part(droid.SerializedCPU);
        droid.SerializedHead = Part(droid.SerializedHead);
        droid.SerializedBody = Part(droid.SerializedBody);
        droid.SerializedArms = Part(droid.SerializedArms);
        droid.SerializedLegs = Part(droid.SerializedLegs);
        changed |= MigrateDictionary(droid.EquippedItems);
        changed |= MigrateDictionary(droid.Inventory);

        droid.ActivePerks ??= new List<DroidPerk>();
        var droidStateChanged = ObsoleteItemMigration.RemoveReassignedDroidPerks(droid.ActivePerks);
        droidStateChanged |= ObsoleteItemMigration.RemoveReassignedDroidPerks(droid.LearnedPerks);
        droidStateChanged |= ObsoleteItemMigration.MergeDroidPerks(droid.ActivePerks, LoadInstructionProperties(controller));
        var (tier, aiSlots) = LoadTierAndSlots(controller);
        droidStateChanged |= DroidInstructions.Normalize(droid, tier, aiSlots);
        droidStateChanged |= SyncInstructionProperties(controller, droid.ActivePerks);
        changed |= droidStateChanged;
        if (changed)
            controller.SetLocalString(ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        return changed;
    }

    private static bool MigrateDictionary<TKey>(Dictionary<TKey, string> items)
    {
        if (items == null || items.Count <= 0)
            return false;
        var changed = false;
        foreach (var key in items.Keys.ToList())
        {
            if (!MigrateSerialized(items[key], out var migrated, out var removedRoot))
                continue;
            if (removedRoot)
                items.Remove(key);
            else
                items[key] = migrated;
            changed = true;
        }
        return changed;
    }

    /// <summary>The controller's instruction properties, without reassigned historical IDs, normalized.</summary>
    private static List<DroidPerk> LoadInstructionProperties(SavedItem controller)
    {
        var result = new List<DroidPerk>();
        foreach (var property in controller.Properties.Where(property => property.Type == ItemPropertyType.DroidInstruction))
        {
            var perk = (PerkType)property.Subtype;
            if (!LegacyPerkRefundMigration.IsReassignedLegacyPerk(perk) &&
                DroidInstructions.TryNormalize(new DroidPerk(perk, property.CostValue), out var normalized))
                result.Add(normalized);
        }
        return result;
    }

    /// <summary>The tier and AI slots <see cref="Service.Droid.LoadDroidItemPropertyDetails"/> reads from a controller.</summary>
    private static (int Tier, int AISlots) LoadTierAndSlots(SavedItem controller)
    {
        var tier = 0;
        var slots = 0;
        foreach (var property in controller.Properties.Where(property => property.Type == ItemPropertyType.DroidStat))
        {
            if (property.Subtype == (int)DroidStatSubType.Tier)
                tier = Math.Clamp(property.CostValue, 1, 5);
            else if (property.Subtype == (int)DroidStatSubType.AISlots)
                slots += property.CostValue;
        }
        return (tier, slots);
    }

    /// <summary>Makes the controller's instruction properties match its normalized active instructions, in order.</summary>
    private static bool SyncInstructionProperties(SavedItem controller, IReadOnlyList<DroidPerk> activePerks)
    {
        var existing = controller.Properties.Where(property => property.Type == ItemPropertyType.DroidInstruction).ToList();
        var existingPerks = existing.Select(property => new DroidPerk((PerkType)property.Subtype, property.CostValue)).ToList();
        if (ObsoleteItemMigration.AreEqualDroidPerks(existingPerks, activePerks))
            return false;
        foreach (var property in existing)
            controller.RemoveProperty(property);
        foreach (var perk in activePerks)
            controller.AddProperty(ItemPropertyType.DroidInstruction, (int)perk.Perk, perk.Level);
        return true;
    }
}
