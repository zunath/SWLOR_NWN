using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// How a city's properties nest: a city holds a building structure, the structure holds the house interior, and the interior holds its
/// garden plots. Pests and the Gardens situation both need a plot in a house to reach its city through that building structure.
/// </summary>
public class PropertyHierarchyRulesTests
{
    private readonly Dictionary<string, WorldProperty> _properties = new();

    private WorldProperty Add(string id, PropertyType type, string parentId = "", StructureType structure = StructureType.Invalid)
    {
        var property = new WorldProperty { Id = id, PropertyType = type, ParentPropertyId = parentId, StructureType = structure };
        _properties[id] = property;
        return property;
    }

    private WorldProperty Find(string id) => _properties.GetValueOrDefault(id);

    private IEnumerable<WorldProperty> ChildrenOf(IReadOnlyCollection<string> parentIds) =>
        _properties.Values.Where(property => parentIds.Contains(property.ParentPropertyId));

    private IReadOnlyCollection<WorldProperty> CityChildren(string cityId) => ChildrenOf(new[] { cityId }).ToList();

    [SetUp]
    public void SetUp()
    {
        _properties.Clear();

        // City > house building structure > house interior > indoor plot, and an outdoor plot on the city's land.
        Add("city", PropertyType.City);
        Add("house-building", PropertyType.Structure, "city", StructureType.SmallHouseStyle1);
        Add("house", PropertyType.House, "house-building");
        Add("indoor-plot", PropertyType.Structure, "house", StructureType.GardenPlot);
        Add("outdoor-plot", PropertyType.Structure, "city", StructureType.GardenPlot);
        Add("bank-building", PropertyType.Structure, "city", StructureType.Bank);
        Add("bank", PropertyType.Bank, "bank-building");
    }

    #region Finding the city

    [Test]
    public void APlotInAHouse_ReachesItsCity_ThroughTheBuildingStructure()
    {
        PropertyHierarchyRules.FindCityId("house", Find).Should().Be("city");
        PropertyHierarchyRules.FindCityId(Find("indoor-plot").ParentPropertyId, Find).Should().Be("city", "a plot's parent is the house interior");
        PropertyHierarchyRules.FindCityId("indoor-plot", Find).Should().Be("city");
    }

    [Test]
    public void EveryStepOfTheChain_ReachesTheCity()
    {
        foreach (var id in new[] { "city", "house-building", "house", "outdoor-plot", "bank-building", "bank" })
            PropertyHierarchyRules.FindCityId(id, Find).Should().Be("city", id);
    }

    [Test]
    public void APropertyOutsideAnyCity_HasNoCityId()
    {
        Add("apartment", PropertyType.Apartment);
        Add("apartment-plot", PropertyType.Structure, "apartment", StructureType.GardenPlot);

        PropertyHierarchyRules.FindCityId("apartment", Find).Should().BeEmpty();
        PropertyHierarchyRules.FindCityId("apartment-plot", Find).Should().BeEmpty();
        PropertyHierarchyRules.FindCityId("missing", Find).Should().BeEmpty();
        PropertyHierarchyRules.FindCityId("", Find).Should().BeEmpty();
        PropertyHierarchyRules.FindCityId(null, Find).Should().BeEmpty();
    }

    [Test]
    public void ABrokenChain_StopsInsteadOfLooping()
    {
        Add("loop-a", PropertyType.Structure, "loop-b");
        Add("loop-b", PropertyType.Structure, "loop-a");
        Add("orphan", PropertyType.House, "gone");

        PropertyHierarchyRules.FindCityId("loop-a", Find).Should().BeEmpty();
        PropertyHierarchyRules.FindCityId("orphan", Find).Should().BeEmpty();
    }

    #endregion

    #region Indoor gardens

    [Test]
    public void ACityWhoseFarmersOnlyGardenIndoors_CountsAsHavingGardens()
    {
        _properties.Remove("outdoor-plot");

        var types = PropertyHierarchyRules.GetStructureTypes(CityChildren("city"), ChildrenOf);

        types.Should().Contain(StructureType.GardenPlot);
        types.Should().Contain(StructureType.SmallHouseStyle1).And.Contain(StructureType.Bank);
    }

