using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.MigrationService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using Node = SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData.Node;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration;

/// <summary>
/// Saved-data <see cref="StoredItemDataMigration.MigrateDroidItems(uint)"/>: replaces retired droid weapon boost
/// blueprints and recipe books, converts legacy droid stat and enhancement properties, adds the Armor skill rank
/// droid CPUs and controllers lack, and applies the same changes to the droid saved on a controller item (its
/// parts, equipment and inventory, which are saved items kept on their own).
/// </summary>
internal static class SavedDroidBoostItemMigration
{
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
    private const string DroidItemIdVariable = "DROID_ITEM_ID";
    private const string RecipesVariable = "RECIPES";
    private const string BlueprintRecipeIdVariable = StoredItemDataMigration.DroidBoostStoredItemMigration.BlueprintRecipeIdVariable;

    private static IReadOnlyDictionary<int, string> _blueprintNames;

    /// <summary>
    /// Main thread only: reads the blueprint name of every replacement droid boost recipe, which the engine
    /// builds from the recipe's item name.
    /// </summary>
    public static void Load()
    {
        var names = new Dictionary<int, string>();
        foreach (var recipeId in DroidBoostRecipeMigration.NewRecipeIdsByName.Values)
        {
            var recipeType = (RecipeType)recipeId;
            if (Craft.RecipeExists(recipeType))
                names[recipeId] = $"Blueprint: {Cache.GetItemNameByResref(Craft.GetRecipe(recipeType).Resref)}";
        }
        Load(names);
    }

    /// <summary>Supplies the blueprint names (by recipe ID) directly, for tests and for callers that read them elsewhere.</summary>
    public static void Load(IReadOnlyDictionary<int, string> blueprintNames) => _blueprintNames = blueprintNames;

    /// <summary>Migrates every equipped and carried item, including container contents, once.</summary>
    internal static bool MigrateCharacter(SavedCharacter character)
    {
        var changed = false;
        foreach (var item in character.Equipped.Concat(character.Carried).ToArray())
            changed |= MigrateItemTree(character, item);
        return changed;
    }

    /// <summary>
    /// Migrates a saved item kept on its own and the items inside its containers. Droid boost blueprints inside a
    /// container are expanded in that container; the item itself has no holder, so it is never expanded.
    /// </summary>
    internal static bool MigrateDocument(SavedItemDocument document) => MigrateItemTree(null, document.Root);

    /// <summary>
    /// Migrates an item and the items inside it. <paramref name="character"/> is null for items saved on a droid,
    /// whose containers have no overflow storage to fall back on.
    /// </summary>
    private static bool MigrateItemTree(SavedCharacter character, SavedItem item)
    {
        var wasMigrated = ExpandCarriedBlueprint(character, item);
        wasMigrated |= MigrateItem(item);

        foreach (var content in item.Contents.ToArray())
            wasMigrated |= MigrateItemTree(character, content);

        return wasMigrated;
    }

    /// <summary>
    /// Adds the additional carried blueprint variants next to the item, then updates the item to the first
    /// variant. Each variant is a copy of the unmigrated item with its own recipe and name, migrated like the item.
    /// </summary>
    private static bool ExpandCarriedBlueprint(SavedCharacter character, SavedItem item)
    {
        // An item saved on its own has no holder to place variants in.
        var hasHolder = item.Container != null || item.List != null;
        if (!hasHolder || !TryGetReplacementBlueprintRecipeIds(item, out var recipeIds))
            return false;

        foreach (var recipeId in recipeIds.Skip(1))
        {
            var copy = CopyItem(item);
            copy.SetLocalInt(BlueprintRecipeIdVariable, recipeId);
            SetName(copy, GetBlueprintName(recipeId));
            MigrateItemTree(character, copy);
            PlaceCopy(character, item, copy.Node);
        }

        item.SetLocalInt(BlueprintRecipeIdVariable, recipeIds[0]);
        SetName(item, GetBlueprintName(recipeIds[0]));
        return true;
    }

    /// <summary>
    /// <c>CopyItem(item, possessor, true)</c> before it is placed: a deep copy with the item's locals. A copy gets a
    /// new identity, so the saved UUIDs are dropped and the engine assigns new ones when the file loads.
    /// </summary>
    private static SavedItem CopyItem(SavedItem item)
    {
        var copy = StoredObjectData.ParseItem(StoredObjectData.ItemToUti(item.Node));
        RemoveIdentities(copy);
        return new SavedItem(copy, null, null);
    }

