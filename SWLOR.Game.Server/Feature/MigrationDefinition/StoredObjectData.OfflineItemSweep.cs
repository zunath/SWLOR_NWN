using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// The stored-item sweep applied to saved GFF with no game engine, for offline runs. Each rule
/// mirrors its native counterpart on an unequipped item: <see cref="HeadgearSlotMigration.ConvertItem"/>,
/// <see cref="EquipmentTypeMigration.ConvertItem"/>, <see cref="ItemIconMigration.MigrateItem"/> and
/// <see cref="LootStatRevisionMigration"/>'s stat revision, in the same order. Fields the sweep does not
/// change keep their saved bytes. New properties and locals are written the way the engine saves them.
/// </summary>
internal sealed partial class StoredObjectData
{
    private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";

    /// <summary>itempropdef.2da CostTableResRef for every property type the stat revision catalogs change.</summary>
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
    };

    /// <summary>Offline counterpart of <see cref="LootStatRevisionMigration.MigrateStoredObjectData"/>.</summary>
    internal static bool MigrateStoredItemOffline(string original, out string migrated, bool repairHeadgear, Action<ConvertedEquipment> converted)
    {
        migrated = original;
        if (string.IsNullOrWhiteSpace(original))
            return false;
        // Saved creatures keep authored stats and slots; only their items' artwork changes.
        if (IsCreatureData(original))
            return MigrateArtworkOffline(original, out migrated);

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
             EquipmentTypeMigration.IsLegacyEquipment(resref, baseItem));
        if (!MigrateItemOffline(root, repairHeadgear, new HashSet<Node>()))
            return false;
        if (refreshRootMetadata)
            converted?.Invoke(GetConvertedEquipment(root));
        migrated = document.Serialize();
        return true;
    }

    /// <summary>Offline counterpart of <see cref="ItemIconMigration.MigrateSerializedObject"/>: artwork only.</summary>
    internal static bool MigrateArtworkOffline(string original, out string migrated)
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
                if (ItemIconMigration.MigrateDroidArtwork(saved, MigrateArtworkOffline, out var updated))
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

    private static bool MigrateItemOffline(Node item, bool repairHeadgear, HashSet<Node> visited)
    {
        if (!visited.Add(item)) return false;
        var changed = false;
        if (repairHeadgear)
        {
            changed |= ConvertHeadgear(item);
            changed |= ConvertEquipmentType(item);
        }
        changed |= UpdateArtwork(item);
        changed |= ReviseStats(item);

        var droid = FindLocal(item, ConstructedDroidVariable, 3);
        if (droid != null)
        {
            var original = ReadText(droid);
            var migrated = HeadgearSlotMigration.MigrateDroid(original, data =>
                MigrateStoredItemOffline(data, out var result, repairHeadgear, null) ? result : data, repairHeadgear);
            if (original != migrated)
            {
                WriteText(droid, migrated);
                changed = true;
            }
        }

        foreach (var list in item.Fields.Where(IsInventoryList))
        foreach (var child in list.Children)
            changed |= MigrateItemOffline(child, repairHeadgear, visited);
        return changed;
    }

    private static bool ConvertHeadgear(Node item)
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

    private static bool ConvertEquipmentType(Node item)
    {
        var resref = ReadResRef(item);
        if (!EquipmentTypeMigration.IsLegacyEquipment(resref, ReadBaseItem(item))) return false;
        var correction = EquipmentTypeMigration.Corrections[resref];
        WriteInteger(item, "BaseItem", (int)correction.Type);
        WriteSimpleModel(item, correction.Model);
        return true;
    }

    private static bool UpdateArtwork(Node item)
    {
        var current = ReadSimpleModel(item);
        var updated = ItemIconMigration.GetUpdatedModel(ReadResRef(item), (int)ReadBaseItem(item), current);
        if (updated == current) return false;
        WriteSimpleModel(item, updated);
        return true;
    }

    /// <summary>Mirrors <c>LootStatRevisionMigration.MigrateItem</c>.</summary>
    private static bool ReviseStats(Node item)
    {
        var alreadyRevisedRare = ReadLocalInteger(item, RareLootStatRevisionCatalog.RevisionVariable) >= RareLootStatRevisionCatalog.CurrentRevision;
        var changed = ApplyRevision(item, LootStatRevisionCatalog.Changes, LootStatRevisionMigration.RevisionVariable, LootStatRevisionMigration.CurrentRevision);
        changed |= ApplyRevision(item, RareLootStatRevisionCatalog.Changes,
            RareLootStatRevisionCatalog.RevisionVariable, RareLootStatRevisionCatalog.CurrentRevision);
        if (alreadyRevisedRare)
            changed |= ApplyRevision(item, EquipmentTypeStatRevisionCatalog.Changes,
                EquipmentTypeStatRevisionCatalog.RevisionVariable, EquipmentTypeStatRevisionCatalog.CurrentRevision);
        else if (EquipmentTypeStatRevisionCatalog.Changes.ContainsKey(ReadResRef(item)) &&
                 ReadLocalInteger(item, EquipmentTypeStatRevisionCatalog.RevisionVariable) < EquipmentTypeStatRevisionCatalog.CurrentRevision)
        {
            // The revised rare catalog already applied the final cloak value.
            WriteLocalInteger(item, EquipmentTypeStatRevisionCatalog.RevisionVariable, EquipmentTypeStatRevisionCatalog.CurrentRevision);
            changed = true;
        }
        changed |= ApplyRevision(item, FoundryLootStatRevisionCatalog.Changes,
            FoundryLootStatRevisionCatalog.RevisionVariable, FoundryLootStatRevisionCatalog.CurrentRevision);
        return changed;
    }

    private static bool ApplyRevision(Node item, IReadOnlyDictionary<string, LootStatChange[]> catalog, string revisionVariable, int revision)
    {
        if (!catalog.TryGetValue(ReadResRef(item), out var changes) || ReadLocalInteger(item, revisionVariable) >= revision)
            return false;
        foreach (var change in changes)
            ApplyChange(item, change);
        WriteLocalInteger(item, revisionVariable, revision);
        return true;
    }

    /// <summary>Shifts one property by the blueprint difference, as <c>LootStatRevisionMigration.ApplyChange</c> does.</summary>
    private static void ApplyChange(Node item, LootStatChange change)
    {
        var properties = item.Fields.SingleOrDefault(x => x.Name == "PropertiesList" && x.Type == 15);
        var matches = properties?.Children.Where(property =>
            ReadInteger(property, "PropertyName") == (int)change.Type &&
            (change.Type != ItemPropertyType.Defense || ReadInteger(property, "Subtype") == change.Subtype)).ToList() ?? new List<Node>();
        var total = matches.Sum(property => ReadInteger(property, "CostValue"));
        var revised = Math.Max(0, total - change.Previous + change.Current);
        if (revised == total && matches.Count <= 1)
            return;

        if (properties == null)
        {
            properties = MakeField(15, "PropertiesList", children: new List<Node>());
            item.Fields.Add(properties);
        }
        properties.Children.RemoveAll(matches.Contains);
        if (revised > 0)
            properties.Children.Add(NewProperty(change, revised));
        NumberListElements(properties);
    }

    /// <summary>The property ItemPropertyCustom(type, subtype or -1, value, -1) saves as.</summary>
    private static Node NewProperty(LootStatChange change, int value)
    {
        if (!RevisedPropertyCostTables.TryGetValue(change.Type, out var costTable))
            throw new InvalidOperationException($"No cost table is known for revised property type {change.Type}.");
        var subtype = change.Type == ItemPropertyType.Defense ? change.Subtype : ushort.MaxValue;
        return new Node
        {
            Fields = new List<Field>
            {
                MakeField(2, "PropertyName", BitConverter.GetBytes((int)change.Type)),
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

    private static ConvertedEquipment GetConvertedEquipment(Node root)
    {
        var baseItem = ReadBaseItem(root);
        var model = ReadSimpleModel(root);
        // Item.GetIconResref for every type a conversion produces: helmets and cloaks
        // use one icon; gloves are simple models with an iit_glove_<model> icon.
        var icon = baseItem switch
        {
            BaseItem.Helmet => "ihelm",
            BaseItem.Cloak => "iit_cloak",
            BaseItem.Gloves when model is 126 or 176 => $"iit_glove_{model:000}",
            _ => throw new InvalidOperationException($"No offline icon is known for converted base item {baseItem} with model {model}.")
        };
        var category = PlayerMarket.GetBaseItemMarketCategory(baseItem)
            ?? throw new InvalidOperationException($"Converted base item {baseItem} has no base-item market category.");
        return new ConvertedEquipment(icon, category);
    }

    private static string ReadResRef(Node item)
    {
        var field = item.Fields.SingleOrDefault(x => x.Name == "TemplateResRef" && x.Type == 11);
        return field == null ? null : Encoding.ASCII.GetString(field.Data, 1, field.Data[0]);
    }

    private static BaseItem ReadBaseItem(Node item) => (BaseItem)ReadInteger(item, "BaseItem");

    private static int ReadInteger(Node node, string name)
    {
        var field = node.Fields.FirstOrDefault(x => x.Name == name && x.Type <= 5);
        return field == null ? 0 : field.Type switch
        {
            1 => (sbyte)field.Data[0],
            3 => BinaryPrimitives.ReadInt16LittleEndian(field.Data),
            _ => ReadScalar(field),
        };
    }

    private static void WriteInteger(Node node, string name, int value)
    {
        var field = node.Fields.First(x => x.Name == name);
        field.Data = BitConverter.GetBytes(value);
    }

    /// <summary>GetItemAppearance(SimpleModel, 0): the extended model part wins over the byte-sized one.</summary>
    private static int ReadSimpleModel(Node item)
    {
        var extended = item.Fields.SingleOrDefault(x => x.Name == "xModelPart1");
        if (extended != null) return ReadScalar(extended);
        var model = item.Fields.SingleOrDefault(x => x.Name == "ModelPart1");
        return model == null ? 0 : ReadScalar(model);
    }

    /// <summary>Saves both model part fields, as the engine does.</summary>
    private static void WriteSimpleModel(Node item, int model)
    {
        void Set(uint type, string name, int value)
        {
            var field = MakeField(type, name, BitConverter.GetBytes(value));
            var index = item.Fields.FindIndex(x => x.Name == name);
            if (index >= 0) item.Fields[index] = field;
            else item.Fields.Add(field);
        }
        Set(0, "ModelPart1", model & byte.MaxValue);
        Set(2, "xModelPart1", model);
    }

    /// <summary>The first local with this name and NWN variable type (1 integer, 3 string), as GetLocal* reads it.</summary>
    private static Node FindLocal(Node item, string name, int variableType)
    {
        var variables = item.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
        return variables?.Children.FirstOrDefault(variable =>
            ReadVariableName(variable) == name && ReadInteger(variable, "Type") == variableType);
    }

    private static int ReadLocalInteger(Node item, string name)
    {
        var variable = FindLocal(item, name, 1);
        var value = variable?.Fields.SingleOrDefault(x => x.Name == "Value" && x.Type == 5);
        return value == null ? 0 : ReadScalar(value);
    }

    private static void WriteLocalInteger(Node item, string name, int value)
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

    private static string ReadText(Node variable)
    {
        var value = variable.Fields.Single(x => x.Name == "Value" && x.Type == 10);
        return Encoding.UTF8.GetString(value.Data.AsSpan(4));
    }

    private static void WriteText(Node variable, string text)
        => variable.Fields.Single(x => x.Name == "Value" && x.Type == 10).Data = StringData(text);

    /// <summary>The engine saves item properties with their index as the struct type (locals all use 0).</summary>
    private static void NumberListElements(Field list)
    {
        for (var index = 0; index < list.Children.Count; index++)
            list.Children[index].Type = (uint)index;
    }
}
