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
        new _22_ReviseLootStats().Version.Should().Be(22);
        typeof(_22_ReviseLootStats).Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration._23_RepairHeadgearSlots").Should().BeNull();
        typeof(_22_ReviseLootStats).Assembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration._31_RepairHeadgearSlots").Should().BeNull();
    }

    [TestCase(0)]
    [TestCase(33)]
    [TestCase(223)]
    public void ArchivedLegacyCopyKeepsEveryUnrelatedFieldAndIsByteStableOnRetry(int existingModel)
    {
        var root = Visor();
        AddField(root, Scalar(0, "ModelPart1", existingModel));
        var original = Encode(root);
        HeadgearSlotMigration.MigrateSerializedObject(original, out var migrated).Should().BeTrue();
        var before = Read(original);
        var after = Read(migrated);
        Value(after, "BaseItem").Should().Be((int)BaseItem.Helmet);
        Value(after, "ModelPart1").Should().Be((byte)(existingModel is 0 or 223 ? 110 : existingModel));
        after.Fields.Where(x => x.Label is not "BaseItem" and not "ModelPart1")
            .Should().BeEquivalentTo(before.Fields.Where(x => x.Label is not "BaseItem" and not "ModelPart1"));
        HeadgearSlotMigration.MigrateSerializedObject(migrated, out var retry).Should().BeFalse();
        retry.Should().Be(migrated);
    }

    [TestCase("ruptorvanecr", BaseItem.Helmet, 223, 110)]
    [TestCase("THERMLANCERCR", BaseItem.Armor, 223, 110)]
    [TestCase("ruptorvanecr", BaseItem.Armor, 0, 110)]
    [TestCase("charm_cowl", BaseItem.Armor, 0, 33)]
    [TestCase("ruptorvanecr", BaseItem.Helmet, 33, 33)]
    [TestCase("ruptorvanecr", BaseItem.Helmet, 110, 110)]
    [TestCase("ruptorvanecr", BaseItem.Cloak, 223, 223)]
    [TestCase("revan_mask", BaseItem.Helmet, 223, 223)]
    [TestCase("other_helmet", BaseItem.Helmet, 223, 223)]
    public void ModelRepairTargetsOnlyTheAffectedVisors(string resref, BaseItem type, int model, int expected)
    {
        HeadgearSlotMigration.GetCorrectedModel(resref, type, model).Should().Be(expected);
    }

    [TestCase(223, 223)]
    [TestCase(110, 223)]
    [TestCase(223, -1)]
    public void ArchivedHelmetRepairsBothModelFieldsWithoutChangingOtherState(int legacyModel, int extendedModel)
    {
        var root = Node(0, Resref("thermlancercr"), Scalar(5, "BaseItem", (int)BaseItem.Helmet),
            Scalar(0, "ModelPart1", legacyModel), Text("UUID", "saved-visor"), Scalar(2, "StackSize", 3),
            Text("Description", "custom description"), Scalar(0, "Metal1Color", 7));
        if (extendedModel >= 0) AddField(root, Scalar(2, "xModelPart1", extendedModel));
        var original = Encode(root);
        var bag = Encode(Node(0, Scalar(5, "BaseItem", (int)BaseItem.MiscSmall), List("ItemList", root)));
        foreach (var data in new[] { original, bag })
        {
            HeadgearSlotMigration.MigrateSerializedObject(data, out var migrated).Should().BeTrue();
            var after = Read(migrated);
            if (after.Fields.Any(x => x.Label == "ItemList")) after = Children(after, "ItemList").Single();
            Value(after, "ModelPart1").Should().Be((byte)110);
            if (extendedModel >= 0) Value(after, "xModelPart1").Should().Be((ushort)110);
            after.Fields.Where(x => x.Label is not "ModelPart1" and not "xModelPart1")
                .Should().BeEquivalentTo(Read(original).Fields.Where(x => x.Label is not "ModelPart1" and not "xModelPart1"));
            HeadgearSlotMigration.MigrateSerializedObject(migrated, out var retry).Should().BeFalse();
            retry.Should().Be(migrated);
        }
    }

    [Test]
    public void SavedHelmetModelRepairDoesNotMoveDroidEquipmentOrChangeItsIdentity()
    {
        var visor = Encode(Node(0, Resref("ruptorvanecr"), Scalar(5, "BaseItem", (int)BaseItem.Helmet),
            Scalar(0, "ModelPart1", 223), Scalar(2, "xModelPart1", 223), Text("UUID", "same-visor")));
        var original = new JObject { ["Name"] = "same droid", ["EquippedItems"] = new JObject { ["Head"] = visor }, ["Inventory"] = new JObject() }.ToString();
        var method = typeof(HeadgearSlotMigration).GetMethod("MigrateDroid", BindingFlags.NonPublic | BindingFlags.Static)!;
        var migrated = (string)method.Invoke(null, new object[] { original, null, true })!;
        var droid = JObject.Parse(migrated);
        droid["Name"]!.Value<string>().Should().Be("same droid");
        ((JObject)droid["Inventory"]!).Should().BeEmpty();
        var saved = Read((string)droid["EquippedItems"]!["Head"]!);
        Value(saved, "UUID").Should().Be("same-visor");
        Value(saved, "xModelPart1").Should().Be((ushort)110);
        method.Invoke(null, new object[] { migrated, null, true }).Should().Be(migrated);
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
        var visits = new List<string>();
        Func<string, string> repair = data =>
        {
            if (!string.IsNullOrWhiteSpace(data)) visits.Add(data);
            return HeadgearSlotMigration.MigrateSerializedObject(data, out var converted) ? converted : data;
        };
        typeof(HeadgearSlotMigration).GetMethod("MigrateDroid", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { original.ToString(), repair, true });
        visits.Should().HaveCount(4, "each original component, equipped item and carried item is migrated once");
        visits.Count(data => data == visor).Should().Be(3, "stowing the chest copy must not visit it a second time");
        var controller = Encode(Node(0, List("VarTable", Node(0, Text("Name", "CONSTRUCTED_DROID"),
            Scalar(4, "Type", 3), Text("Value", original.ToString())))));
        HeadgearSlotMigration.MigrateSerializedObject(controller, out var migrated).Should().BeTrue();
        var droid = JObject.Parse((string)Value(Children(Read(migrated), "VarTable").Single(), "Value"));
        ((JObject)droid["EquippedItems"]!).Properties().Should().ContainSingle();
        droid["EquippedItems"]!["Head"]!.Value<string>().Should().Be(helmet);
        ((JObject)droid["Inventory"]!).Properties().Should().HaveCount(2);
        var stowed = ((JObject)droid["Inventory"]!).Properties().Single(x => x.Name != "old-id");
        var idLocal = Children(Read(stowed.Value.Value<string>()!), "VarTable").Single(x => (string)Value(x, "Name") == "DROID_ITEM_ID");
        stowed.Name.Should().Be("DROID_RESTORE_ITEM:" + (int)InventorySlot.Head + ":" + Value(idLocal, "Value"));
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

    [TestCase(false)]
    [TestCase(true)]
    public void PendingChestRestoresAreRetargetedWithoutDuplicatingOrLosingTheSavedCopy(bool missingId)
    {
        var legacy = Encode(Visor(id: missingId ? null : "legacy-id"));
        var oldKey = "DROID_RESTORE_ITEM:" + (int)InventorySlot.Chest + ":legacy-id";
        var archive = new JObject { ["Inventory"] = new JObject { [oldKey] = legacy }, ["UnknownState"] = 42 };
        var visits = 0;
        Func<string, string> repair = data =>
        {
            if (!string.IsNullOrWhiteSpace(data)) visits++;
            return HeadgearSlotMigration.MigrateSerializedObject(data, out var converted) ? converted : data;
        };
        var method = typeof(HeadgearSlotMigration).GetMethod("MigrateDroid", BindingFlags.Static | BindingFlags.NonPublic)!;
        var migrated = (string)method.Invoke(null, new object[] { archive.ToString(), repair, true })!;
        visits.Should().Be(1, "retargeting a pending entry must not migrate it a second time");
        var result = JObject.Parse(migrated);
        var item = ((JObject)result["Inventory"]!).Properties().Should().ContainSingle().Subject;
        item.Name.Should().StartWith("DROID_RESTORE_ITEM:" + (int)InventorySlot.Head + ":");
        var saved = Read(item.Value.Value<string>()!);
        Value(saved, "BaseItem").Should().Be((int)BaseItem.Helmet);
        Value(saved, "UUID").Should().Be("visor-uuid");
        result["UnknownState"]!.Value<int>().Should().Be(42);
        method.Invoke(null, new object[] { migrated, repair, true }).Should().Be(migrated);
    }

    [TestCase("hexweave", BaseItem.Bracer, BaseItem.Cloak, InventorySlot.Arms, InventorySlot.Cloak, false, false)]
    [TestCase("hexweave", BaseItem.Bracer, BaseItem.Cloak, InventorySlot.Arms, InventorySlot.Cloak, true, false)]
    [TestCase("absdef_l3", BaseItem.Gloves, BaseItem.Helmet, InventorySlot.Arms, InventorySlot.Head, false, false)]
    [TestCase("absdef_l3", BaseItem.Gloves, BaseItem.Helmet, InventorySlot.Arms, InventorySlot.Head, false, true)]
    [TestCase("piston_gaunt", BaseItem.Armor, BaseItem.Gloves, InventorySlot.Chest, InventorySlot.Arms, false, false)]
    [TestCase("piston_gaunt", BaseItem.Armor, BaseItem.Gloves, InventorySlot.Chest, InventorySlot.Arms, true, false)]
    [TestCase("piston_gaunt", BaseItem.Armor, BaseItem.Gloves, InventorySlot.Chest, InventorySlot.Arms, false, true)]
    [TestCase("brawl_wraps", BaseItem.Bracer, BaseItem.Gloves, InventorySlot.Arms, InventorySlot.Arms, false, false)]
    public void CorrectedDroidWearablesPreserveCollidingCopiesAndRestoreTheCorrectSlot(
        string resref, BaseItem legacyType, BaseItem correctedType, InventorySlot previousSlot, InventorySlot targetSlot, bool numeric, bool pending)
    {
        object Wearable(BaseItem type, string uuid) => Node(0, Resref(resref), Scalar(5, "BaseItem", (int)type),
            Scalar(0, "ModelPart1", EquipmentTypeMigration.Corrections[resref].Model), Text("UUID", uuid), Scalar(2, "StackSize", 3),
            List("VarTable", Node(0, Text("Name", "DROID_ITEM_ID"), Scalar(4, "Type", 3), Text("Value", "same-id"))));
        var original = Encode(Wearable(legacyType, "equipped-uuid"));
        var replacement = Encode(Wearable(correctedType, "equipped-uuid"));
        var carried = Encode(Wearable(correctedType, "carried-uuid"));
        var archive = new JObject { ["EquippedItems"] = new JObject(), ["Inventory"] = new JObject { ["same-id"] = carried } };
        var key = pending ? "DROID_RESTORE_ITEM:" + (int)previousSlot + ":same-id" : numeric ? ((int)previousSlot).ToString() : previousSlot.ToString();
        ((JObject)archive[pending ? "Inventory" : "EquippedItems"]!)[key] = original;
        Func<string, string> convert = data => data == original ? replacement : data;
        var method = typeof(HeadgearSlotMigration).GetMethod("MigrateDroid", BindingFlags.Static | BindingFlags.NonPublic)!;
        var migrated = (string)method.Invoke(null, new object[] { archive.ToString(), convert, true })!;
        var result = JObject.Parse(migrated);
        ((string)result["Inventory"]!["same-id"]!).Should().Be(carried, "the colliding carried copy must never be overwritten");
        string repaired;
        if (previousSlot != targetSlot)
        {
            ((JObject)result[pending ? "Inventory" : "EquippedItems"]!).Property(key).Should().BeNull();
            var moved = ((JObject)result["Inventory"]!).Properties().Single(x => x.Name.StartsWith("DROID_RESTORE_ITEM:" + (int)targetSlot + ":", StringComparison.Ordinal));
            repaired = (string)moved.Value!;
            ((JObject)result["Inventory"]!).Should().HaveCount(2);
        }
        else
        {
            repaired = (string)result["EquippedItems"]![key]!;
            ((JObject)result["Inventory"]!).Should().HaveCount(1);
        }
        var item = Read(repaired);
        Value(item, "BaseItem").Should().Be((int)correctedType);
        Value(item, "UUID").Should().Be("equipped-uuid");
        Value(item, "StackSize").Should().Be((ushort)3);
        method.Invoke(null, new object[] { migrated, convert, true }).Should().Be(migrated, "a retry must be byte-stable");
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