    private static void RemoveIdentities(Node item)
    {
        item.Fields.RemoveAll(x => x.Name == "UUID" && x.Type == 10);
        foreach (var list in item.Fields.Where(StoredObjectData.IsInventoryList))
        foreach (var content in list.Children)
            RemoveIdentities(content);
    }

    /// <summary>
    /// Places a copy where the engine puts one made for the item's possessor: the containing bag, otherwise the
    /// character's inventory. A character's copy that has no room goes to overflow storage; a droid's container
    /// has none, so the migration fails.
    /// </summary>
    private static void PlaceCopy(SavedCharacter character, SavedItem item, Node copy)
    {
        if (character != null)
        {
            if (item.Container == null)
                character.AddToInventory(copy);
            else
                character.AddToContainer(item.Container, copy);
            return;
        }

        var list = SavedGff.List(item.Container.Node, "ItemList", true);
        if (!SavedCharacter.Place(list, SavedCharacterData.ContainerWidth, SavedCharacterData.ContainerHeight, copy))
            throw new InvalidOperationException("The new droid blueprint did not fit in its migration container.");
        StoredObjectData.NumberListElements(list);
    }

    private static bool MigrateItem(SavedItem item)
    {
        var wasMigrated = MigrateRecipeLocalVariable(item);
        wasMigrated |= MigrateBlueprintRecipeLocalVariable(item);
        wasMigrated |= MigrateConstructedDroidLocalVariable(item);
        var replacements = new List<(SavedItemProperty Property, ItemPropertyType Type, int Value, int[] SubTypes)>();
        var tier = 0;
        var hasArmorStat = false;
        var isDroidCpu = false;

        foreach (var property in item.Properties)
        {
            var propertyType = property.Type;
            var subType = property.Subtype;
            var value = property.CostValue;

            if (propertyType == ItemPropertyType.DroidStat)
            {
                if (subType == (int)DroidStatSubType.Tier)
                    tier = value;
                else if (subType == (int)DroidStatSubType.Armor)
                    hasArmorStat = true;

                if (StoredItemDataMigration.DroidBoostStoredItemMigration.LegacyDroidStatMap.TryGetValue(subType, out var droidStatSubTypes))
                    replacements.Add((property, propertyType, value, droidStatSubTypes));
            }
            else if (propertyType == ItemPropertyType.DroidEnhancement &&
                     StoredItemDataMigration.DroidBoostStoredItemMigration.LegacyDroidEnhancementMap.TryGetValue(subType, out var enhancementSubTypes))
            {
                replacements.Add((property, propertyType, value, enhancementSubTypes));
            }
            else if (propertyType == ItemPropertyType.DroidPart &&
                     subType == (int)DroidPartItemPropertySubType.CPU)
            {
                isDroidCpu = true;
            }
        }

        foreach (var (property, _, _, _) in replacements)
        {
            item.RemoveProperty(property);
        }

        foreach (var (_, propertyType, value, subTypes) in replacements)
        {
            foreach (var subType in subTypes)
            {
                item.AddProperty(propertyType, subType, value);
            }
        }

        var isConstructedDroidController =
            item.ResRef == Droid.DroidControlItemResref &&
            !string.IsNullOrWhiteSpace(item.GetLocalString(ConstructedDroidVariable));
        var addedArmorStat = AddArmorSkillRankIfNeeded(item, isDroidCpu || isConstructedDroidController, hasArmorStat, tier);

        return wasMigrated || replacements.Count > 0 || addedArmorStat;
    }

    private static bool AddArmorSkillRankIfNeeded(SavedItem item, bool isDroidStatSource, bool hasArmorStat, int tier)
    {
        if (!isDroidStatSource || hasArmorStat)
            return false;

        var armorRank = StoredItemDataMigration.DroidBoostStoredItemMigration.GetTierArmorSkillRank(tier);
        if (armorRank <= 0)
            return false;

        item.AddProperty(ItemPropertyType.DroidStat, (int)DroidStatSubType.Armor, armorRank);
        return true;
    }

