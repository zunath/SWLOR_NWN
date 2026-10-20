using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.Bioware;
using Node = SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData.Node;
using Field = SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData.Field;

namespace SWLOR.Game.Server.Service.MigrationService;

/// <summary>
/// An item saved inside a character file: equipped, carried, or inside a carried container.
/// Edits change the saved GFF directly and are written the way the engine saves them.
/// </summary>
internal sealed class SavedItem
{
    internal SavedItem(Node node, Field list, SavedItem container)
    {
        Node = node;
        List = list;
        Container = container;
    }

    internal Node Node { get; }
    internal Field List { get; set; }

    /// <summary>The container item holding this item, or null when the character holds it.</summary>
    public SavedItem Container { get; }

    public bool IsEquipped => Container == null && List.Name == "Equip_ItemList";

    /// <summary>The equipment slot, for equipped items; Equip_ItemList entries use the slot's bit as their struct type.</summary>
    public InventorySlot Slot => IsEquipped ? (InventorySlot)BitOperations.Log2(Node.Type) : InventorySlot.Invalid;

    public string ResRef => StoredObjectData.ReadResRef(Node) ?? string.Empty;

    public string Tag
    {
        get => ReadString("Tag");
        set => WriteString("Tag", value);
    }

    public BaseItem BaseItem
    {
        get => StoredObjectData.ReadBaseItem(Node);
        set => StoredObjectData.WriteInteger(Node, "BaseItem", (int)value);
    }

    public int StackSize
    {
        get => StoredObjectData.ReadInteger(Node, "StackSize");
        set => SavedGff.WriteScalar(Node, "StackSize", 2, value);
    }

    public bool HasInventory => Node.Fields.Any(x => x.Name == "ItemList" && x.Type == 15);

    /// <summary>Items inside this container, in saved order.</summary>
    public IEnumerable<SavedItem> Contents =>
        Node.Fields.Where(x => x.Name == "ItemList" && x.Type == 15)
            .SelectMany(list => list.Children.ToArray().Select(child => new SavedItem(child, list, this)));

    public int SimpleModel
    {
        get => StoredObjectData.ReadSimpleModel(Node);
        set => StoredObjectData.WriteSimpleModel(Node, value);
    }

    public IReadOnlyList<SavedItemProperty> Properties =>
        PropertyList?.Children.Select(child => new SavedItemProperty(child)).ToList() ?? new List<SavedItemProperty>();

    private Field PropertyList => Node.Fields.SingleOrDefault(x => x.Name == "PropertiesList" && x.Type == 15);

    public void RemoveProperty(SavedItemProperty property)
    {
        var list = PropertyList;
        if (list == null || !list.Children.Remove(property.Node))
            throw new InvalidOperationException("The saved item property is not on this item.");
        StoredObjectData.NumberListElements(list);
    }

    /// <summary>Removes every saved property that matches; returns how many were removed.</summary>
    public int RemoveProperties(Func<SavedItemProperty, bool> match)
    {
        var removed = 0;
        foreach (var property in Properties.Where(match).ToArray())
        {
            RemoveProperty(property);
            removed++;
        }
        return removed;
    }

    /// <summary>
    /// <see cref="MigrationObject.AddProperty"/> on saved data: with <see cref="AddItemPropertyPolicy.ReplaceExisting"/>
    /// it first removes saved properties of the same type and subtype, and the new property is appended last, as
    /// the engine adds a permanent property. Saved properties are all permanent; temporary ones live in EffectList.
    /// </summary>
    public SavedItemProperty AddProperty(ItemPropertyType type, int subtype, int costValue, AddItemPropertyPolicy policy, int param1Value = -1)
    {
        var savedSubtype = subtype < 0 ? ushort.MaxValue : subtype;
        if (policy == AddItemPropertyPolicy.ReplaceExisting)
            RemoveProperties(property => property.Type == type && property.Subtype == savedSubtype);
        else if (policy == AddItemPropertyPolicy.KeepExisting && Properties.Any(property => property.Type == type && property.Subtype == savedSubtype))
            return null;
        return AddProperty(type, subtype, costValue, param1Value);
    }

