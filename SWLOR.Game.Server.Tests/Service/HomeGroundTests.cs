using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityProjectService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.FarmingService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Home Ground (Player City Revamp, "Planets" and "Stations"): one completion time on the city that every bonus reads, the garden
/// harvest bonus, and a Wild Space station's drill settled up to that time before the 5% starts.
/// </summary>
public class HomeGroundTests
{
    private static readonly DateTime Start = new(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);

    #region One source of truth

    [Test]
    public void ACityWithoutTheProject_HasNoHomeGround()
    {
        CityProjectRules.IsHomeGroundBuilt(new WorldProperty(), Start).Should().BeFalse();
        CityProjectRules.IsHomeGroundBuilt(null, Start).Should().BeFalse();
    }

    [Test]
    public void HomeGround_BeginsAtItsCompletionTime()
    {
        var city = new WorldProperty { HomeGroundCompletedUtc = Start.AddDays(2) };

        CityProjectRules.IsHomeGroundBuilt(city, Start.AddDays(2).AddTicks(-1)).Should().BeFalse();
        CityProjectRules.IsHomeGroundBuilt(city, Start.AddDays(2)).Should().BeTrue();
    }

    [Test]
    public void TheHarvestChanceAndTheGardenBonus_ReadTheSameCityTime()
    {
        var edictEffects = ReadSource("Service", "EdictEffects.cs");
        var farming = ReadSource("Service", "Farming.cs");

        ExtractBody(edictEffects, "public static bool HasHomeGround(WorldProperty city)").Should().Contain("CityProjects.HasHomeGround(city)");
        ExtractBody(farming, "public static bool HasHomeGround(WorldProperty plot)").Should().Contain("CityProjects.HasHomeGround(");
        ExtractBody(ReadSource("Service", "CityProjects.cs"), "public static bool HasHomeGround(WorldProperty city)")
            .Should().Contain("CityProjectRules.IsHomeGroundBuilt(city");
    }

    [Test]
    public void AGardenInTheCity_YieldsOneMore_WhenHomeGroundIsBuilt()
    {
        var without = FarmingRules.GetYield(true, false, 0, false, false);
        var with = FarmingRules.GetYield(true, false, 0, false, true);

        (with - without).Should().Be(1);
        FarmingRules.GetYield(true, false, 0, true, true).Should().Be(without + 2, "a Greenhouse and Home Ground each add 1");
    }

    [Test]
    public void ThePlotInAHouse_FindsItsCityThroughTheBuilding()
    {
        var property = ReadSource("Service", "Property.cs");

        // plot -> house interior -> building structure -> city
        ExtractBody(property, "public static string GetCityIdForProperty(string propertyId)").Should().Contain("property.ParentPropertyId");
        ExtractBody(ReadSource("Service", "Farming.cs"), "public static bool HasHomeGround(WorldProperty plot)")
            .Should().Contain("Property.GetCityIdForProperty(plot.ParentPropertyId)");
    }

    #endregion

    #region Stations

    [Test]
    public void OnlyAWildSpaceStation_GetsTheDrillBonus_AfterItsCompletionTime()
    {
        StationBuildingRules.GetsDrillHomeGround(true, Start, Start).Should().BeTrue();
        StationBuildingRules.GetsDrillHomeGround(true, Start, Start.AddTicks(-1)).Should().BeFalse();
        StationBuildingRules.GetsDrillHomeGround(true, null, Start).Should().BeFalse();
        StationBuildingRules.GetsDrillHomeGround(false, Start, Start.AddDays(9)).Should().BeFalse("an orbit station gets the planet harvesting bonus instead");
    }

    [Test]
    public void TheDrill_MinesFivePercentFaster_WithHomeGround()
    {
        var without = StationBuildingRules.DrillUnitsPerDay(0, homeGround: false);
        var with = StationBuildingRules.DrillUnitsPerDay(0, homeGround: true);

        with.Should().BeApproximately(without * 1.05, 0.0001);
        StationBuildingRules.HomeGroundDrillPercent.Should().Be(5);
    }

    [Test]
    public void TheDrill_StacksTheDroidAndHomeGround()
    {
        StationBuildingRules.DrillUnitsPerDay(50, homeGround: true)
            .Should().BeApproximately(StationBuildingRules.DrillUnitsPerDay(50) * 1.05, 0.0001);
    }

