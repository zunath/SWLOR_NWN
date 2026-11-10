#nullable enable
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.Formats.Gff;

namespace SWLOR.Game.Server.Tests.Feature;

public partial class SavedDataItemSweepTests
{
    private static object Staff(string resref, int bottom, int middle, int top, params object[] fields) =>
        Item(resref, BaseItem.QuarterStaff, bottom, new object[]
        {
            Scalar(0, "ModelPart2", middle), Scalar(2, "xModelPart2", middle),
            Scalar(0, "ModelPart3", top), Scalar(2, "xModelPart3", top),
        }.Concat(fields).ToArray());

    private static (int, int, int, int, int, int) StaffParts(GffStruct item) =>
        (Int(item, "ModelPart1"), Int(item, "xModelPart1"), Int(item, "ModelPart2"), Int(item, "xModelPart2"),
            Int(item, "ModelPart3"), Int(item, "xModelPart3"));

    [Test]
    public void Gaderffii_RestoresThePlainStaffCopiesTheCatalogMigrationMoved()
    {
        var original = Encode(Staff("sandstorm_gaffi", 53, 11, 11, Scalar(2, "StackSize", 1), Text("UUID", "owned-gaderffii"),
            Property(ItemPropertyType.Attack, 0, 45, 7, param1Value: 0),
            List("VarTable", Node(0, Text("Name", "GADERFFII_OWNER"), Scalar(4, "Type", 1), Scalar(5, "Value", 42)))));

        EnzymeBaseItemMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        StaffParts(root).Should().Be((62, 62, 62, 62, 62, 62));
        Int(root, "BaseItem").Should().Be((int)BaseItem.QuarterStaff);
        Value(root, "UUID").Should().Be("owned-gaderffii");
        Children(root, "PropertiesList").Select(property => Int(property, "CostValue")).Should().Equal(7);
        Locals(root).Should().Equal(("GADERFFII_OWNER", 42));

        EnzymeBaseItemMigration.MigrateSavedObject(migrated, out var rerun).Should().BeFalse("the staff already shows the Gaderffii");
        rerun.Should().BeSameAs(migrated);
    }

    [TestCase("sandstorm_gaffi", 53, 11, 21, TestName = "Gaderffii_KeepsARestyledTop")]
    [TestCase("sandstorm_gaffi", 53, 21, 11, TestName = "Gaderffii_KeepsARestyledMiddle")]
    [TestCase("sandstorm_gaffi", 11, 11, 11, TestName = "Gaderffii_KeepsARestyledBottom")]
    [TestCase("sandstorm_gaffi", 62, 11, 11, TestName = "Gaderffii_LeavesCopiesTheCatalogMigrationHasNotMoved")]
    [TestCase("gutter_staff", 53, 11, 11, TestName = "Gaderffii_LeavesOtherStaffsAlone")]
    public void Gaderffii_LeavesEverythingElseUntouched(string resref, int bottom, int middle, int top)
    {
        var original = Encode(Staff(resref, bottom, middle, top));
        EnzymeBaseItemMigration.MigrateSavedObject(original, out var migrated).Should().BeFalse();
        migrated.Should().BeSameAs(original);
    }

    [Test]
    public void Gaderffii_IgnoresTheResrefOnAnotherBaseItem()
    {
        var original = Encode(Item("sandstorm_gaffi", BaseItem.Longsword, 53,
            Scalar(0, "ModelPart2", 11), Scalar(2, "xModelPart2", 11), Scalar(0, "ModelPart3", 11), Scalar(2, "xModelPart3", 11)));
        EnzymeBaseItemMigration.MigrateSavedObject(original, out var migrated).Should().BeFalse();
        migrated.Should().BeSameAs(original);
    }

    [Test]
    public void Gaderffii_ConvertsWornAndBaggedStaffsOnSavedCreatures()
    {
        var bag = Item("bag", BaseItem.LargeBox, 1,
            List("ItemList", Staff("sandstorm_gaffi", 53, 11, 11, Text("UUID", "bagged"))));
        var original = Encode(Node(0, Text("Tag", "saved-npc"),
            List("Equip_ItemList", Staff("sandstorm_gaffi", 53, 11, 11, Text("UUID", "worn"))),
            List("ItemList", bag, Staff("gutter_staff", 53, 11, 11, Text("UUID", "other")))), "UTC ");

        EnzymeBaseItemMigration.MigrateSavedObject(original, out var migrated).Should().BeTrue();
        var root = Read(migrated);
        var worn = Children(root, "Equip_ItemList").Single();
        (StaffParts(worn), Value(worn, "UUID")).Should().Be(((62, 62, 62, 62, 62, 62), "worn"));
        var carried = Children(root, "ItemList").ToList();
        var bagged = Children(carried[0], "ItemList").Single();
        (StaffParts(bagged), Value(bagged, "UUID")).Should().Be(((62, 62, 62, 62, 62, 62), "bagged"));
        StaffParts(carried[1]).Should().Be((53, 53, 11, 11, 11, 11), "another staff keeps its look");
        EnzymeBaseItemMigration.MigrateSavedObject(migrated, out _).Should().BeFalse();
    }

    [Test]
    public void Gaderffii_RevisionFollowsTheBlueprintAndTheCatalogMigrationItUndoes()
    {
        var (baseItem, previous, current) = GaderffiiAppearanceMigration.Revisions["sandstorm_gaffi"];
        ItemAppearanceCatalogMigration.Revisions["sandstorm_gaffi"].Current.Should().Equal(previous,
            "the revision starts from the plain staff the shipped catalog migration saved");
        baseItem.Should().Be(BaseItem.QuarterStaff);
        current.Should().Equal(62, 62, 62);
    }
}