    /// <summary>
    /// Converts serialized droid components, inventory, and equipment while retaining their stable inventory keys.
    /// </summary>
    private static bool MigrateConstructedDroidLocalVariable(SavedItem item)
    {
        var serialized = item.GetLocalString(ConstructedDroidVariable);
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

        if (droid.EquippedItems != null)
        {
            foreach (var key in droid.EquippedItems.Keys.ToList())
            {
                var value = droid.EquippedItems[key];
                if (!MigrateSerializedObject(value, out var migratedValue))
                    continue;

                droid.EquippedItems[key] = migratedValue;
                migrated = true;
            }
        }

        if (droid.Inventory != null)
        {
            foreach (var key in droid.Inventory.Keys.ToList())
            {
                var value = droid.Inventory[key];
                if (TryMigrateRootBlueprintVariants(value, out var variants, out var recipeIds))
                {
                    droid.Inventory[key] = variants[0];
                    for (var index = 1; index < variants.Count; index++)
                    {
                        var variantId = StoredItemDataMigration.GetVariantId(key, recipeIds[index]);
                        var variant = new SavedItemDocument(variants[index]);
                        variant.Root.SetLocalString(DroidItemIdVariable, variantId);
                        droid.Inventory[variantId] = variant.Serialize();
                    }

                    migrated = true;
                    continue;
                }

                if (!MigrateSerializedObject(value, out var migratedValue))
                    continue;

                droid.Inventory[key] = migratedValue;
                migrated = true;
            }
        }

        if (!migrated)
            return false;

        item.SetLocalString(ConstructedDroidVariable, JsonConvert.SerializeObject(droid));
        return true;
    }

    private static bool MigrateSerializedObjectField(string serializedObject, Action<string> setSerializedObject)
    {
        if (!MigrateSerializedObject(serializedObject, out var migratedSerializedObject))
            return false;

        setSerializedObject(migratedSerializedObject);
        return true;
    }

    /// <summary>
    /// Migrates a saved droid item; unchanged payloads remain intact. The equipment requirement rule runs first.
    /// </summary>
    private static bool MigrateSerializedObject(string serializedObject, out string migratedSerializedObject) =>
        SavedItemDocument.Migrate(serializedObject, MigrateDroidDocument, out migratedSerializedObject);

    private static bool MigrateDroidDocument(SavedItemDocument document)
    {
        var wasMigrated = SavedEquipmentRequirementMigration.MigrateDocument(document);
        wasMigrated |= MigrateDocument(document);
        return wasMigrated;
    }

    /// <summary>
    /// Expands an eligible saved blueprint into one serialized variant per replacement recipe. The blueprint is
    /// migrated first, as the stored-item pipeline does; the other pipeline rules apply to each variant on their own.
    /// </summary>
    private static bool TryMigrateRootBlueprintVariants(
        string serializedObject, out List<string> migratedSerializedObjects, out int[] newRecipeIds)
    {
        migratedSerializedObjects = null;
        newRecipeIds = null;

        if (string.IsNullOrWhiteSpace(serializedObject) || StoredObjectData.IsCreatureData(serializedObject))
            return false;

        var document = new SavedItemDocument(serializedObject);
        var root = document.Root;
        var recipeId = root.GetLocalInt(BlueprintRecipeIdVariable);
        if (recipeId <= 0 ||
            !DroidBoostRecipeMigration.TryGetReplacementRecipeNames(recipeId.ToString(), out var recipeNames) ||
            DroidBoostRecipeMigration.GetReplacementRecipeIds(recipeNames).Count() <= 1)
            return false;

        if (ObsoleteItemMigration.IsObsoleteResRef(root.ResRef) ||
            !TryGetReplacementBlueprintRecipeIds(root, out newRecipeIds) ||
            newRecipeIds.Length <= 1)
        {
            return false;
        }

        _ = MigrateDroidDocument(document);

        migratedSerializedObjects = new List<string>();
        foreach (var newRecipeId in newRecipeIds)
        {
            root.SetLocalInt(BlueprintRecipeIdVariable, newRecipeId);
            SetName(root, GetBlueprintName(newRecipeId));
            migratedSerializedObjects.Add(document.Serialize());
        }

        return true;
    }