    [Test]
    public void HomeGround_DoesNotMakeTheDrillStoreMore()
    {
        StationBuildingRules.DrillCapacity(0).Should().Be(StationTuning.DrillUnitsPerDay * StationTuning.DrillStorageDays);
    }

    [Test]
    public void SettlingTheDrillFirst_KeepsTheEarlierDaysAtTheOldRate()
    {
        var completion = Start.AddDays(1);
        var now = Start.AddDays(2);

        // The order the service uses: settle up to the completion time at the old rate, then count the rest with the bonus.
        var (before, afterSettle) = StationBuildingRules.AccrueDrill(0, Start, completion, 0, homeGround: false);
        var (after, _) = StationBuildingRules.AccrueDrill(before, afterSettle, now, 0, homeGround: true);

        // Counting the whole span at the new rate would give the earlier day the bonus too.
        var (wholeSpan, _) = StationBuildingRules.AccrueDrill(0, Start, now, 0, homeGround: true);
        var (baseOnly, _) = StationBuildingRules.AccrueDrill(0, Start, now, 0, homeGround: false);

        before.Should().BeInRange(StationTuning.DrillUnitsPerDay - 1, StationTuning.DrillUnitsPerDay, "the first day mined at the base rate");
        (before + after).Should().BeLessThan(wholeSpan);
        (before + after).Should().BeGreaterThan(baseOnly);
        after.Should().BeInRange(156, 158, "the second day mined 5% faster");
    }

    [Test]
    public void ADrillSettledAtTheCompletionTime_StartsItsNextUnitFromThere()
    {
        var completion = Start.AddHours(12);

        var (units, next) = StationBuildingRules.AccrueDrill(0, Start, completion, 0);

        units.Should().BeInRange(StationTuning.DrillUnitsPerDay / 2 - 1, StationTuning.DrillUnitsPerDay / 2);
        Math.Abs((completion - next).TotalHours).Should().BeLessThan(24.0 / StationTuning.DrillUnitsPerDay, "less than one unit's time is carried over");
    }

    [Test]
    public void SettlingTwice_AfterARestart_AddsNothingTheSecondTime()
    {
        var completion = Start.AddDays(1);
        var (_, next) = StationBuildingRules.AccrueDrill(0, Start, completion, 0);

        var (again, _) = StationBuildingRules.AccrueDrill(150, next, completion, 0);

        again.Should().Be(0);
    }

    [Test]
    public void TheStationService_SettlesTheDrillBeforeHomeGroundIsSet_AndNotAfterItWasReadPastThatTime()
    {
        var projects = ReadSource("Service", "CityProjects.cs");
        var finish = ExtractBody(projects, "private static void FinishComplete(CityProject project, WorldProperty city)");

        finish.IndexOf("StationBuildings.SettleDrills(", StringComparison.Ordinal)
            .Should().BeGreaterThan(-1)
            .And.BeLessThan(finish.IndexOf("CityProjectRules.ApplyCompletion(", StringComparison.Ordinal), "settle first, then start the bonus");
        finish.Should().Contain("Edict.IsWildSpaceStation(city)");

        var settle = ExtractBody(ReadSource("Service", "StationBuildings.cs"), "public static void SettleDrills(WorldProperty station, DateTime completedUtc)");
        settle.Should().Contain("state.DrillLastAccrual < completedUtc", "a drill already read past that moment is left alone");
        settle.Should().Contain("StructureType.AsteroidDrill");
    }

    [Test]
    public void TheDrill_AppliesItsBonusFromTheStationsOwnCompletionTime()
    {
        var accrue = ExtractBody(ReadSource("Service", "StationBuildings.cs"), "public static void AccrueDrill(WorldProperty station, StationBuildingState state, DateTime nowUtc)");

        accrue.Should().Contain("StationBuildingRules.GetsDrillHomeGround(anchor.IsWildSpace, station.HomeGroundCompletedUtc, nowUtc)");
    }

    #endregion

    private static string ReadSource(params string[] path)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;

        root.Should().NotBeNull();
        return File.ReadAllText(Path.Combine(new[] { root!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray()));
    }

    private static string ExtractBody(string source, string signature)
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
}
