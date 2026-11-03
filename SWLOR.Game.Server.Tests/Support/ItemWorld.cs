using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// A small scripted world of items for tests of the item service, played through a <see cref="FakeNWScript"/>: objects
/// that are valid or not, with a tag, a resref, a name and a base item type; creatures with equipped slots; containers
/// with an inventory; item properties walked with the engine's first/next iterators; 2DA rows, string references and
/// the string functions the item code uses. Local variables are the job of <see cref="LocalVariables"/>.
/// </summary>
public sealed class ItemWorld
{
    public const uint ObjectInvalid = 0x7F000000;

    private readonly FakeNWScript _engine;
    private readonly HashSet<uint> _valid = new();
    private readonly Dictionary<uint, string> _tags = new();
    private readonly Dictionary<uint, string> _resrefs = new();
    private readonly Dictionary<uint, string> _names = new();
    private readonly Dictionary<uint, BaseItem> _baseTypes = new();
    private readonly Dictionary<uint, uint> _possessors = new();
    private readonly Dictionary<(uint Creature, InventorySlot Slot), uint> _equipped = new();
    private readonly Dictionary<uint, List<uint>> _inventories = new();
    private readonly Dictionary<uint, int> _inventoryCursor = new();
    private readonly Dictionary<uint, List<ItemPropertyRow>> _properties = new();
    private readonly Dictionary<uint, int> _propertyCursor = new();
    private readonly Dictionary<nint, ItemPropertyRow> _byHandle = new();
    private readonly Dictionary<(string File, string Column, int Row), string> _twoDA = new();
    private readonly Dictionary<int, string> _strings = new();
    private readonly HashSet<(string Resref, ResType Type)> _resources = new();
    private nint _nextHandle = 1;

    public ItemWorld(FakeNWScript engine)
    {
        _engine = engine;
        engine.SetReturnHandler("GetIsObjectValid", args => _valid.Contains((uint)args[0]!));
        engine.SetReturnHandler("GetTag", args => _tags.GetValueOrDefault((uint)args[0]!, string.Empty));
        engine.SetReturnHandler("GetResRef", args => _resrefs.GetValueOrDefault((uint)args[0]!, string.Empty));
        engine.SetReturnHandler("GetName", args => _names.GetValueOrDefault((uint)args[0]!, string.Empty));
        engine.SetReturnHandler("GetBaseItemType", args => _baseTypes.GetValueOrDefault((uint)args[0]!, BaseItem.Invalid));
        engine.SetReturnHandler("GetItemPossessor", args => _possessors.GetValueOrDefault((uint)args[0]!, ObjectInvalid));
        engine.SetReturnHandler("GetItemInSlot", args =>
            _equipped.GetValueOrDefault(((uint)args[1]!, (InventorySlot)args[0]!), ObjectInvalid));
        engine.SetReturnHandler("GetHasInventory", args => _inventories.ContainsKey((uint)args[0]!));
        engine.SetReturnHandler("GetFirstItemInInventory", args =>
        {
            var owner = (uint)args[0]!;
            _inventoryCursor[owner] = 0;
            return InventoryAt(owner);
        });
        engine.SetReturnHandler("GetNextItemInInventory", args =>
        {
            var owner = (uint)args[0]!;
            _inventoryCursor[owner] = _inventoryCursor.GetValueOrDefault(owner) + 1;
            return InventoryAt(owner);
        });

        engine.SetReturnHandler("GetFirstItemProperty", args =>
        {
            var item = (uint)args[0]!;
            _propertyCursor[item] = 0;
            return PropertyAt(item);
        });
        engine.SetReturnHandler("GetNextItemProperty", args =>
        {
            var item = (uint)args[0]!;
            _propertyCursor[item] = _propertyCursor.GetValueOrDefault(item) + 1;
            return PropertyAt(item);
        });
        engine.SetReturnHandler("GetIsItemPropertyValid", args => ((ItemProperty)args[0]!).Handle != 0);
        engine.SetReturnHandler("GetItemHasItemProperty", args =>
            _properties.TryGetValue((uint)args[0]!, out var rows) && rows.Any(row => row.Type == (ItemPropertyType)args[1]!));
        engine.SetReturnHandler("GetItemPropertyType", args => Row(args[0]).Type);
        engine.SetReturnHandler("GetItemPropertySubType", args => Row(args[0]).SubType);
        engine.SetReturnHandler("GetItemPropertyCostTable", args => Row(args[0]).CostTable);
        engine.SetReturnHandler("GetItemPropertyCostTableValue", args => Row(args[0]).CostTableValue);
        engine.SetReturnHandler("GetItemPropertyParam1", args => Row(args[0]).Param1);
        engine.SetReturnHandler("GetItemPropertyParam1Value", args => Row(args[0]).Param1Value);

        engine.SetReturnHandler("Get2DAString", args => _twoDA.GetValueOrDefault(((string)args[0]!, (string)args[1]!, (int)args[2]!), string.Empty));
        engine.SetReturnHandler("GetStringByStrRef", args => _strings.GetValueOrDefault((int)args[0]!, string.Empty));
        engine.SetReturnHandler("ResManGetAliasFor", args =>
            _resources.Contains(((string)args[0]!, (ResType)args[1]!)) ? "alias" : string.Empty);
        engine.SetReturnHandler("StringToInt", args => int.TryParse((string)args[0]!, out var number) ? number : 0);
        engine.SetReturnHandler("GetStringLength", args => ((string)args[0]!).Length);
        engine.SetReturnHandler("GetSubString", args => SubString((string)args[0]!, (int)args[1]!, (int)args[2]!));
        engine.SetReturnHandler("GetStringLeft", args =>
        {
            var text = (string)args[0]!;
            var count = (int)args[1]!;
            return count <= 0 ? string.Empty : text.Substring(0, Math.Min(count, text.Length));
        });
    }

