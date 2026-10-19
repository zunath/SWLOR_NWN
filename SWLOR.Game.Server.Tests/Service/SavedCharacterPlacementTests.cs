#nullable enable
using System.Reflection;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.Formats.Gff;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// <c>SavedCharacter.RepairPlacement</c> on hand-built character files: misplaced items move to free cells and the
/// quickbar buttons that name them, or items inside them, follow.
/// </summary>
public class SavedCharacterPlacementTests
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    private static readonly Assembly Game = typeof(Migration).Assembly;
    private static readonly Type Document = Game.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData")!;
    private static readonly Type NodeType = Document.GetNestedType("Node", BindingFlags.NonPublic)!;
    private static readonly Type DataType = Game.GetType("SWLOR.Game.Server.Service.MigrationService.SavedCharacterData")!;
    private static readonly Type CharacterType = Game.GetType("SWLOR.Game.Server.Service.MigrationService.SavedCharacter")!;
    private static readonly string[] DataFields = { "_inventorySizes", "_propertyDefinitions", "_blueprints" };
    private object?[] _savedData = Array.Empty<object?>();

    [SetUp]
    public void LoadSizes()
    {
        _savedData = DataFields.Select(name => DataType.GetField(name, Members)!.GetValue(null)).ToArray();
        var sizes = new Dictionary<int, (int Width, int Height)>
        {
            [(int)BaseItem.Helmet] = (2, 2),
            [(int)BaseItem.LargeBox] = (2, 2),
            [(int)BaseItem.Ring] = (1, 1),
        };
        DataType.GetMethod("Load", Members, new[]
        {
            typeof(IReadOnlyDictionary<int, (int Width, int Height)>),
            typeof(IReadOnlyDictionary<int, (int CostTable, int Param1Table)>),
            typeof(IReadOnlyDictionary<string, string>),
        })!.Invoke(null, new object[] { sizes, new Dictionary<int, (int, int)>(), new Dictionary<string, string>() });
    }

    [TearDown]
    public void RestoreSizes()
    {
        for (var index = 0; index < DataFields.Length; index++)
            DataType.GetField(DataFields[index], Members)!.SetValue(null, _savedData[index]);
    }

    [Test]
    public void A_moved_bag_takes_the_shortcuts_to_its_contents_along()
    {
        // The helmet (listed first) keeps (0,0)-(1,1); the bag at (1,0) overlaps it and must move to (2,0).
        var file = Character(
            new[]
            {
                Item("helmet", BaseItem.Helmet, 0, 0),
                Item("bag", BaseItem.LargeBox, 1, 0, Item("ring", BaseItem.Ring, 3, 2)),
            },
            Button(3, 2, 1, 0),
            Button(1, 0, byte.MaxValue, byte.MaxValue));

        var repaired = Repair(file, out var moved);

        moved.Should().BeTrue();
        var items = Children(repaired, "ItemList").ToList();
        var bag = items.Single(item => Resref(item) == "bag");
        (Int(bag, "Repos_PosX"), Int(bag, "Repos_Posy")).Should().Be((2, 0));
        var helmet = items.Single(item => Resref(item) == "helmet");
        (Int(helmet, "Repos_PosX"), Int(helmet, "Repos_Posy")).Should().Be((0, 0), "an item in place keeps its cells");
        var ring = Children(bag, "ItemList").Single();
        (Int(ring, "Repos_PosX"), Int(ring, "Repos_Posy")).Should().Be((3, 2), "contents keep their cells in the bag");

        var buttons = Children(repaired, "QBList").ToList();
        Shortcut(buttons[0]).Should().Be((3, 2, 2, 0), "the shortcut to the ring names the bag's new cells");
        Shortcut(buttons[1]).Should().Be((2, 0, byte.MaxValue, byte.MaxValue), "the shortcut to the bag follows it");
    }

    [Test]
    public void A_character_with_every_item_in_place_is_unchanged()
    {
        var file = Character(new[] { Item("helmet", BaseItem.Helmet, 0, 0), Item("bag", BaseItem.LargeBox, 2, 0) },
            Button(2, 0, byte.MaxValue, byte.MaxValue));

        var repaired = Repair(file, out var moved);

        moved.Should().BeFalse();
        Shortcut(Children(repaired, "QBList").Single()).Should().Be((2, 0, byte.MaxValue, byte.MaxValue));
    }

    private static GffStruct Repair(byte[] file, out bool moved)
    {
        var character = Activator.CreateInstance(CharacterType, Members, null, new object?[] { file, null }, null)!;
        moved = (bool)CharacterType.GetMethod("RepairPlacement", Members)!.Invoke(character, null)!;
        var saved = (byte[])CharacterType.GetMethod("Serialize", Members)!.Invoke(character, null)!;
        return GffReader.Read(saved).RootStruct;
    }

    private static (int, int, int, int) Shortcut(GffStruct button) =>
        (Int(button, "QBItemReposX"), Int(button, "QBItemReposY"), Int(button, "QBContReposX"), Int(button, "QBContReposY"));

    private static byte[] Character(object[] items, params object[] buttons)
    {
        var root = Node(uint.MaxValue, Text("UUID", "placement-test"), Scalar(0, "IsDM", 0), List("ClassList"),
            List("Equip_ItemList"), List("ItemList", items), List("QBList", buttons));
        var document = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(Document);
        Document.GetField("_header", Members)!.SetValue(document, Encoding.ASCII.GetBytes("BIC V3.2"));
        Document.GetField("_root", Members)!.SetValue(document, root);
        return (byte[])Document.GetMethod("SerializeBytes", Members)!.Invoke(document, null)!;
    }

    private static object Item(string resref, BaseItem baseItem, int x, int y, params object[] contents)
    {
        var bytes = Encoding.ASCII.GetBytes(resref);
        var fields = new List<object>
        {
            Field(11, "TemplateResRef", new[] { (byte)bytes.Length }.Concat(bytes).ToArray()),
            Scalar(5, "BaseItem", (int)baseItem), Scalar(2, "Repos_PosX", x), Scalar(2, "Repos_Posy", y),
        };
        if (contents.Length > 0)
            fields.Add(List("ItemList", contents));
        return Node(0, fields.ToArray());
    }

    /// <summary>A quickbar item button for a carried item, inside a bag at the container cells (255 when carried directly).</summary>
    private static object Button(int x, int y, int containerX, int containerY) =>
        Node(0, Scalar(0, "QBObjectType", 1), Scalar(4, "QBItemInvSlot", -1), Scalar(0, "QBItemReposX", x),
            Scalar(0, "QBItemReposY", y), Scalar(0, "QBContReposX", containerX), Scalar(0, "QBContReposY", containerY));

    private static object Node(uint type, params object[] fields)
    {
        var node = Activator.CreateInstance(NodeType, true)!;
        NodeType.GetField("Type")!.SetValue(node, type);
        var list = (System.Collections.IList)NodeType.GetField("Fields")!.GetValue(node)!;
        foreach (var field in fields) list.Add(field);
        return node;
    }

    private static object Field(uint type, string name, byte[]? data = null, params object[] children)
    {
        object? list = null;
        if (type == 15)
        {
            list = Activator.CreateInstance(typeof(List<>).MakeGenericType(NodeType))!;
            foreach (var child in children) ((System.Collections.IList)list).Add(child);
        }
        return Document.GetMethod("MakeField", Members)!.Invoke(null, new[] { (object)type, name, data, list })!;
    }

    private static object Scalar(uint type, string name, int value) => Field(type, name, BitConverter.GetBytes(value));
    private static object Text(string name, string text) { var bytes = Encoding.UTF8.GetBytes(text); return Field(10, name, BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray()); }
    private static object List(string name, params object[] children) => Field(15, name, null, children);

    private static object Value(GffStruct node, string name) => node.Fields.Single(x => x.Label == name).Value!;
    private static int Int(GffStruct node, string name) => Convert.ToInt32(Value(node, name));
    private static string Resref(GffStruct node) => Value(node, "TemplateResRef").ToString()!;
    private static IEnumerable<GffStruct> Children(GffStruct node, string name) => ((GffList)Value(node, name)).Elements;
}
