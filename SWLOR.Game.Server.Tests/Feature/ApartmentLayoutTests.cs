using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PropertyLayoutDefinition;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Feature;

public class ApartmentLayoutTests
{
    [TestCase(PropertyLayoutType.ApartmentSmallStyleUnfurnished, 111)]
    [TestCase(PropertyLayoutType.ApartmentSmallStyleFurnished, 112)]
    [TestCase(PropertyLayoutType.ApartmentMediumStyleUnfurnished, 113)]
    [TestCase(PropertyLayoutType.ApartmentMediumStyleFurnished, 114)]
    public void NewLayouts_HaveStableUniqueIdentities(PropertyLayoutType layout, int expectedId)
    {
        ((int)layout).Should().Be(expectedId);
        Enum.GetValues<PropertyLayoutType>().Count(value => value == layout).Should().Be(1);
        new ApartmentLayoutDefinition().Build().Should().ContainKey(layout);
    }

    [Test]
    public void MissingFurnishedSmallMap_IsUnavailableForRentButPreservesExistingLeases()
    {
        var layouts = new ApartmentLayoutDefinition().Build();
        var legacyLayout = layouts[PropertyLayoutType.ApartmentSmallStyleFurnished];

        layouts.Where(entry => !entry.Value.IsAvailableForRent).Select(entry => entry.Key)
            .Should().Equal(PropertyLayoutType.ApartmentSmallStyleFurnished);
        legacyLayout.AreaInstanceResref.Should().Be("playerap_s_fur");
        legacyLayout.StructureLimit.Should().Be(30);
        legacyLayout.ItemStorageLimit.Should().Be(25);
        legacyLayout.InitialPrice.Should().Be(7000);
        legacyLayout.PricePerDay.Should().Be(300);
    }

    [TestCase(PropertyLayoutType.ApartmentSmallStyleUnfurnished, "v_apartment_s00")]
    [TestCase(PropertyLayoutType.ApartmentMediumStyleUnfurnished, "v_apartment_m00")]
    [TestCase(PropertyLayoutType.ApartmentMediumStyleFurnished, "v_apartment_m01")]
    public void RentableStarWarsLayouts_UseTheirAuthoredMaps(PropertyLayoutType layoutType, string resref)
    {
        var layout = new ApartmentLayoutDefinition().Build()[layoutType];

        layout.IsAvailableForRent.Should().BeTrue();
        layout.AreaInstanceResref.Should().Be(resref);
        using var area = Load("are", resref);
        Text(area.RootElement, "Tileset").Should().Be("udp2");
    }

    [Test]
    public void ApartmentTemplates_AreRegisteredAndHaveCompleteResources()
    {
        using var module = Load("ifo", "module");
        var registered = List(module.RootElement, "Mod_Area_list")
            .Select(area => Text(area, "Area_Name")).ToArray();

        foreach (var layout in new ApartmentLayoutDefinition().Build().Values)
        {
            var resref = layout.AreaInstanceResref;
            registered.Count(area => area == resref).Should().Be(1, layout.Name);
            foreach (var extension in new[] { "are", "git", "gic" })
            {
                using var resource = Load(extension, resref);
                resource.RootElement.GetProperty("__data_type").GetString()
                    .Should().Be(extension.ToUpperInvariant() + " ");
            }
        }
    }

    [TestCase("v_apartment_s00")]
    [TestCase("v_apartment_m00")]
    [TestCase("v_apartment_m01")]
    [TestCase("playerap_s_fur")]
    public void Templates_PreserveExplorationEntryAndOneUsableExit(string resref)
    {
        using var git = Load("git", resref);
        var area = git.RootElement;
        var achievement = List(area, "VarTable")
            .Single(local => Text(local, "Name") == "EXPLORE_ACHIEVEMENT_ID");
        Number(achievement, "Type").Should().Be(1);
        Number(achievement, "Value").Should().Be((int)AchievementType.ExplorePlayerApartment);
        List(area, "WaypointList").Count(point => Text(point, "Tag") == "PROPERTY_ENTRANCE")
            .Should().Be(1);

        var exit = List(area, "Placeable List").Where(placeable => Text(placeable, "Tag") == "building_exit")
            .Should().ContainSingle().Subject;
        Number(exit, "Useable").Should().Be(1);
        Number(exit, "Static").Should().Be(0);
        Number(exit, "Plot").Should().Be(1);
        Text(exit, "OnUsed").Should().Be("generic_convo");
        Text(List(exit, "VarTable").Single(local => Text(local, "Name") == "CONVERSATION"), "Value")
            .Should().Be("PropertyExitDialog");
    }

    [TestCase("v_apartment_s00")]
    [TestCase("v_apartment_m00")]
    [TestCase("v_apartment_m01")]
    public void AreaComments_RemainAlignedWithPlacedInstances(string resref)
    {
        using var git = Load("git", resref);
        using var gic = Load("gic", resref);
        foreach (var property in gic.RootElement.EnumerateObject()
                     .Where(property => property.Value.ValueKind == JsonValueKind.Object))
        {
            // GIC lists retain one editor comment per instance in the same order.
            if (property.Value.TryGetProperty("type", out var type) && type.GetString() == "list")
                List(gic.RootElement, property.Name).Count().Should()
                    .Be(List(git.RootElement, property.Name).Count(), property.Name);
        }
    }

    private static IEnumerable<JsonElement> List(JsonElement element, string name) =>
        element.GetProperty(name).GetProperty("value").EnumerateArray();

    private static string Text(JsonElement element, string name) =>
        element.GetProperty(name).GetProperty("value").GetString();

    private static int Number(JsonElement element, string name) =>
        element.GetProperty(name).GetProperty("value").GetInt32();

    private static JsonDocument Load(string extension, string resref)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var root = directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", extension,
            $"{resref}.{extension}.json")));
    }
}