    /// <summary>Makes an object valid and describes it.</summary>
    public uint Add(uint obj, string tag = "", BaseItem baseItem = BaseItem.Invalid, string resref = "", string name = "")
    {
        _valid.Add(obj);
        _tags[obj] = tag;
        _baseTypes[obj] = baseItem;
        _resrefs[obj] = resref;
        _names[obj] = name;
        return obj;
    }

    /// <summary>Makes an object invalid again (destroyed, say).</summary>
    public void Invalidate(uint obj) => _valid.Remove(obj);

    /// <summary>Puts the items into the container's inventory, in order; the container then has an inventory.</summary>
    public void Carry(uint container, params uint[] items)
    {
        if (!_inventories.TryGetValue(container, out var list))
            _inventories[container] = list = new List<uint>();
        foreach (var item in items)
        {
            list.Add(item);
            _possessors[item] = container;
        }
    }

    /// <summary>Gives an object an inventory without items (an empty bag).</summary>
    public void MakeContainer(uint container)
    {
        if (!_inventories.ContainsKey(container))
            _inventories[container] = new List<uint>();
    }

    public void SetPossessor(uint item, uint possessor) => _possessors[item] = possessor;

    public void Equip(uint creature, InventorySlot slot, uint item)
    {
        _equipped[(creature, slot)] = item;
        _possessors[item] = creature;
    }

    /// <summary>Adds an item property to an item, in the order the engine walks them.</summary>
    public void AddProperty(uint item, ItemPropertyType type, int subType = -1, int costTableValue = 0, int costTable = -1,
        int param1 = -1, int param1Value = 0)
    {
        if (!_properties.TryGetValue(item, out var rows))
            _properties[item] = rows = new List<ItemPropertyRow>();
        var row = new ItemPropertyRow(_nextHandle++, type, subType, costTableValue, costTable, param1, param1Value);
        rows.Add(row);
        _byHandle[row.Handle] = row;
    }

    /// <summary>Makes <c>Get2DAString</c> answer for one cell.</summary>
    public void TwoDA(string file, string column, int row, string value) => _twoDA[(file, column, row)] = value;

    /// <summary>Makes <c>GetStringByStrRef</c> answer for one string reference.</summary>
    public void StrRef(int reference, string text) => _strings[reference] = text;

    /// <summary>Makes a texture exist for <c>ResManGetAliasFor</c>.</summary>
    public void Texture(string resref, ResType type = ResType.TGA) => _resources.Add((resref, type));

    /// <summary>The engine structure of an item property that is not valid (the end of the walk).</summary>
    public ItemProperty Property(uint item, int index) => PropertyOf(item, index);

    private uint InventoryAt(uint owner)
    {
        var list = _inventories.GetValueOrDefault(owner);
        var cursor = _inventoryCursor.GetValueOrDefault(owner);
        return list != null && cursor < list.Count ? list[cursor] : ObjectInvalid;
    }

    private ItemProperty PropertyAt(uint item) => PropertyOf(item, _propertyCursor.GetValueOrDefault(item));

    private ItemProperty PropertyOf(uint item, int index)
    {
        var rows = _properties.GetValueOrDefault(item);
        return EngineStructures.Create<ItemProperty>(rows != null && index < rows.Count ? rows[index].Handle : 0);
    }

    private ItemPropertyRow Row(object? property) => _byHandle[((ItemProperty)property!).Handle];

    private static string SubString(string text, int start, int count)
    {
        if (start < 0 || start >= text.Length || count <= 0)
            return string.Empty;
        return text.Substring(start, Math.Min(count, text.Length - start));
    }

    private sealed record ItemPropertyRow(nint Handle, ItemPropertyType Type, int SubType, int CostTableValue, int CostTable,
        int Param1, int Param1Value);
}