    /// <summary>
    /// Adds the property ItemPropertyCustom(type, subtype, costValue, param1Value) saves as. A -1 subtype or
    /// parameter saves as 65535 or 255, and the cost table and parameter table come from itempropdef.2da.
    /// </summary>
    public SavedItemProperty AddProperty(ItemPropertyType type, int subtype, int costValue, int param1Value = -1)
    {
        var definition = SavedCharacterData.GetPropertyDefinition(type);
        var list = PropertyList;
        if (list == null)
        {
            list = StoredObjectData.MakeField(15, "PropertiesList", children: new List<Node>());
            Node.Fields.Add(list);
        }
        var node = new Node
        {
            Fields = new List<Field>
            {
                SavedGff.Scalar(2, "PropertyName", (int)type),
                SavedGff.Scalar(2, "Subtype", subtype < 0 ? ushort.MaxValue : subtype),
                SavedGff.Scalar(0, "CostTable", definition.CostTable),
                SavedGff.Scalar(2, "CostValue", costValue),
                SavedGff.Scalar(0, "Param1", definition.Param1Table),
                SavedGff.Scalar(0, "Param1Value", param1Value < 0 ? byte.MaxValue : param1Value),
                SavedGff.Scalar(0, "ChanceAppear", 100),
                SavedGff.Scalar(0, "UsesPerDay", byte.MaxValue),
                SavedGff.Scalar(0, "Useable", 1),
                StoredObjectData.MakeField(10, "CustomTag", StoredObjectData.StringData(string.Empty)),
            }
        };
        list.Children.Add(node);
        StoredObjectData.NumberListElements(list);
        return new SavedItemProperty(node);
    }

    public int GetLocalInt(string name) => StoredObjectData.ReadLocalInteger(Node, name);

    public void SetLocalInt(string name, int value) => StoredObjectData.WriteLocalInteger(Node, name, value);

    public string GetLocalString(string name)
    {
        var variable = StoredObjectData.FindLocal(Node, name, 3);
        return variable == null ? string.Empty : StoredObjectData.ReadText(variable);
    }

    public void SetLocalString(string name, string value)
    {
        var variable = StoredObjectData.FindLocal(Node, name, 3);
        if (variable != null)
        {
            StoredObjectData.WriteText(variable, value);
            return;
        }
        Locals(true).Children.Add(new Node
        {
            Fields = new List<Field>
            {
                StoredObjectData.MakeField(10, "Name", StoredObjectData.StringData(name)),
                SavedGff.Scalar(4, "Type", 3),
                StoredObjectData.MakeField(10, "Value", StoredObjectData.StringData(value)),
            }
        });
    }

    /// <summary>Deletes every local of this name and NWN variable type (1 integer, 2 float, 3 string, 4 object, 5 location).</summary>
    public void DeleteLocal(string name, int variableType)
    {
        var locals = Locals(false);
        locals?.Children.RemoveAll(variable => StoredObjectData.ReadVariableName(variable) == name &&
            StoredObjectData.ReadInteger(variable, "Type") == variableType);
    }

    public void DeleteAllLocals() => Locals(false)?.Children.Clear();

    internal Field Locals(bool create)
    {
        var locals = Node.Fields.SingleOrDefault(x => x.Name == "VarTable" && x.Type == 15);
        if (locals == null && create)
        {
            locals = StoredObjectData.MakeField(15, "VarTable", children: new List<Node>());
            Node.Fields.Add(locals);
        }
        return locals;
    }

    /// <summary>
    /// Saved-data GetName: the name SetName gave the item (its DisplayName), otherwise its saved localized name;
    /// empty when that is only a talk table reference.
    /// </summary>
    public string Name
    {
        get
        {
            var displayName = ReadString("DisplayName");
            return string.IsNullOrEmpty(displayName) ? StoredObjectData.ReadLocalizedName(Node) : displayName;
        }
    }

    /// <summary>
    /// Saved-data SetName: the engine saves the new name as the item's DisplayName and keeps its localized name;
    /// an empty name removes the DisplayName, restoring the localized one.
    /// </summary>
    public void SetName(string name)
    {
        if (string.IsNullOrEmpty(name))
            Node.Fields.RemoveAll(x => x.Name == "DisplayName");
        else
            WriteString("DisplayName", name);
    }

    public string ReadString(string name)
    {
        var field = Node.Fields.SingleOrDefault(x => x.Name == name && x.Type == 10);
        return field == null ? string.Empty : Encoding.UTF8.GetString(field.Data.AsSpan(4));
    }

    public void WriteString(string name, string value) => SavedGff.WriteString(Node, name, value);

    /// <summary>Replaces a localized string with an empty one, as SetName or SetDescription with "" saves.</summary>
    public void ClearLocalizedString(string name) => SavedGff.ClearLocalizedString(Node, name);

    public override string ToString() => $"{ResRef} ({BaseItem})";
}

/// <summary>
/// An item saved on its own (UTI, base64), as droid parts, droid inventories and item storage keep them. Rules
/// edit <see cref="Root"/> and its contents; <see cref="Serialize"/> returns the edited item in the same form.
/// </summary>
internal sealed class SavedItemDocument
{
    private readonly StoredObjectData _document;

    public SavedItemDocument(string data)
    {
        _document = new StoredObjectData(Convert.FromBase64String(data));
        Root = new SavedItem(_document.Root, null, null);
    }

    public SavedItem Root { get; }

    /// <summary>The root and every item in its containers, outer items first.</summary>
    public IEnumerable<SavedItem> AllItems => new[] { Root }.Concat(Expand(Root));