    private static bool MigrateRecipeLocalVariable(SavedItem item)
    {
        var previousRecipeList = item.GetLocalString(RecipesVariable);
        var recipeList = StoredItemDataMigration.ExpandTrainingRecipeBook(item.ResRef, previousRecipeList);
        if (string.IsNullOrWhiteSpace(recipeList))
            return false;

        var migrated = recipeList != previousRecipeList;
        var newRecipeIds = new List<int>();

        foreach (var recipeId in recipeList.Split(','))
        {
            if (!int.TryParse(recipeId, out var parsedId) ||
                !DroidBoostRecipeMigration.TryGetReplacementRecipeNames(parsedId.ToString(), out var newRecipeNames))
            {
                if (int.TryParse(recipeId, out parsedId))
                    newRecipeIds.Add(parsedId);
                continue;
            }

            foreach (var newRecipeName in newRecipeNames)
            {
                newRecipeIds.AddRange(DroidBoostRecipeMigration.GetReplacementRecipeIds(new[] { newRecipeName }));
            }

            migrated = true;
        }

        if (!migrated)
            return false;

        item.SetLocalString(RecipesVariable, string.Join(",", newRecipeIds.Distinct()));
        return true;
    }

    private static bool MigrateBlueprintRecipeLocalVariable(SavedItem item)
    {
        if (!TryGetReplacementBlueprintRecipeIds(item, out var newRecipeIds))
            return false;

        item.SetLocalInt(BlueprintRecipeIdVariable, newRecipeIds[0]);
        return true;
    }

    private static bool TryGetReplacementBlueprintRecipeIds(SavedItem item, out int[] newRecipeIds)
    {
        newRecipeIds = null;

        var recipeId = item.GetLocalInt(BlueprintRecipeIdVariable);
        if (recipeId <= 0 ||
            !DroidBoostRecipeMigration.TryGetReplacementRecipeNames(recipeId.ToString(), out var newRecipeNames))
        {
            return false;
        }

        newRecipeIds = DroidBoostRecipeMigration.GetReplacementRecipeIds(newRecipeNames).ToArray();
        return newRecipeIds.Length > 0;
    }

    /// <summary>
    /// The name of a replacement blueprint. The engine looks the recipe up with Craft.GetRecipe, which fails for a
    /// recipe that was never registered; so does this.
    /// </summary>
    private static string GetBlueprintName(int recipeId)
    {
        if (_blueprintNames == null)
            throw new InvalidOperationException("Saved droid blueprint names are not loaded.");
        return _blueprintNames.TryGetValue(recipeId, out var name)
            ? name
            : throw new InvalidOperationException($"No blueprint name is loaded for recipe {recipeId}.");
    }

    /// <summary>
    /// SetName on saved data: every saved string of the item's localized name becomes <paramref name="name"/>, and
    /// a name saved only as a string reference gets a language-0 string. The string reference is kept.
    /// </summary>
    private static void SetName(SavedItem item, string name)
    {
        var field = item.Node.Fields.SingleOrDefault(x => x.Name == "LocalizedName" && x.Type == 12);
        var languages = new List<uint>();
        if (field != null && field.Data.Length >= 12)
        {
            var count = BinaryPrimitives.ReadUInt32LittleEndian(field.Data.AsSpan(8));
            var offset = 12;
            for (var index = 0u; index < count && offset + 8 <= field.Data.Length; index++)
            {
                languages.Add(BinaryPrimitives.ReadUInt32LittleEndian(field.Data.AsSpan(offset)));
                offset += 8 + (int)BinaryPrimitives.ReadUInt32LittleEndian(field.Data.AsSpan(offset + 4));
            }
        }
        if (languages.Count == 0)
            languages.Add(0);

        var text = Encoding.UTF8.GetBytes(name);
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output, Encoding.UTF8, true);
        writer.Write(0u);
        writer.Write(StoredObjectData.ReadLocalizedNameReference(item.Node));
        writer.Write((uint)languages.Count);
        foreach (var language in languages)
        {
            writer.Write(language);
            writer.Write(text.Length);
            writer.Write(text);
        }
        writer.Flush();

        var data = output.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(data, data.Length - 4);
        if (field == null)
            item.Node.Fields.Add(StoredObjectData.MakeField(12, "LocalizedName", data));
        else
            field.Data = data;
    }
}
