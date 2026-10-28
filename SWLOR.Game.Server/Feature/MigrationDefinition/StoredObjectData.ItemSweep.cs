using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// The stored-item sweep applied to saved GFF with no game engine, so the server can convert stored records
/// on worker threads at boot. Each rule mirrors its native counterpart on an unequipped item: <see cref="HeadgearSlotMigration.ConvertItem"/>,
/// <see cref="EquipmentTypeMigration.ConvertItem"/>, <see cref="ItemIconMigration.MigrateItem"/> (with
/// <see cref="ItemAppearanceCatalogMigration.ConvertItem"/>), <see cref="LootStatRevisionMigration"/>'s stat
/// revision and <see cref="DroidWeaponSkillMigration.MigrateItem"/>, in the same order. Fields the sweep does not
/// change keep their saved bytes. New properties and locals are written the way the engine saves them.
/// </summary>
internal sealed partial class StoredObjectData
{
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

    /// <summary>itempropdef.2da CostTableResRef for every property type the stat revision catalogs and droid weapon skills add.</summary>
    internal static IReadOnlyDictionary<ItemPropertyType, byte> RevisedPropertyCostTables { get; } = new Dictionary<ItemPropertyType, byte>
    {
        [ItemPropertyType.HPBonus] = 37,
        [ItemPropertyType.Stamina] = 36,
        [ItemPropertyType.DMG] = 34,
        [ItemPropertyType.Defense] = 35,
        [ItemPropertyType.Attack] = 45,
        [ItemPropertyType.ForceAttack] = 45,
        [ItemPropertyType.Evasion] = 41,
        [ItemPropertyType.CombatReadiness] = 42,
        [ItemPropertyType.STMRegen] = 45,
        [ItemPropertyType.RequiresSkill] = 48,
        [ItemPropertyType.Resistance] = 54,
        [ItemPropertyType.DroidStat] = 45,
    };