    private static IEnumerable<SavedItem> Expand(SavedItem item) =>
        item.Contents.SelectMany(content => new[] { content }.Concat(Expand(content)));

    public string Serialize() => Convert.ToBase64String(_document.SerializeBytes());

    /// <summary>
    /// Applies <paramref name="migrate"/> to a saved item and returns whether it changed anything; on a change
    /// <paramref name="migrated"/> holds the re-serialized item, otherwise the original data. Creature data and
    /// empty values are left alone.
    /// </summary>
    public static bool Migrate(string data, Func<SavedItemDocument, bool> migrate, out string migrated)
    {
        migrated = data;
        if (string.IsNullOrWhiteSpace(data) || StoredObjectData.IsCreatureData(data))
            return false;
        var document = new SavedItemDocument(data);
        if (!migrate(document))
            return false;
        migrated = document.Serialize();
        return true;
    }
}

/// <summary>A permanent item property saved in an item's PropertiesList.</summary>
internal sealed class SavedItemProperty
{
    internal SavedItemProperty(Node node) => Node = node;

    internal Node Node { get; }

    public ItemPropertyType Type => (ItemPropertyType)StoredObjectData.ReadInteger(Node, "PropertyName");

    /// <summary>The saved subtype; a property without one saves 65535.</summary>
    public int Subtype => StoredObjectData.ReadInteger(Node, "Subtype");

    public int CostTable => StoredObjectData.ReadInteger(Node, "CostTable");

    public int CostValue
    {
        get => StoredObjectData.ReadInteger(Node, "CostValue");
        set => SavedGff.WriteScalar(Node, "CostValue", 2, value);
    }

    public int Param1Value => StoredObjectData.ReadInteger(Node, "Param1Value");

    public override string ToString() => $"{Type}/{Subtype}={CostValue}";
}

/// <summary>Scalar and string field helpers that write values the way the engine saves them.</summary>
internal static class SavedGff
{
    /// <summary>A scalar field of GFF type 0-5, with unused payload bytes left at zero.</summary>
    public static Field Scalar(uint type, string name, int value) =>
        StoredObjectData.MakeField(type, name, Payload(type, value));

    public static byte[] Payload(uint type, int value)
    {
        var data = new byte[4];
        switch (type)
        {
            case 0:
            case 1:
                data[0] = unchecked((byte)value);
                break;
            case 2:
            case 3:
                BinaryPrimitives.WriteUInt16LittleEndian(data, unchecked((ushort)value));
                break;
            default:
                BinaryPrimitives.WriteInt32LittleEndian(data, value);
                break;
        }
        return data;
    }

    /// <summary>Writes a scalar, keeping an existing field's type or adding one of <paramref name="type"/>.</summary>
    public static void WriteScalar(Node node, string name, uint type, int value)
    {
        var field = node.Fields.FirstOrDefault(x => x.Name == name && x.Type <= 5);
        if (field == null)
            node.Fields.Add(Scalar(type, name, value));
        else
            field.Data = Payload(field.Type, value);
    }

    public static float ReadFloat(Node node, string name)
    {
        var field = node.Fields.FirstOrDefault(x => x.Name == name && x.Type == 8);
        return field == null || field.Data.Length < 4 ? 0f : BitConverter.ToSingle(field.Data, 0);
    }

    public static void WriteFloat(Node node, string name, float value)
    {
        var field = node.Fields.FirstOrDefault(x => x.Name == name && x.Type == 8);
        if (field == null)
            node.Fields.Add(StoredObjectData.MakeField(8, name, BitConverter.GetBytes(value)));
        else
            field.Data = BitConverter.GetBytes(value);
    }

    public static void WriteString(Node node, string name, string value)
    {
        var field = node.Fields.SingleOrDefault(x => x.Name == name && x.Type == 10);
        if (field == null)
            node.Fields.Add(StoredObjectData.MakeField(10, name, StoredObjectData.StringData(value)));
        else
            field.Data = StoredObjectData.StringData(value);
    }

    /// <summary>An empty CExoLocString: no string reference (0xFFFFFFFF) and no substrings.</summary>
    public static void ClearLocalizedString(Node node, string name)
    {
        var data = new byte[12];
        BinaryPrimitives.WriteUInt32LittleEndian(data, 8);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4), uint.MaxValue);
        var field = node.Fields.SingleOrDefault(x => x.Name == name && x.Type == 12);
        if (field == null)
            node.Fields.Add(StoredObjectData.MakeField(12, name, data));
        else
            field.Data = data;
    }

    public static Field List(Node node, string name, bool create)
    {
        var list = node.Fields.SingleOrDefault(x => x.Name == name && x.Type == 15);
        if (list == null && create)
        {
            list = StoredObjectData.MakeField(15, name, children: new List<Node>());
            node.Fields.Add(list);
        }
        return list;
    }
}
