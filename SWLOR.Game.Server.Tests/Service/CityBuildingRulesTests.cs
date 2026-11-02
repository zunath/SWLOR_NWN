using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

[TestFixture]
public class CityBuildingRulesTests
{
    [TestCase(StructureType.CityHall)]
    [TestCase(StructureType.Starport)]
    [TestCase(StructureType.Cantina)]
    [TestCase(StructureType.Shipyard)]
    [TestCase(StructureType.AsteroidDrill)]
    [TestCase(StructureType.JumpBeacon)]
    public void BuildingsWithAnInteriorOrStationBuildings_CountAgainstTheBuildingLimit(StructureType structureType)
    {
        CityBuildingRules.CountsAsBuilding(structureType).Should().BeTrue();
    }

    [TestCase(StructureType.DuelingRing)]
    [TestCase(StructureType.MarketStall)]
    [TestCase(StructureType.Corral)]
    [TestCase(StructureType.GardenPlot)]
    public void OpenGroundCityStructures_CountAgainstTheStructureLimit(StructureType structureType)
    {
        CityBuildingRules.CountsAsBuilding(structureType).Should().BeFalse();
    }
}
