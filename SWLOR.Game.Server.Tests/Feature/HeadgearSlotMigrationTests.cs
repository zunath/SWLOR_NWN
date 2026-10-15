using System.Reflection;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Feature.RecipeDefinition.SmitheryRecipeDefinition;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.Formats.Gff;

namespace SWLOR.Game.Server.Tests.Feature;

public class HeadgearSlotMigrationTests
{
    [Test]
    public void NativeFixturesReallyContainHistoricalArmorAndProperties()
    {
        var fixtures = typeof(PlayerAbilityFixture).Assembly.GetType("SWLOR.Game.Server.EngineTests.Framework.LegacyHeadgearFixtures")!;
        foreach (var name in new[] { "Ruptor", "Thermal" })
        {
            var saved = Read((string)fixtures.GetField(name)!.GetRawConstantValue()!);
            Value(saved, "BaseItem").Should().Be((int)BaseItem.Armor);
            saved.Fields.Should().Contain(x => x.Label == "ArmorPart_Torso");
            saved.Fields.Should().NotContain(x => x.Label == "ModelPart1");
            Children(saved, "PropertiesList").Should().NotBeEmpty();
            HeadgearSlotMigration.IsLegacyHeadgear((string)Value(saved, "TemplateResRef"), BaseItem.Armor).Should().BeTrue();
        }
    }
    [Test]
    public void CatalogMatchesEveryCorrectedHeadgearRecipeAndBlueprint()
    {
        var recipes = new SalvagedFieldGearRecipes().BuildRecipes().Values
            .Where(x => x.Category == RecipeCategoryType.Helmet).Select(x => x.Resref).ToArray();
        HeadgearSlotMigration.LegacyHeadgear.Keys.Should().BeEquivalentTo(recipes);
        HeadgearSlotMigration.LegacyHeadgear.Should().HaveCount(13);
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module"))) directory = directory.Parent;
        foreach (var (resref, model) in HeadgearSlotMigration.LegacyHeadgear)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory!.FullName, "Module", "uti", resref + ".uti.json")));
            blueprint.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be((int)BaseItem.Helmet);
            blueprint.RootElement.GetProperty("ModelPart1").GetProperty("value").GetInt32().Should().Be(model);
            HeadgearSlotMigration.IsLegacyHeadgear(resref, BaseItem.Armor).Should().BeTrue();
            HeadgearSlotMigration.IsLegacyHeadgear(resref, BaseItem.Helmet).Should().BeFalse();
            HeadgearSlotMigration.IsLegacyHeadgear(resref, BaseItem.Gloves).Should().BeFalse();
        }
        HeadgearSlotMigration.IsLegacyHeadgear("piston_gaunt", BaseItem.Armor).Should().BeFalse();
        HeadgearSlotMigration.IsLegacyHeadgear("ordinary_armor", BaseItem.Armor).Should().BeFalse();
        new _23_RepairHeadgearSlots().Version.Should().Be(23);
        new _31_RepairHeadgearSlots().Version.Should().Be(31);
    }

    [TestCase(0)]
    [TestCase(33)]
    public void ArchivedLegacyCopyKeepsEveryUnrelatedFieldAndIsByteStableOnRetry(int existingModel)
    {
        var root = Visor();
        AddField(root, Scalar(0, "ModelPart1", existingModel));
        var original = Encode(root);
        HeadgearSlotMigration.MigrateSerializedObject(original, out var migrated).Should().BeTrue();
        var before = Read(original);
        var after = Read(migrated);
        Value(after, "BaseItem").Should().Be((int)BaseItem.Helmet);
        Value(after, "ModelPart1").Should().Be((byte)(existingModel > 0 ? existingModel : 223));
        after.Fields.Where(x => x.Label is not "BaseItem" and not "ModelPart1")
            .Should().BeEquivalentTo(before.Fields.Where(x => x.Label is not "BaseItem" and not "ModelPart1"));
        HeadgearSlotMigration.MigrateSerializedObject(migrated, out var retry).Should().BeFalse();
        retry.Should().Be(migrated);
    }

    [Test]
    public void ArchivesPreserveNestedInventoryIdentitiesAndUnrelatedState()
    {
        var visor = Visor();
        var bag = Node(0, Scalar(5, "BaseItem", (int)BaseItem.MiscSmall), List("ItemList", Visor()));
        var original = Encode(Node(0, Scalar(2, "Appearance_Type", 65535), Text("UnknownState", "keep"),
            List("ItemList", bag, visor)));
        HeadgearSlotMigration.MigrateSerializedObject(original, out var migrated).Should().BeTrue();
        var after = Read(migrated);
        Children(after, "ItemList").Should().HaveCount(2);
        var stowed = Children(after, "ItemList").Single(x => x.Fields.Any(f => f.Label == "TemplateResRef"));
        Value(stowed, "BaseItem").Should().Be((int)BaseItem.Helmet);
        Value(stowed, "UUID").Should().Be("visor-uuid");
        var nested = Children(Children(after, "ItemList").Single(x => x.Fields.Any(f => f.Label == "ItemList")), "ItemList").Single();
        Value(nested, "BaseItem").Should().Be((int)BaseItem.Helmet);
        Value(after, "Appearance_Type").Should().Be((ushort)65535);
        Value(after, "UnknownState").Should().Be("keep");
        HeadgearSlotMigration.MigrateSerializedObject(migrated, out _).Should().BeFalse();
    }

    [TestCase("BIC ")]
    [TestCase("UTC ")]
    public void SavedCreaturesWaitForLivePlayerMigrationSoDatabaseStatsCannotBecomeStale(string fileType)
    {
        var original = Encode(Node(0, List("Equip_ItemList", Visor(1u << (int)InventorySlot.Chest))), fileType);
        HeadgearSlotMigration.MigrateSerializedObject(original, out var migrated).Should().BeFalse();
        migrated.Should().Be(original, "PC gear needs its loaded DB record and native unequip; authored NPC gear is outside player migration scope");
    }

    [TestCase("Chest", false)]
    [TestCase("Chest", true)]
    [TestCase("1", false)]
    [TestCase("1", true)]
    public void DroidArchiveStowsOnlyLegacyChestAndRepairsMissingOrCollidingItemIds(string chestKey, bool missingId)
    {
        var visor = Encode(Visor(id: missingId ? null : "old-id"));
        var helmet = Encode(Node(0, Scalar(5, "BaseItem", (int)BaseItem.Helmet), Text("UUID", "existing-helmet")));
        var original = new JObject
        {
            ["UnknownState"] = 42, ["EquippedItems"] = new JObject { [chestKey] = visor, ["Head"] = helmet },
            ["Inventory"] = new JObject { ["old-id"] = visor }, ["SerializedCPU"] = visor,
        };
        var controller = Encode(Node(0, List("VarTable", Node(0, Text("Name", "CONSTRUCTED_DROID"),
            Scalar(4, "Type", 3), Text("Value", original.ToString())))));
        HeadgearSlotMigration.MigrateSerializedObject(controller, out var migrated).Should().BeTrue();
        var droid = JObject.Parse((string)Value(Children(Read(migrated), "VarTable").Single(), "Value"));
        ((JObject)droid["EquippedItems"]!).Properties().Should().ContainSingle();
        droid["EquippedItems"]!["Head"]!.Value<string>().Should().Be(helmet);
        ((JObject)droid["Inventory"]!).Properties().Should().HaveCount(2);
        var stowed = ((JObject)droid["Inventory"]!).Properties().Single(x => x.Name != "old-id");
        var idLocal = Children(Read(stowed.Value.Value<string>()!), "VarTable").Single(x => (string)Value(x, "Name") == "DROID_ITEM_ID");
        Value(idLocal, "Value").Should().Be(stowed.Name);
        Value(Read(stowed.Value.Value<string>()!), "UUID").Should().Be("visor-uuid");
        Value(Read(droid["SerializedCPU"]!.Value<string>()!), "BaseItem").Should().Be((int)BaseItem.Helmet);
        droid["UnknownState"]!.Value<int>().Should().Be(42);
        HeadgearSlotMigration.MigrateSerializedObject(migrated, out var retry).Should().BeFalse();
        retry.Should().Be(migrated);
    }

    [TestCase("ruptorvanecr", BaseItem.Helmet)]
    [TestCase("piston_gaunt", BaseItem.Armor)]
    [TestCase("ordinary_armor", BaseItem.Armor)]
    public void CorrectItemsAndOtherEquipmentStayByteForByteUnchanged(string resref, BaseItem type)
    {
        var original = Encode(Node(0, Resref(resref), Scalar(5, "BaseItem", (int)type), Text("LocalizedName", "Ruptor Visor")));
        HeadgearSlotMigration.MigrateSerializedObject(original, out var migrated).Should().BeFalse();
        migrated.Should().Be(original);
    }

    // Build raw GFF fixtures with the established writer and inspect with the independent formats reader.
    private static readonly Type Document = typeof(HeadgearSlotMigration).Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.StoredObjectData")!;
    private static readonly Type NodeType = Document.GetNestedType("Node", BindingFlags.NonPublic)!;
    private static object Node(uint type, params object[] fields)
    {
        var node = Activator.CreateInstance(NodeType, true)!;
        NodeType.GetField("Type")!.SetValue(node, type);
        foreach (var field in fields) AddField(node, field);
        return node;
    }
    private static void AddField(object node, object field) => ((System.Collections.IList)NodeType.GetField("Fields")!.GetValue(node)!).Add(field);
    private static object Field(uint type, string name, byte[] data = null, params object[] children)
    {
        object list = null;
        if (type == 15)
        {
            list = Activator.CreateInstance(typeof(List<>).MakeGenericType(NodeType))!;
            foreach (var child in children) ((System.Collections.IList)list).Add(child);
        }
        return Document.GetMethod("MakeField", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new[] { (object)type, name, data, list })!;
    }
    private static object Scalar(uint type, string name, int value) => Field(type, name, BitConverter.GetBytes(value));
    private static object Text(string name, string text) { var bytes = Encoding.UTF8.GetBytes(text); return Field(10, name, BitConverter.GetBytes(bytes.Length).Concat(bytes).ToArray()); }
    private static object Resref(string resref) { var bytes = Encoding.ASCII.GetBytes(resref); return Field(11, "TemplateResRef", new[] { (byte)bytes.Length }.Concat(bytes).ToArray()); }
    private static object List(string name, params object[] children) => Field(15, name, null, children);
    private static object Visor(uint slot = 0, string id = "old-id")
    {
        var variables = new List<object> { Node(0, Text("Name", "CRAFTED_BY"), Scalar(4, "Type", 3), Text("Value", "crafting-identity")) };
        if (id != null) variables.Add(Node(0, Text("Name", "DROID_ITEM_ID"), Scalar(4, "Type", 3), Text("Value", id)));
        return Node(slot, Resref("ruptorvanecr"), Scalar(5, "BaseItem", (int)BaseItem.Armor),
            Text("UUID", "visor-uuid"), Text("DescIdentified", "custom description"), Scalar(2, "StackSize", 3),
            Scalar(0, "Metal1Color", 7), Scalar(0, "ArmorPart_Torso", 186),
            List("PropertiesList", Node(0, Scalar(2, "PropertyName", (int)ItemPropertyType.Attack), Scalar(2, "CostValue", 42))),
            List("VarTable", variables.ToArray()));
    }
    private static string Encode(object root, string fileType = "UTI ")
    {
        NodeType.GetField("Type")!.SetValue(root, uint.MaxValue);
        var document = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(Document);
        Document.GetField("_header", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(document, Encoding.ASCII.GetBytes(fileType + "V3.2"));
        Document.GetField("_root", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(document, root);
        return (string)Document.GetMethod("Serialize", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(document, null)!;
    }
    private static GffStruct Read(string data) => GffReader.Read(Convert.FromBase64String(data)).RootStruct;
    private static object Value(GffStruct node, string name) => node.Fields.Single(x => x.Label == name).Value!;
    private static IEnumerable<GffStruct> Children(GffStruct node, string name) => ((GffList)Value(node, name)).Elements;
}
