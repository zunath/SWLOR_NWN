#nullable enable
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public partial class SavedDataItemSweepTests
{
    private const int EnzymeBaseItem = 544;

    private static IEnumerable<(string Resref, BaseItem Previous, int Model)> EnzymeBlueprints() =>
        Directory.EnumerateFiles(Path.Combine(FindRepositoryRoot(), "Module", "uti"), "*.uti.json")
            .Select(path => Path.GetFileName(path).Replace(".uti.json", ""))
            .Where(resref => resref.StartsWith("hydrolase_") || resref.StartsWith("lyase_") || resref.StartsWith("isomerase_"))
            .Select(resref => (resref, resref.StartsWith("isomerase_") ? BaseItem.Electronics4 : BaseItem.Electronics5, EnzymeModel(resref)));

    private static int EnzymeModel(string resref)
    {
        using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(FindRepositoryRoot(), "Module", "uti", resref + ".uti.json")));
        blueprint.RootElement.GetProperty("BaseItem").GetProperty("value").GetInt32().Should().Be(EnzymeBaseItem, resref);
        var model = blueprint.RootElement.GetProperty("ModelPart1").GetProperty("value").GetInt32();
        blueprint.RootElement.GetProperty("xModelPart1").GetProperty("value").GetInt32().Should().Be(model, resref);
        return model;
    }

    [Test]
    public void EnzymeShrink_MovesEveryEnzymeBlueprintToTheOneByOneBaseItemWithItsOwnIcon()
    {
        var blueprints = EnzymeBlueprints().ToList();
        blueprints.Should().HaveCount(15);
        blueprints.Select(blueprint => blueprint.Model).Should().OnlyHaveUniqueItems("each enzyme color has its own icon").And.BeEquivalentTo(Enumerable.Range(1, 15));
        var haks = Path.Combine(FindRepositoryRoot(), "SWLOR_Haks");
        foreach (var (resref, previous, model) in blueprints)
        {
            var legacy = Encode(Item(resref, previous, 3, Scalar(2, "StackSize", 1), Text("UUID", "enzyme-" + resref)));
            PelvisAppearanceMigration.MigrateSavedObject(legacy, out var migrated).Should().BeTrue(resref);
            var root = Read(migrated);
            Int(root, "BaseItem").Should().Be(EnzymeBaseItem, resref);
            (Int(root, "ModelPart1"), Int(root, "xModelPart1")).Should().Be((model, model), resref);
            Value(root, "UUID").Should().Be("enzyme-" + resref);
            Int(root, "StackSize").Should().Be(1);
        }
        foreach (var model in Enumerable.Range(1, 15))
        {
            var icon = Path.Combine(haks, "sw_item", $"iit_enzyme_{model:000}");
            var dds = File.ReadAllBytes(icon + ".dds");
            dds.Should().HaveCount(128 + 16 * 8 * 8, "a single-level 32x32 DXT5 inventory icon");
            System.Text.Encoding.ASCII.GetString(dds, 84, 4).Should().Be("DXT5");
            (BitConverter.ToInt32(dds, 12), BitConverter.ToInt32(dds, 16)).Should().Be((32, 32));
            File.ReadAllBytes(icon + ".tga").Should().HaveCount(18 + 8 * 8 * 4, "the 8x8 registration stub");
        }
        Enumerable.Range(1, 15).Select(model => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                File.ReadAllBytes(Path.Combine(haks, "sw_item", $"iit_enzyme_{model:000}.dds")))))
            .Should().OnlyHaveUniqueItems("different enzymes need different artwork");
    }

    [Test]
    public void EnzymeBaseItemRow_IsOneByOneAndTheOldEssenceRowsStayTwoByTwo()
    {
        var rows = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_2da", "baseitems.2da")).Skip(3)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToDictionary(columns => int.Parse(columns[0]));
        var header = File.ReadAllLines(Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_2da", "baseitems.2da"))[2]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        string Column(int row, string name) => rows[row][header.IndexOf(name) + 1];
        Column(EnzymeBaseItem, "label").Should().Be("enzyme");
        (Column(EnzymeBaseItem, "InvSlotWidth"), Column(EnzymeBaseItem, "InvSlotHeight")).Should().Be(("1", "1"));
        (Column(EnzymeBaseItem, "ItemClass"), Column(EnzymeBaseItem, "Stacking"), Column(EnzymeBaseItem, "ModelType")).Should().Be(("it_enzyme", "1", "0"));
        foreach (var essence in new[] { (int)BaseItem.Electronics4, (int)BaseItem.Electronics5 })
            (Column(essence, "InvSlotWidth"), Column(essence, "InvSlotHeight")).Should().Be(("2", "2"), "beast DNA and other items still use this row");
        rows.Keys.Max().Should().BeGreaterThanOrEqualTo(EnzymeBaseItem);
    }

    [Test]
    public void EnzymeShrink_KeepsStatsAndIdentityAndIsIdempotent()
    {
        var original = Encode(Item("hydrolase_blue", BaseItem.Electronics5, 3,
            Scalar(2, "StackSize", 1), Text("UUID", "retained-enzyme"),
            Property(ItemPropertyType.Attack, 0, 45, 7, param1Value: 0),
            List("VarTable", Node(0, Text("Name", "ENZYME_PRIMARY"), Scalar(4, "Type", 1), Scalar(5, "Value", 42)))));

        PelvisAppearanceMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        (Int(root, "BaseItem"), Int(root, "ModelPart1"), Int(root, "xModelPart1")).Should().Be((EnzymeBaseItem, 1, 1));
        Value(root, "UUID").Should().Be("retained-enzyme");
        Children(root, "PropertiesList").Select(property => Int(property, "CostValue")).Should().Equal(7);
        Locals(root).Should().Equal(("ENZYME_PRIMARY", 42));

        PelvisAppearanceMigration.MigrateSavedObject(migrated, out var rerun).Should().BeFalse("the item is no longer on a 2x2 base item");
        rerun.Should().BeSameAs(migrated);
    }

    [TestCase("beast_dna", BaseItem.Electronics5, 1)]
    [TestCase("dna_0001", BaseItem.Electronics5, 1)]
    [TestCase("hydrolase_blue", BaseItem.MiscSmall, 3)]
    [TestCase("isomerase_red", BaseItem.Electronics5, 6)]
    [TestCase("lyase_black", BaseItem.Electronics4, 8)]
    [TestCase("hydrolase_blue", (BaseItem)EnzymeBaseItem, 1)]
    public void EnzymeShrink_LeavesEverythingElseUntouched(string resref, BaseItem baseItem, int model)
    {
        var original = Encode(Item(resref, baseItem, model));
        PelvisAppearanceMigration.MigrateSavedObject(original, out var migrated).Should().BeFalse();
        migrated.Should().BeSameAs(original);
    }

    [Test]
    public void EnzymeShrink_TraversesBagsDroidsAndSavedCreaturesWithoutChangingOwnership()
    {
        string Enzyme(string resref, BaseItem previous, string id) => Encode(Item(resref, previous, 3, Text("UUID", id)));
        var droid = new JObject
        {
            ["SerializedBody"] = Enzyme("lyase_red", BaseItem.Electronics5, "body"),
            ["EquippedItems"] = new JObject { ["Chest"] = Enzyme("hydrolase_red", BaseItem.Electronics5, "worn") },
            ["Inventory"] = new JObject { ["owned-id"] = Enzyme("isomerase_blue", BaseItem.Electronics4, "carried") },
            ["Name"] = "Retained droid",
        };
        var controller = Item("droid_ctrl", (BaseItem)24, 1, List("VarTable", Node(0,
            Text("Name", "CONSTRUCTED_DROID"), Scalar(4, "Type", 3), Text("Value", droid.ToString(Formatting.None)))));
        var bag = Item("bag", BaseItem.LargeBox, 1,
            List("ItemList", Item("lyase_green", BaseItem.Electronics5, 3, Text("UUID", "nested")), controller));
        var original = Encode(Node(0, Text("Tag", "saved-npc"),
            List("Equip_ItemList", Item("beast_dna", BaseItem.Electronics5, 1)),
            List("ItemList", bag, Item("hydrolase_white", BaseItem.Electronics5, 3, Text("UUID", "top")))), "UTC ");

        PelvisAppearanceMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        Value(root, "Tag").Should().Be("saved-npc");
        Int(Children(root, "Equip_ItemList").Single(), "BaseItem").Should().Be((int)BaseItem.Electronics5, "DNA keeps its row");
        var carried = Children(root, "ItemList").ToList();
        (Int(carried[1], "BaseItem"), Int(carried[1], "ModelPart1")).Should().Be((EnzymeBaseItem, 5));
        Value(carried[1], "UUID").Should().Be("top");
        var nested = Children(carried[0], "ItemList").ToList();
        (Int(nested[0], "BaseItem"), Int(nested[0], "ModelPart1")).Should().Be((EnzymeBaseItem, 8));
        Value(nested[0], "UUID").Should().Be("nested");
        var converted = JObject.Parse((string)Value(Children(nested[1], "VarTable").Single(), "Value"));
        converted["Name"]!.Value<string>().Should().Be("Retained droid");
        foreach (var (value, id, model) in new[] { ((string)converted["SerializedBody"]!, "body", 10),
                     ((string)converted["EquippedItems"]!["Chest"]!, "worn", 4), ((string)converted["Inventory"]!["owned-id"]!, "carried", 11) })
        {
            var enzyme = Read(value);
            (Int(enzyme, "BaseItem"), Int(enzyme, "ModelPart1")).Should().Be((EnzymeBaseItem, model));
            Value(enzyme, "UUID").Should().Be(id);
        }
        PelvisAppearanceMigration.MigrateSavedObject(migrated, out _).Should().BeFalse();
    }

    [TestCase("hydrolase_orange", "iit_ess5_004", "iit_enzyme_002")]
    [TestCase("hydrolase_orange", "iit_ess5_154", "iit_enzyme_002")]
    [TestCase("isomerase_yellow", "iit_ess4_007", "iit_enzyme_015")]
    [TestCase("lyase_black", "iit_ess5_008", "iit_enzyme_006")]
    [TestCase("lyase_black", "iit_enzyme_006", null)]
    [TestCase("lyase_black", "iit_ess2_008", null)]
    [TestCase("beast_dna", "iit_ess5_003", null)]
    public void EnzymeShrink_UpdatesOnlyLegacyListingIcons(string resref, string icon, string? expected)
    {
        var updated = TryUpdateEnzymeListingIcon(resref, icon, out var result);
        updated.Should().Be(expected != null);
        result.Should().Be(expected);
    }

    private static bool TryUpdateEnzymeListingIcon(string resref, string icon, out string? result)
    {
        string? written = null;
        var update = ServerAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.EnzymeBaseItemMigration")!.GetMethod("UpdateListingIcon", BindingFlags.NonPublic | BindingFlags.Static)!;
        var changed = (bool)update.Invoke(null, new object[] { resref, icon, (Action<string>)(value => written = value) })!;
        result = written;
        return changed;
    }

    [Test]
    public void EnzymeShrink_CharacterRetryChangesOnlyTheOldFile()
    {
        const BindingFlags members = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        var characterType = ServerAssembly.GetType("SWLOR.Game.Server.Service.MigrationService.SavedCharacter")!;
        var migrationType = ServerAssembly.GetType("SWLOR.Game.Server.Feature.MigrationDefinition.CharacterMigration._25_ConsolidatePelvisAppearance")!;
        var migration = Activator.CreateInstance(migrationType, true)!;
        var record = new Player { Version = 25, CharacterFileVersion = 25, HP = 17, FP = 13, Stamina = 11 };
        var beforeRecord = JsonConvert.SerializeObject(record);
        var original = Encode(Node(0,
            List("ItemList",
                Item("hydrolase_blue", BaseItem.Electronics5, 3, Scalar(2, "StackSize", 1), Text("UUID", "player-enzyme"),
                    Scalar(0, "Repos_PosX", 2), Scalar(0, "Repos_Posy", 4)),
                Item("beast_dna", BaseItem.Electronics5, 1, Text("UUID", "player-dna"), Scalar(0, "Repos_PosX", 4), Scalar(0, "Repos_Posy", 4)),
                Item("bag", BaseItem.LargeBox, 1, Text("UUID", "player-bag"),
                    List("ItemList", Item("isomerase_green", BaseItem.Electronics4, 4, Text("UUID", "bagged-enzyme")))))), "BIC ");
        var current = original;
        for (var pass = 0; pass < 3; pass++)
        {
            var character = Activator.CreateInstance(characterType, members, null,
                new object[] { Convert.FromBase64String(current), record }, null)!;
            characterType.GetProperty("AppliesRecordEffects", members)!.SetValue(character, false);
            migrationType.GetMethod("Migrate")!.Invoke(migration, new[] { character });
            var migrated = Convert.ToBase64String((byte[])characterType.GetMethod("Serialize")!.Invoke(character, null)!);
            if (pass > 0) migrated.Should().Be(current);
            current = migrated;
            var items = Children(Read(current), "ItemList").ToList();
            (Int(items[0], "BaseItem"), Int(items[0], "ModelPart1")).Should().Be((EnzymeBaseItem, 1));
            (Int(items[0], "Repos_PosX"), Int(items[0], "Repos_Posy")).Should().Be((2, 4), "the enzyme keeps its inventory cell");
            Value(items[0], "UUID").Should().Be("player-enzyme");
            Int(items[1], "BaseItem").Should().Be((int)BaseItem.Electronics5);
            var bagged = Children(items[2], "ItemList").Single();
            (Int(bagged, "BaseItem"), Int(bagged, "ModelPart1")).Should().Be((EnzymeBaseItem, 12));
            JsonConvert.SerializeObject(record).Should().Be(beforeRecord);
        }
    }
}
