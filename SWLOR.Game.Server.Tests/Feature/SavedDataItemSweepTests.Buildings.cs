using System.Security.Cryptography;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public partial class SavedDataItemSweepTests
{
    [Test]
    public void BuildingIcons_ConvertAllOwnedBuildingsAndPreserveTheirIdentityOnRetry()
    {
        BuildingIconMigration.Models.Should().HaveCount(18);
        foreach (var (resref, model) in BuildingIconMigration.Models)
        {
            var current = Encode(Item(resref, (BaseItem)29, 202, Text("UUID", resref),
                Scalar(2, "StackSize", 1), Property(ItemPropertyType.CastSpell, 329, 3, 13)));
            for (var pass = 0; pass < 3; pass++)
            {
                EnzymeBaseItemMigration.MigrateSavedObject(current, out var converted).Should().Be(pass == 0);
                var item = Read(converted);
                (Int(item, "BaseItem"), Int(item, "ModelPart1"), Int(item, "xModelPart1")).Should().Be((29, model, model));
                Value(item, "UUID").Should().Be(resref);
                Int(item, "StackSize").Should().Be(1);
                Int(Children(item, "PropertiesList").Single(), "Subtype").Should().Be(329);
                if (pass > 0) converted.Should().Be(current);
                current = converted;
            }
            ItemIconMigration.GetUpdatedModel(resref.ToUpperInvariant(), 29, 202).Should().Be(model);
            ItemIconMigration.GetUpdatedIcon(resref, "IIT_MIDMISC_202").Should().Be($"iit_midmisc_{model:000}");
            BuildingIconMigration.GetUpdatedModel(resref, 29, 203).Should().Be(203);
            BuildingIconMigration.GetUpdatedModel(resref, 24, 202).Should().Be(202);
            BuildingIconMigration.GetUpdatedIcon(resref, "custom_icon").Should().Be("custom_icon");
            EnzymeBaseItemMigration.MigrateSavedObject(Encode(Item(resref, (BaseItem)29, 203)), out _).Should().BeFalse();
        }
        BuildingIconMigration.GetUpdatedModel("unrelated", 29, 202).Should().Be(202);
    }

    [Test]
    public void BuildingIcons_TravelThroughBagsAlongsideEnzymesInTheReleaseVisit()
    {
        var original = Encode(Item("bag", BaseItem.LargeBox, 1, List("ItemList",
            Item("structure_5003", (BaseItem)29, 202, Text("UUID", "owned-starport")),
            Item("hydrolase_blue", BaseItem.Electronics5, 3))));
        EnzymeBaseItemMigration.MigrateSavedObject(original, out var converted).Should().BeTrue();
        var contents = Children(Read(converted), "ItemList").ToArray();
        Int(contents[0], "ModelPart1").Should().Be(154);
        Value(contents[0], "UUID").Should().Be("owned-starport");
        Int(contents[1], "BaseItem").Should().Be(544);
        EnzymeBaseItemMigration.MigrateSavedObject(converted, out _).Should().BeFalse();
    }

    [Test]
    public void BuildingIcons_BlueprintsMatchDistinctNativeDdsResources()
    {
        var root = FindRepositoryRoot();
        foreach (var (resref, model) in BuildingIconMigration.Models)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", "uti", resref + ".uti.json")));
            foreach (var field in new[] { "ModelPart1", "xModelPart1" })
                blueprint.RootElement.GetProperty(field).GetProperty("value").GetInt32().Should().Be(model, resref);
        }
        var hashes = new HashSet<string>();
        foreach (var model in BuildingIconMigration.Models.Values.Distinct())
        {
            var path = Path.Combine(root, "SWLOR_Haks", "sw_item", $"iit_midmisc_{model:000}");
            var dds = File.ReadAllBytes(path + ".dds");
            dds.Should().HaveCount(128 + 4096);
            System.Text.Encoding.ASCII.GetString(dds, 84, 4).Should().Be("DXT5");
            (BitConverter.ToInt32(dds, 12), BitConverter.ToInt32(dds, 16)).Should().Be((64, 64));
            BitConverter.ToInt32(dds, 28).Should().Be(1);
            hashes.Add(Convert.ToHexString(SHA256.HashData(dds))).Should().BeTrue();
            var stub = File.ReadAllBytes(path + ".tga");
            stub.Should().HaveCount(18 + 8 * 8 * 4);
            stub[17].Should().Be(8);
        }
        hashes.Should().HaveCount(9);
    }
}