    [Test]
    public void AnOutdoorPlot_StillCounts()
    {
        _properties.Remove("indoor-plot");

        PropertyHierarchyRules.GetStructureTypes(CityChildren("city"), ChildrenOf).Should().Contain(StructureType.GardenPlot);
    }

    [Test]
    public void ACityWithNoPlotAnywhere_DoesNotCountAsHavingGardens()
    {
        _properties.Remove("indoor-plot");
        _properties.Remove("outdoor-plot");

        PropertyHierarchyRules.GetStructureTypes(CityChildren("city"), ChildrenOf).Should().NotContain(StructureType.GardenPlot);
    }

    [Test]
    public void APlotInABankOrAnotherBuilding_IsNotAGardenIndoors()
    {
        _properties.Remove("indoor-plot");
        _properties.Remove("outdoor-plot");
        Add("bank-plot", PropertyType.Structure, "bank", StructureType.GardenPlot);

        PropertyHierarchyRules.GetStructureTypes(CityChildren("city"), ChildrenOf).Should().NotContain(StructureType.GardenPlot, "only houses hold plots");
    }

    [Test]
    public void AnotherCitysHousePlot_IsNotThisCitys()
    {
        _properties.Remove("indoor-plot");
        _properties.Remove("outdoor-plot");
        Add("other-city", PropertyType.City);
        Add("other-building", PropertyType.Structure, "other-city", StructureType.SmallHouseStyle2);
        Add("other-house", PropertyType.House, "other-building");
        Add("other-plot", PropertyType.Structure, "other-house", StructureType.GardenPlot);

        PropertyHierarchyRules.GetStructureTypes(CityChildren("city"), ChildrenOf).Should().NotContain(StructureType.GardenPlot);
        PropertyHierarchyRules.GetStructureTypes(CityChildren("other-city"), ChildrenOf).Should().Contain(StructureType.GardenPlot);
    }

    [Test]
    public void ACityWithNothingBuilt_HasNoTypes_AndAsksNoQuestions()
    {
        var asked = 0;

        var types = PropertyHierarchyRules.GetStructureTypes(new List<WorldProperty>(), ids => { asked++; return ChildrenOf(ids); });

        types.Should().BeEmpty();
        asked.Should().Be(0);
    }

    #endregion

    #region Wiring

    private static string ReadServerSource(params string[] path)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;

        root.Should().NotBeNull();
        return File.ReadAllText(Path.Combine(new[] { root!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray()));
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"{signature} should exist");
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException($"{signature} has no closing brace.");
    }

    [Test]
    public void ThePestPenalty_WalksFromThePlotToItsCityThroughThePropertyHierarchy()
    {
        var pests = ExtractMethod(ReadServerSource("Service", "Farming.cs"), "public static bool HasPests(WorldProperty plot)");

        pests.Should().Contain("Property.GetCityIdForProperty(plot.ParentPropertyId)");
        pests.Should().NotContain("parent.ParentPropertyId", "a house interior's parent is its building structure, not the city");
    }

    [Test]
    public void TheGardensSituation_FindsIndoorPlotsThroughTheBuildingStructure()
    {
        var types = ExtractMethod(ReadServerSource("Service", "CityAffairs.cs"), "private static IReadOnlyCollection<StructureType> GetStructureTypes(WorldProperty city)");

        types.Should().Contain("PropertyHierarchyRules.GetStructureTypes(");
        types.Should().NotContain("PropertyType.House", "houses are not the city's direct children");
    }

    [Test]
    public void TheCityIdLookup_IsOneWalk_SharedByTheAreaAndThePropertyLookups()
    {
        var property = ReadServerSource("Service", "Property.cs");

        ExtractMethod(property, "public static string GetCityIdForArea(uint area)").Should().Contain("GetCityIdForProperty(GetPropertyId(area))");
        ExtractMethod(property, "public static string GetCityIdForProperty(string propertyId)").Should().Contain("PropertyHierarchyRules.FindCityId(propertyId, DB.Get<WorldProperty>)");
        property.Split("public static string GetCityIdForProperty(").Length.Should().Be(2, "one pair, not two copies of the walk");
        property.Split("public static string GetCityIdForArea(").Length.Should().Be(2);
    }

    #endregion
}
