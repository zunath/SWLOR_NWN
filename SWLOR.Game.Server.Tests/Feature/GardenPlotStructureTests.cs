using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>The Garden Plot structure and the IDs the farming slice of the Player City Revamp takes from its reserved ranges.</summary>
public class GardenPlotStructureTests
{
    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;

            directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
            return directory!.FullName;
        }
    }

    private static StructureAttribute Plot => StructureType.GardenPlot.GetAttribute<StructureType, StructureAttribute>();

    [Test]
    public void TheGardenPlotIsAnActiveBasicStructureForCitiesAndHousesOnly()
    {
        Plot.IsActive.Should().BeTrue();
        Plot.Name.Should().Be("Garden Plot");
        Plot.RestrictedPropertyTypes.Should().Be(PropertyType.City | PropertyType.House);
        Plot.LayoutType.Should().Be(PropertyLayoutType.Invalid, "a plot has no interior");
        Plot.Category.Should().Be(StructureCategoryType.Structure, "an outdoor plot counts against the city structure limit");
        Plot.CanBeRetrieved.Should().BeTrue();
        Plot.ItemStorage.Should().Be(0);
        Plot.Description.Should().MatchRegex("^[\\x20-\\x7E]+$");
    }

    [Test]
    public void ThePlotKeepsClearOfTheCityOnlyBuildingLogic()
    {
        // A structure restricted to City alone is a building and counts against the building limit; the plot is not one.
        Plot.RestrictedPropertyTypes.Should().NotBe(PropertyType.City);
    }

    [Test]
    public void TheReservedIdsAreUsed()
    {
        ((int)StructureType.GardenPlot).Should().Be(5100).And.BeInRange(5100, 5129);
        ((int)GuiWindowType.GardenPlot).Should().Be(112).And.BeInRange(110, 129);
        ((int)PropertyPermissionType.TendGardens).Should().Be(21);
        ScriptName.OnGardenPlotUsed.Length.Should().BeLessThanOrEqualTo(16);
    }

    [Test]
    public void TheTendGardensPermissionIsActiveAndExplainsItself()
    {
        var permission = PropertyPermissionType.TendGardens.GetAttribute<PropertyPermissionType, PropertyPermissionAttribute>();

        permission.IsActive.Should().BeTrue();
        permission.Name.Should().Be("Tend Gardens");
        permission.Description.Should().NotBeNullOrWhiteSpace().And.MatchRegex("^[\\x20-\\x7E]+$");
    }

    [Test]
    public void ThePlotUsesAnExistingUsablePlaceableWithoutAnInventory()
    {
        var path = Path.Combine(Root, "Module", "utp", Plot.Resref + ".utp.json");

        File.Exists(path).Should().BeTrue($"the placeable '{Plot.Resref}' exists in the module");
        using var blueprint = JsonDocument.Parse(File.ReadAllText(path));
        Value(blueprint, "Static").Should().Be(0, "a static placeable cannot be used");
        Value(blueprint, "HasInventory").Should().Be(0, "a placeable with an inventory opens it instead of the plot window");

        static int Value(JsonDocument document, string field) => document.RootElement.GetProperty(field).GetProperty("value").GetInt32();
    }

    [Test]
    public void ARecordIsKeyedApartFromItsPlotStructure()
    {
        var plot = new GardenPlot();

        GardenPlot.KeyFor("abc").Should().Be("garden-plot:abc").And.NotBe("abc");
        plot.PlotId.Should().BeEmpty();
        plot.PlanterPlayerId.Should().BeEmpty();
        plot.PlanterCdKey.Should().BeEmpty();
        plot.EntityType.Should().Be(nameof(GardenPlot));
    }
}