    /// <summary>Saved-data counterpart of <see cref="LootStatRevisionMigration.MigrateStoredObjectData"/>.</summary>
    internal static bool MigrateStoredItemData(string original, out string migrated, bool repairHeadgear, Action<RootListing> rootChanged,
        bool updateArtwork = true)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original))
            return false;
        // Saved creatures keep authored stats and slots; only their items' artwork changes.
        if (IsCreatureData(original))
            return MigrateSavedArtwork(original, out migrated);

        var document = new StoredObjectData(Convert.FromBase64String(original));
        if (Encoding.ASCII.GetString(document._header, 0, 4) != "UTI ")
            throw new InvalidDataException($"Unsupported stored object type '{Encoding.ASCII.GetString(document._header, 0, 4)}'.");
        var root = document._root;
        var resref = ReadResRef(root);
        var baseItem = ReadBaseItem(root);
        var model = ReadSimpleModel(root);
        var refreshRootMetadata = repairHeadgear &&
            (HeadgearSlotMigration.IsLegacyHeadgear(resref, baseItem) ||
             HeadgearSlotMigration.GetCorrectedModel(resref, baseItem, model) != model ||
             EquipmentTypeMigration.IsLegacyEquipment(resref, baseItem)) ||
            IsLegacyAppearance(root);
        if (!MigrateSavedItem(root, repairHeadgear, new HashSet<Node>(), updateArtwork))
            return false;
        if (refreshRootMetadata)
            rootChanged?.Invoke(GetRootListing(root));
        migrated = document.Serialize();
        return true;
    }

    /// <summary>Saved-data counterpart of <see cref="ItemIconMigration.MigrateSerializedObject"/>: artwork only.</summary>
    internal static bool MigrateSavedArtwork(string original, out string migrated)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original))
            return false;
        var document = new StoredObjectData(Convert.FromBase64String(original));
        var visited = new HashSet<Node>();
        bool Visit(Node node, bool isItem)
        {
            if (!visited.Add(node)) return false;
            var changed = false;
            if (isItem)
            {
                changed |= UpdateArtwork(node);
                var droid = FindLocal(node, ConstructedDroidVariable, 3);
                var saved = droid == null ? null : ReadText(droid);
                if (ItemIconMigration.MigrateDroidArtwork(saved, MigrateSavedArtwork, out var updated))
                {
                    WriteText(droid, updated);
                    changed = true;
                }
            }
            foreach (var list in node.Fields.Where(IsInventoryList))
            foreach (var item in list.Children)
                changed |= Visit(item, true);
            return changed;
        }
        if (!Visit(document._root, !document.IsCreature))
            return false;
        migrated = document.Serialize();
        return true;
    }

    internal static bool MigrateSavedItem(Node item, bool repairHeadgear, HashSet<Node> visited, bool updateArtwork = true)
    {
        if (!visited.Add(item)) return false;
        var changed = false;
        if (repairHeadgear)
        {
            changed |= ConvertHeadgear(item);
            changed |= ConvertEquipmentType(item);
        }
        if (updateArtwork) changed |= UpdateArtwork(item);
        changed |= ReviseStats(item);
        if (updateArtwork) changed |= AddDroidWeaponSkills(item);
        changed |= MigrateSavedDroid(item, repairHeadgear, updateArtwork);

        foreach (var list in item.Fields.Where(IsInventoryList))
        foreach (var child in list.Children)
            changed |= MigrateSavedItem(child, repairHeadgear, visited, updateArtwork);
        return changed;
    }

    /// <summary>The droid saved on a controller item: its parts, equipment and inventory get the same rules.</summary>
    internal static bool MigrateSavedDroid(Node item, bool repairHeadgear, bool updateArtwork)
    {
        var droid = FindLocal(item, ConstructedDroidVariable, 3);
        if (droid == null)
            return false;
        var original = ReadText(droid);
        var migrated = HeadgearSlotMigration.MigrateDroid(original, data =>
            MigrateStoredItemData(data, out var result, repairHeadgear, null, updateArtwork) ? result : data, repairHeadgear);
        if (original == migrated)
            return false;
        WriteText(droid, migrated);
        return true;
    }

    internal static bool ConvertHeadgear(Node item)
    {
        var resref = ReadResRef(item);
        var baseItem = ReadBaseItem(item);
        if (baseItem == BaseItem.Helmet)
        {
            var model = ReadSimpleModel(item);
            var corrected = HeadgearSlotMigration.GetCorrectedModel(resref, baseItem, model);
            if (corrected == model) return false;
            WriteSimpleModel(item, corrected);
            return true;
        }
        if (!HeadgearSlotMigration.IsLegacyHeadgear(resref, baseItem)) return false;
        WriteInteger(item, "BaseItem", (int)BaseItem.Helmet);
        var current = ReadSimpleModel(item);
        var fallback = HeadgearSlotMigration.GetCorrectedModel(resref, baseItem, current);
        if (fallback != current)
            WriteSimpleModel(item, fallback);
        return true;
    }

    internal static bool ConvertEquipmentType(Node item)
    {
        var resref = ReadResRef(item);
        if (!EquipmentTypeMigration.IsLegacyEquipment(resref, ReadBaseItem(item))) return false;
        var correction = EquipmentTypeMigration.Corrections[resref];
        WriteInteger(item, "BaseItem", (int)correction.Type);
        WriteSimpleModel(item, correction.Model);
        return true;
    }

    internal static bool UpdateArtwork(Node item)
    {
        var changed = ConvertAppearance(item);
        var current = ReadSimpleModel(item);
        var updated = ItemIconMigration.GetUpdatedModel(ReadResRef(item), (int)ReadBaseItem(item), current);
        if (updated == current) return changed;
        WriteSimpleModel(item, updated);
        return true;
    }

    // AppearanceArmor order.
    private static readonly string[] ArmorPartLabels =
    {
        "RFoot", "LFoot", "RShin", "LShin", "LThigh", "RThigh", "Pelvis", "Torso", "Belt", "Neck",
        "RFArm", "LFArm", "RBicep", "LBicep", "RShoul", "LShoul", "RHand", "LHand", "Robe",
    };

    /// <summary>Mirrors <see cref="ItemAppearanceCatalogMigration.IsLegacyAppearance"/>.</summary>
    private static bool IsLegacyAppearance(Node item)
    {
        if (!TryGetAppearanceRevision(item, out var revision))
            return false;
        var parts = ReadAppearanceParts(item, revision.Previous.Length);
        return Enumerable.Range(0, parts.Length)
            .All(index => revision.Current[index] == revision.Previous[index] || parts[index] == revision.Previous[index]);
    }

    /// <summary>Mirrors <see cref="ItemAppearanceCatalogMigration.ConvertItem"/> on an unequipped item.</summary>
    private static bool ConvertAppearance(Node item)
    {
        if (!IsLegacyAppearance(item))
            return false;
        var revision = ItemAppearanceCatalogMigration.Revisions[ReadResRef(item)];
        for (var index = 0; index < revision.Current.Length; index++)
            if (revision.Current[index] != revision.Previous[index])
                WritePart(item, PartLabel(revision.Current.Length, index), revision.Current[index]);
        return true;
    }

    private static bool TryGetAppearanceRevision(Node item, out (BaseItem BaseItem, int[] Previous, int[] Current) revision)
    {
        revision = default;
        var resref = ReadResRef(item);
        return resref != null && ItemAppearanceCatalogMigration.Revisions.TryGetValue(resref, out revision) &&
               revision.BaseItem == ReadBaseItem(item);
    }

    /// <summary>
    /// <see cref="ItemAppearanceCatalogMigration.ReadParts"/> from saved fields: a weapon part (model * 10 + color)
    /// is its saved model part, armor parts are the armor part fields, and anything else uses simple models.
    /// </summary>
    private static int[] ReadAppearanceParts(Node item, int count) =>
        Enumerable.Range(0, count).Select(index => ReadPart(item, PartLabel(count, index))).ToArray();

    private static string PartLabel(int count, int index) => count == ArmorPartLabels.Length
        ? "ArmorPart_" + ArmorPartLabels[index]
        : "ModelPart" + (index + 1);

    // GFF labels hold 16 characters, so long extended part names are saved truncated.
    private static string ExtendedLabel(string label) => ("x" + label).Length > 16 ? ("x" + label).Substring(0, 16) : "x" + label;

    /// <summary>The extended part field wins over the byte-sized one, as when the engine loads an item.</summary>
    internal static int ReadPart(Node item, string label)
    {
        var extended = item.Fields.SingleOrDefault(x => x.Name == ExtendedLabel(label));
        if (extended != null) return ReadScalar(extended);
        var part = item.Fields.SingleOrDefault(x => x.Name == label);
        return part == null ? 0 : ReadScalar(part);
    }

    /// <summary>
    /// Saves both the byte-sized and the extended part fields, as the engine does: a part above 255 leaves
    /// the byte-sized field at 0 and lives only in the extended one.
    /// </summary>
    internal static void WritePart(Node item, string label, int value)
    {
        void Set(uint type, string name, int data)
        {
            var field = MakeField(type, name, BitConverter.GetBytes(data));
            var index = item.Fields.FindIndex(x => x.Name == name);
            if (index >= 0) item.Fields[index] = field;
            else item.Fields.Add(field);
        }
        Set(0, label, value <= byte.MaxValue ? value : 0);
        Set(2, ExtendedLabel(label), value);
    }

    /// <summary>
    /// Mirrors <c>LootStatRevisionMigration.MigrateItem</c>. <paramref name="difference"/> receives how much each
    /// revised property's total changed, which an equipped item's wearer counts.
    /// </summary>
    internal static bool ReviseStats(Node item, Action<LootStatChange, int> difference = null)
    {
        var alreadyRevisedRare = ReadLocalInteger(item, RareLootStatRevisionCatalog.RevisionVariable) >= RareLootStatRevisionCatalog.CurrentRevision;
        var changed = ApplyRevision(item, LootStatRevisionCatalog.Changes, LootStatRevisionMigration.RevisionVariable, LootStatRevisionMigration.CurrentRevision,
            difference: difference);
        changed |= ApplyRevision(item, RareLootStatRevisionCatalog.Changes,
            RareLootStatRevisionCatalog.RevisionVariable, RareLootStatRevisionCatalog.CurrentRevision, difference: difference);
        if (alreadyRevisedRare)
            changed |= ApplyRevision(item, EquipmentTypeStatRevisionCatalog.Changes,
                EquipmentTypeStatRevisionCatalog.RevisionVariable, EquipmentTypeStatRevisionCatalog.CurrentRevision, difference: difference);
        else if (EquipmentTypeStatRevisionCatalog.Changes.ContainsKey(ReadResRef(item)) &&
                 ReadLocalInteger(item, EquipmentTypeStatRevisionCatalog.RevisionVariable) < EquipmentTypeStatRevisionCatalog.CurrentRevision)
        {
            // The revised rare catalog already applied the final cloak value.
            WriteLocalInteger(item, EquipmentTypeStatRevisionCatalog.RevisionVariable, EquipmentTypeStatRevisionCatalog.CurrentRevision);
            changed = true;
        }
        changed |= ApplyRevision(item, FoundryLootStatRevisionCatalog.Changes,
            FoundryLootStatRevisionCatalog.RevisionVariable, FoundryLootStatRevisionCatalog.CurrentRevision, difference: difference);
        changed |= ApplyRevision(item, ArkanianArmorStatRevisionCatalog.Changes,
            ArkanianArmorStatRevisionCatalog.RevisionVariable, ArkanianArmorStatRevisionCatalog.CurrentRevision,
            current => ReviseIceProtection(current, ArkanianArmorStatRevisionCatalog.IceResistance), difference);
        changed |= ApplyRevision(item, RetiredDefenseStatRevisionCatalog.Changes,
            RetiredDefenseStatRevisionCatalog.RevisionVariable, RetiredDefenseStatRevisionCatalog.CurrentRevision,
            ReviseRetiredDefense, difference);
        return changed;
    }

    private static bool ApplyRevision(Node item, IReadOnlyDictionary<string, LootStatChange[]> catalog, string revisionVariable, int revision,
        Func<Node, IEnumerable<LootStatChange>> itemChanges = null, Action<LootStatChange, int> difference = null)
    {
        if (!catalog.TryGetValue(ReadResRef(item), out var catalogChanges) || ReadLocalInteger(item, revisionVariable) >= revision)
            return false;
        // Item-specific changes read the item before any change applies.
        var changes = itemChanges == null ? catalogChanges : catalogChanges.Concat(itemChanges(item)).ToArray();
        foreach (var change in changes)
        {
            var changedBy = ApplyChange(item, change);
            if (changedBy != 0)
                difference?.Invoke(change, changedBy);
        }
        WriteLocalInteger(item, revisionVariable, revision);
        return true;
    }

    /// <summary>Mirrors <c>LootStatRevisionMigration.ReviseIceProtection</c>.</summary>
    private static IEnumerable<LootStatChange> ReviseIceProtection(Node item, int baseResistance)
    {
        var defense = ReadTotal(item, ItemPropertyType.Defense, (int)CombatDamageType.Ice).Total;
        var resistance = ReadTotal(item, ItemPropertyType.Resistance, (int)ResistanceType.Ice).Total;
        if (defense > 0)
            yield return new LootStatChange(ItemPropertyType.Defense, (int)CombatDamageType.Ice, defense, 0);
        var missingBase = defense > 0 ? defense : resistance < baseResistance ? baseResistance : 0;
        yield return new LootStatChange(ItemPropertyType.Resistance, (int)ResistanceType.Ice, resistance, resistance + missingBase);
    }

    /// <summary>Mirrors <c>LootStatRevisionMigration.ReviseRetiredDefense</c>.</summary>
    private static IEnumerable<LootStatChange> ReviseRetiredDefense(Node item) =>
        RetiredDefenseStatRevisionCatalog.ConvertedResistanceChanges(ReadResRef(item),
            subtype => ReadTotal(item, ItemPropertyType.Resistance, subtype).Total).ToArray();

    /// <summary>
    /// Shifts one property by the blueprint difference, as <c>LootStatRevisionMigration.ApplyChange</c> does, and
    /// returns how much the item's total changed.
    /// </summary>
    private static int ApplyChange(Node item, LootStatChange change)
    {
        var (total, matches) = ReadTotal(item, change.Type, change.Subtype);
        var properties = item.Fields.SingleOrDefault(x => x.Name == "PropertiesList" && x.Type == 15);
        var revised = Math.Max(0, total - change.Previous + change.Current);
        if (revised == total && matches.Count <= 1)
            return 0;

        if (properties == null)
        {
            properties = MakeField(15, "PropertiesList", children: new List<Node>());
            item.Fields.Add(properties);
        }
        properties.Children.RemoveAll(matches.Contains);
        if (revised > 0)
            properties.Children.Add(NewProperty(change,
                change.Type == ItemPropertyType.Resistance ? Resistance.EncodeItemPropertyCostTableValue(revised) : revised));
        NumberListElements(properties);
        return revised - total;
    }

    /// <summary>Mirrors <c>LootStatRevisionMigration.ReadTotal</c>; saved property lists hold only permanent properties.</summary>
    private static (int Total, List<Node> Matches) ReadTotal(Node item, ItemPropertyType type, int subtype)
    {
        var properties = item.Fields.SingleOrDefault(x => x.Name == "PropertiesList" && x.Type == 15);
        var matches = properties?.Children.Where(property =>
            ReadInteger(property, "PropertyName") == (int)type &&
            (!HasSubtype(type) || ReadInteger(property, "Subtype") == subtype)).ToList() ?? new List<Node>();
        var total = matches.Sum(property => type == ItemPropertyType.Resistance
            ? Resistance.DecodeItemPropertyCostTableValue(ReadInteger(property, "CostValue"))
            : ReadInteger(property, "CostValue"));
        return (total, matches);
    }

    /// <summary>Mirrors <see cref="DroidWeaponSkillMigration.MigrateItem"/>.</summary>
    internal static bool AddDroidWeaponSkills(Node item)
    {
        var properties = item.Fields.SingleOrDefault(x => x.Name == "PropertiesList" && x.Type == 15);
        var droidStats = properties?.Children
            .Where(property => ReadInteger(property, "PropertyName") == (int)ItemPropertyType.DroidStat)
            .Select(property => (ReadInteger(property, "Subtype"), ReadInteger(property, "CostValue")))
            .ToList();
        if (droidStats == null || droidStats.Count == 0)
            return false;

        var missing = DroidWeaponSkillMigration.GetMissingSkills(droidStats).ToList();
        foreach (var (skill, value) in missing)
            properties.Children.Add(NewProperty(ItemPropertyType.DroidStat, (int)skill, value));
        if (missing.Count == 0)
            return false;
        NumberListElements(properties);
        return true;
    }

    internal static bool HasSubtype(ItemPropertyType type) =>
        type is ItemPropertyType.Defense or ItemPropertyType.Resistance or ItemPropertyType.RequiresSkill;

    /// <summary>The property ItemPropertyCustom(type, subtype or -1, value, -1) saves as.</summary>
    private static Node NewProperty(LootStatChange change, int value) =>
        NewProperty(change.Type, HasSubtype(change.Type) ? change.Subtype : -1, value);

    /// <summary>The property ItemPropertyCustom(type, subtype, value, -1) saves as; a -1 subtype saves as 65535.</summary>
    private static Node NewProperty(ItemPropertyType type, int subtype, int value)
    {
        if (!RevisedPropertyCostTables.TryGetValue(type, out var costTable))
            throw new InvalidOperationException($"No cost table is known for revised property type {type}.");
        if (subtype < 0)
            subtype = ushort.MaxValue;
        return new Node
        {
            Fields = new List<Field>
            {
                MakeField(2, "PropertyName", BitConverter.GetBytes((int)type)),
                MakeField(2, "Subtype", BitConverter.GetBytes(subtype)),
                MakeField(0, "CostTable", BitConverter.GetBytes((int)costTable)),
                MakeField(2, "CostValue", BitConverter.GetBytes(value)),
                MakeField(0, "Param1", BitConverter.GetBytes((int)byte.MaxValue)),
                MakeField(0, "Param1Value", BitConverter.GetBytes((int)byte.MaxValue)),
                MakeField(0, "ChanceAppear", BitConverter.GetBytes(100)),
                MakeField(0, "UsesPerDay", BitConverter.GetBytes((int)byte.MaxValue)),
                MakeField(0, "Useable", BitConverter.GetBytes(1)),
                MakeField(10, "CustomTag", StringData(string.Empty)),
            }
        };
    }

    /// <summary>
    /// <see cref="Item.GetIconResref"/> for every root type a conversion produces. Helmets, cloaks, armor and
    /// composite weapons are not simple models, so their icon depends only on the base item; gloves and
    /// shuriken are simple models whose icon is named by model. Checked against the engine by
    /// <c>SavedDataItemSweepMatchesEngine</c>.
    /// </summary>
    internal static IReadOnlyDictionary<BaseItem, string> RootIcons { get; } = new Dictionary<BaseItem, string>
    {
        [BaseItem.Helmet] = "ihelm",
        [BaseItem.Cloak] = "iit_cloak",
        [BaseItem.Armor] = "iit_chest",
        [BaseItem.Longsword] = "iwswls",
        [BaseItem.GreatSword] = "iwswgs",
        [BaseItem.Dagger] = "iwswdg",
        [BaseItem.ShortSpear] = "iwplss",
        [BaseItem.QuarterStaff] = "iwdbqs",
        [BaseItem.Rifle] = "iwbwxl",
        [BaseItem.Sling] = "iwbwsh",
    };

    internal static IReadOnlyDictionary<(BaseItem BaseItem, int Model), string> SimpleModelRootIcons { get; } =
        new Dictionary<(BaseItem, int), string>
        {
            [(BaseItem.Gloves, 126)] = "iit_glove_126",
            [(BaseItem.Gloves, 176)] = "iit_glove_176",
            [(BaseItem.Shuriken, 42)] = "iwthsh_042",
        };

    private static RootListing GetRootListing(Node root)
    {
        var baseItem = ReadBaseItem(root);
        var model = ReadSimpleModel(root);
        if (!RootIcons.TryGetValue(baseItem, out var icon) && !SimpleModelRootIcons.TryGetValue((baseItem, model), out icon))
            throw new InvalidOperationException($"No saved-data icon is known for converted base item {baseItem} with model {model}.");
        var category = PlayerMarket.GetBaseItemMarketCategory(baseItem)
            ?? throw new InvalidOperationException($"Converted base item {baseItem} has no base-item market category.");
        return new RootListing(icon, category);
    }

    internal static string ReadResRef(Node item)
    {
        var field = item.Fields.SingleOrDefault(x => x.Name == "TemplateResRef" && x.Type == 11);
        return field == null ? null : Encoding.ASCII.GetString(field.Data, 1, field.Data[0]);
    }

    internal static BaseItem ReadBaseItem(Node item) => (BaseItem)ReadInteger(item, "BaseItem");

    internal static int ReadInteger(Node node, string name)
    {
        var field = node.Fields.FirstOrDefault(x => x.Name == name && x.Type <= 5);
        return field == null ? 0 : field.Type switch
        {
            1 => (sbyte)field.Data[0],
            3 => BinaryPrimitives.ReadInt16LittleEndian(field.Data),
            _ => ReadScalar(field),
        };
    }

    internal static void WriteInteger(Node node, string name, int value)
    {
        var field = node.Fields.First(x => x.Name == name);
        field.Data = BitConverter.GetBytes(value);
    }

    /// <summary>GetItemAppearance(SimpleModel, 0).</summary>
    internal static int ReadSimpleModel(Node item) => ReadPart(item, "ModelPart1");

    internal static void WriteSimpleModel(Node item, int model) => WritePart(item, "ModelPart1", model);

    /// <summary>The first local with this name and NWN variable type (1 integer, 3 string), as GetLocal* reads it.</summary>
    internal static Node FindLocal(Node item, string name, int variableType)
    {
        var variables = item.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
        return variables?.Children.FirstOrDefault(variable =>
            ReadVariableName(variable) == name && ReadInteger(variable, "Type") == variableType);
    }

    internal static int ReadLocalInteger(Node item, string name)
    {
        var variable = FindLocal(item, name, 1);
        var value = variable?.Fields.SingleOrDefault(x => x.Name == "Value" && x.Type == 5);
        return value == null ? 0 : ReadScalar(value);
    }

    internal static void WriteLocalInteger(Node item, string name, int value)
    {
        var variable = FindLocal(item, name, 1);
        if (variable != null)
        {
            variable.Fields.Single(x => x.Name == "Value" && x.Type == 5).Data = BitConverter.GetBytes(value);
            return;
        }
        var variables = item.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
        if (variables == null)
        {
            variables = MakeField(15, "VarTable", children: new List<Node>());
            item.Fields.Add(variables);
        }
        variables.Children.Add(new Node
        {
            Fields = new List<Field>
            {
                MakeField(10, "Name", StringData(name)),
                MakeField(4, "Type", BitConverter.GetBytes(1)),
                MakeField(5, "Value", BitConverter.GetBytes(value)),
            }
        });
    }

    internal static string ReadText(Node variable)
    {
        var value = variable.Fields.Single(x => x.Name == "Value" && x.Type == 10);
        return Encoding.UTF8.GetString(value.Data.AsSpan(4));
    }

    internal static void WriteText(Node variable, string text)
        => variable.Fields.Single(x => x.Name == "Value" && x.Type == 10).Data = StringData(text);

    /// <summary>The engine saves item properties with their index as the struct type (locals all use 0).</summary>
    internal static void NumberListElements(Field list)
    {
        for (var index = 0; index < list.Children.Count; index++)
            list.Children[index].Type = (uint)index;
    }
}
