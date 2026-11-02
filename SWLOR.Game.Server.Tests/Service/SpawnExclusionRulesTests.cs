using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Service.SpawnService;

namespace SWLOR.Game.Server.Tests.Service;

public class SpawnExclusionRulesTests
{
    private static Func<Vector3> Sequence(params Vector3[] candidates)
    {
        var queue = new Queue<Vector3>(candidates);
        return () => queue.Dequeue();
    }

    private static SpawnExclusionZone BuildingAt(float x, float y)
    {
        return new SpawnExclusionZone(new List<Vector2> { new(x, y) }, null, 0f);
    }

    private static SpawnExclusionZone CityHallAt(float x, float y, int cityLevel, float shorterSideMeters)
    {
        var radius = SpawnExclusionRules.GetSettledRadius(cityLevel, shorterSideMeters);
        return new SpawnExclusionZone(new List<Vector2>(), new Vector2(x, y), radius);
    }

    // The buffer around buildings

    [Test]
    public void IsLegal_InsideTheBuffer_IsIllegal()
    {
        var zone = BuildingAt(100, 100);

        SpawnExclusionRules.IsLegal(zone, new Vector3(100, 100, 0)).Should().BeFalse();
        SpawnExclusionRules.IsLegal(zone, new Vector3(114.9f, 100, 0)).Should().BeFalse();
        SpawnExclusionRules.IsLegal(zone, new Vector3(100, 85.1f, 0)).Should().BeFalse();
    }

    [Test]
    public void IsLegal_ExactlyAtTheBufferEdge_IsLegal()
    {
        var zone = BuildingAt(100, 100);

        SpawnExclusionRules.IsLegal(zone, new Vector3(115, 100, 0)).Should().BeTrue();
        SpawnExclusionRules.IsLegal(zone, new Vector3(100, 85, 0)).Should().BeTrue();
        SpawnExclusionRules.IsLegal(zone, new Vector3(115.1f, 100, 0)).Should().BeTrue();
    }

    [Test]
    public void IsLegal_BufferIsFifteenMeters()
    {
        SpawnExclusionRules.BuildingBufferMeters.Should().Be(15f);
    }

    [Test]
    public void IsLegal_MeasuresHorizontalDistanceOnly()
    {
        var zone = BuildingAt(100, 100);

        SpawnExclusionRules.IsLegal(zone, new Vector3(105, 100, 40)).Should().BeFalse();
        SpawnExclusionRules.IsLegal(zone, new Vector3(120, 100, 0)).Should().BeTrue();
    }

    [Test]
    public void IsLegal_MeasuresAcrossTheDiagonal()
    {
        var zone = BuildingAt(100, 100);

        // 10 m on each axis is 14.14 m away: inside. 11 m on each axis is 15.56 m away: outside.
        SpawnExclusionRules.IsLegal(zone, new Vector3(110, 110, 0)).Should().BeFalse();
        SpawnExclusionRules.IsLegal(zone, new Vector3(111, 111, 0)).Should().BeTrue();
    }

    [Test]
    public void IsLegal_AnyStructureCanExcludeAPosition()
    {
        var zone = new SpawnExclusionZone(
            new List<Vector2> { new(10, 10), new(200, 200), new(300, 50) },
            null,
            0f);

        SpawnExclusionRules.IsLegal(zone, new Vector3(205, 195, 0)).Should().BeFalse();
        SpawnExclusionRules.IsLegal(zone, new Vector3(100, 100, 0)).Should().BeTrue();
    }

    [Test]
    public void IsLegal_WithNoZone_AnyPositionIsLegal()
    {
        SpawnExclusionRules.IsLegal(SpawnExclusionZone.None, new Vector3(0, 0, 0)).Should().BeTrue();
        SpawnExclusionRules.IsLegal(SpawnExclusionZone.None, new Vector3(160, 160, 0)).Should().BeTrue();
    }

    // The Settled zone

    [TestCase(1, 10f)]
    [TestCase(2, 20f)]
    [TestCase(3, 30f)]
    [TestCase(4, 40f)]
    [TestCase(5, 50f)]
    public void GetSettledRadius_IsTenMetersPerLevelInALargeArea(int level, float expected)
    {
        // 21 of the 29 areas open to cities are 32 tiles across.
        var shorterSide = SpawnExclusionRules.GetShorterSideMeters(32, 32);

        SpawnExclusionRules.GetSettledRadius(level, shorterSide).Should().Be(expected);
    }

    [TestCase(1, 10f)]
    [TestCase(2, 20f)]
    [TestCase(3, 25f)]
    [TestCase(4, 25f)]
    [TestCase(5, 25f)]
    public void GetSettledRadius_AHundredMeterSide_CapsAtAQuarter(int level, float expected)
    {
        var shorterSide = SpawnExclusionRules.GetShorterSideMeters(10, 16);

        shorterSide.Should().Be(100f);
        SpawnExclusionRules.GetSettledRadius(level, shorterSide).Should().Be(expected);
    }

    [TestCase(1, 10f)]
    [TestCase(2, 20f)]
    [TestCase(3, 22.5f)]
    [TestCase(5, 22.5f)]
    public void GetSettledRadius_ANinetyMeterSide_CapsAtAQuarter(int level, float expected)
    {
        var shorterSide = SpawnExclusionRules.GetShorterSideMeters(9, 16);

        shorterSide.Should().Be(90f);
        SpawnExclusionRules.GetSettledRadius(level, shorterSide).Should().Be(expected);
    }

    [Test]
    public void GetShorterSideMeters_UsesTheShorterDimensionInTenMeterTiles()
    {
        SpawnExclusionRules.GetShorterSideMeters(16, 10).Should().Be(100f);
        SpawnExclusionRules.GetShorterSideMeters(10, 16).Should().Be(100f);
        SpawnExclusionRules.GetShorterSideMeters(24, 24).Should().Be(240f);
    }

    [Test]
    public void GetSettledRadius_AMetropolisOnALargeAreaCoversUnderEightPercent()
    {
        var shorterSide = SpawnExclusionRules.GetShorterSideMeters(32, 32);
        var radius = SpawnExclusionRules.GetSettledRadius(5, shorterSide);

        var covered = MathF.PI * radius * radius / (shorterSide * shorterSide);

        covered.Should().BeLessThan(0.08f);
    }

    [Test]
    public void GetSettledRadius_NeverCoversMoreThanAFifthOfAnyArea()
    {
        foreach (var tiles in new[] { 9, 10, 12, 16, 24, 32 })
        {
            var shorterSide = SpawnExclusionRules.GetShorterSideMeters(tiles, tiles);
            var radius = SpawnExclusionRules.GetSettledRadius(5, shorterSide);

            (MathF.PI * radius * radius / (shorterSide * shorterSide)).Should().BeLessThan(0.2f);
        }
    }

    [Test]
    public void GetSettledRadius_ALevelBelowOne_HasNoZone()
    {
        SpawnExclusionRules.GetSettledRadius(0, 320f).Should().Be(0f);
        SpawnExclusionRules.GetSettledRadius(-3, 320f).Should().Be(0f);
    }

    [Test]
    public void IsLegal_InsideTheSettledZone_IsIllegal()
    {
        var zone = CityHallAt(160, 160, 3, 320f);

        SpawnExclusionRules.IsLegal(zone, new Vector3(160, 160, 0)).Should().BeFalse();
        SpawnExclusionRules.IsLegal(zone, new Vector3(189.9f, 160, 0)).Should().BeFalse();
    }

    [Test]
    public void IsLegal_ExactlyAtTheSettledEdge_IsLegal()
    {
        var zone = CityHallAt(160, 160, 3, 320f);

        SpawnExclusionRules.IsLegal(zone, new Vector3(190, 160, 0)).Should().BeTrue();
        SpawnExclusionRules.IsLegal(zone, new Vector3(160, 130, 0)).Should().BeTrue();
    }

    [Test]
    public void IsLegal_TheSettledZoneGrowsWithTheCityLevel()
    {
        var position = new Vector3(185, 160, 0);

        SpawnExclusionRules.IsLegal(CityHallAt(160, 160, 2, 320f), position).Should().BeTrue();
        SpawnExclusionRules.IsLegal(CityHallAt(160, 160, 3, 320f), position).Should().BeFalse();
    }

    [Test]
    public void IsLegal_TheSettledZoneShrinksToTheAreaCap()
    {
        // A level 5 city on a 100 m side keeps 25 m clear, not 50.
        var zone = CityHallAt(50, 50, 5, 100f);

        SpawnExclusionRules.IsLegal(zone, new Vector3(70, 50, 0)).Should().BeFalse();
        SpawnExclusionRules.IsLegal(zone, new Vector3(75, 50, 0)).Should().BeTrue();
    }

    [Test]
    public void IsLegal_BufferAndSettledZoneBothApply()
    {
        var zone = new SpawnExclusionZone(new List<Vector2> { new(60, 160) }, new Vector2(160, 160), 20f);

        SpawnExclusionRules.IsLegal(zone, new Vector3(170, 160, 0)).Should().BeFalse("it is inside the Settled zone");
        SpawnExclusionRules.IsLegal(zone, new Vector3(70, 160, 0)).Should().BeFalse("it is inside a building's buffer");
        SpawnExclusionRules.IsLegal(zone, new Vector3(110, 160, 0)).Should().BeTrue();
    }

    // Which structures count

    [TestCase(StructureType.CityHall)]
    [TestCase(StructureType.Bank)]
    [TestCase(StructureType.Cantina)]
    [TestCase(StructureType.MedicalCenter)]
    [TestCase(StructureType.Starport)]
    [TestCase(StructureType.LabStyle1)]
    [TestCase(StructureType.SmallHouseStyle1)]
    [TestCase(StructureType.MediumHouseStyle3)]
    [TestCase(StructureType.LargeHouseStyle4)]
    public void IsQualifyingStructure_BuildingsAndHouses_Count(StructureType type)
    {
        SpawnExclusionRules.IsQualifyingStructure(type).Should().BeTrue();
    }

    [TestCase(StructureType.Invalid)]
    [TestCase(StructureType.Campfire)]
    [TestCase(StructureType.Fountain)]
    [TestCase(StructureType.LampPost)]
    [TestCase(StructureType.MiningWell)]
    [TestCase(StructureType.Jukebox)]
    [TestCase(StructureType.StationCore)]
    [TestCase(StructureType.Shipyard)]
    [TestCase(StructureType.JumpBeacon)]
    public void IsQualifyingStructure_DecorationsAndOtherStructures_DoNotCount(StructureType type)
    {
        SpawnExclusionRules.IsQualifyingStructure(type).Should().BeFalse();
    }

    [Test]
    public void QualifyingStructures_IncludeEveryHouse()
    {
        var houses = Enum.GetValues<StructureType>()
            .Where(t => t.ToString().Contains("House", StringComparison.Ordinal) && (int)t >= 5000)
            .ToList();

        houses.Should().HaveCount(12);
        houses.Should().OnlyContain(t => SpawnExclusionRules.IsQualifyingStructure(t));
    }

    [Test]
    public void QualifyingStructures_IncludeEveryCityBuilding()
    {
        // A building is a structure restricted to cities that has an interior layout. A station core stands on a
        // station deck, which has no hostile spawns. A new building must be added to SpawnExclusionRules.
        var buildings = Enum.GetValues<StructureType>()
            .Where(t => t != StructureType.StationCore)
            .Select(t => (Type: t, Attribute: t.GetAttribute<StructureType, StructureAttribute>()))
            .Where(x => x.Attribute.RestrictedPropertyTypes == PropertyType.City &&
                        x.Attribute.LayoutType != PropertyLayoutType.Invalid)
            .Select(x => x.Type)
            .ToList();

        buildings.Should().NotBeEmpty();
        buildings.Where(t => !SpawnExclusionRules.IsQualifyingStructure(t)).Should().BeEmpty();
    }

    [Test]
    public void QualifyingStructures_AreAllBuildings()
    {
        // Decorations carry no interior layout and are not restricted to cities, so none may sneak into the set.
        foreach (var type in SpawnExclusionRules.QualifyingStructures)
        {
            var attribute = type.GetAttribute<StructureType, StructureAttribute>();

            attribute.LayoutType.Should().NotBe(PropertyLayoutType.Invalid, $"{type} must be a building");
            attribute.RestrictedPropertyTypes.Should().Be(PropertyType.City, $"{type} must be a city structure");
        }
    }

    // Picking a legal position

    [Test]
    public void TryPickLegalPosition_TakesTheFirstLegalCandidate()
    {
        var zone = BuildingAt(100, 100);

        var picked = SpawnExclusionRules.TryPickLegalPosition(
            zone,
            Sequence(new Vector3(105, 100, 0), new Vector3(110, 100, 0), new Vector3(200, 100, 0), new Vector3(300, 100, 0)),
            10,
            out var position);

        picked.Should().BeTrue();
        position.Should().Be(new Vector3(200, 100, 0));
    }

    [Test]
    public void TryPickLegalPosition_WhenEveryCandidateIsIllegal_ReturnsFalse()
    {
        var zone = BuildingAt(100, 100);
        var calls = 0;

        var picked = SpawnExclusionRules.TryPickLegalPosition(
            zone,
            () =>
            {
                calls++;
                return new Vector3(101, 100, 0);
            },
            SpawnSpacing.MaximumAttempts,
            out var position);

        picked.Should().BeFalse();
        position.Should().Be(Vector3.Zero);
        calls.Should().Be(SpawnSpacing.MaximumAttempts);
    }

    [Test]
    public void TryPickLegalPosition_StopsDrawingAtTheFirstLegalCandidate()
    {
        var calls = 0;

        SpawnExclusionRules.TryPickLegalPosition(
            BuildingAt(100, 100),
            () =>
            {
                calls++;
                return new Vector3(300, 300, 0);
            },
            10,
            out _);

        calls.Should().Be(1);
    }

    [Test]
    public void TryPickLegalPosition_StopsAfterTheGivenAttempts()
    {
        var calls = 0;

        SpawnExclusionRules.TryPickLegalPosition(
            BuildingAt(100, 100),
            () =>
            {
                calls++;
                return new Vector3(100, 100, 0);
            },
            3,
            out _);

        calls.Should().Be(3);
    }

    [Test]
    public void TryPickLegalPosition_DrawsAtLeastOnce()
    {
        var picked = SpawnExclusionRules.TryPickLegalPosition(
            SpawnExclusionZone.None,
            Sequence(new Vector3(7, 8, 0)),
            0,
            out var position);

        picked.Should().BeTrue();
        position.Should().Be(new Vector3(7, 8, 0));
    }

    [Test]
    public void TryPickLegalPosition_WithNoZone_TakesTheFirstCandidate()
    {
        var picked = SpawnExclusionRules.TryPickLegalPosition(
            SpawnExclusionZone.None,
            Sequence(new Vector3(1, 1, 0), new Vector3(2, 2, 0)),
            10,
            out var position);

        picked.Should().BeTrue();
        position.Should().Be(new Vector3(1, 1, 0));
    }

    [Test]
    public void TryPickLegalPosition_Placeable_SkipsALegalCandidateWithoutRoom()
    {
        var zone = BuildingAt(100, 100);
        var occupied = new List<Vector3> { new(200, 200, 0) };

        SpawnExclusionRules.TryPickLegalPosition(
            zone,
            Sequence(new Vector3(202, 200, 0), new Vector3(105, 100, 0), new Vector3(260, 200, 0)),
            10,
            out var position,
            occupied);

        position.Should().Be(new Vector3(260, 200, 0));
    }

    [Test]
    public void TryPickLegalPosition_Placeable_WhenNoLegalCandidateHasRoom_UsesTheMostOpenLegalOne()
    {
        var zone = BuildingAt(100, 100);
        var occupied = new List<Vector3> { new(200, 200, 0), new(206, 200, 0) };

        // The candidate at (103, 100) is the most open of all but sits in the building's buffer, so it is not eligible.
        var picked = SpawnExclusionRules.TryPickLegalPosition(
            zone,
            Sequence(new Vector3(201, 200, 0), new Vector3(103, 100, 0), new Vector3(203, 200, 0), new Vector3(205, 200, 0)),
            4,
            out var position,
            occupied);

        picked.Should().BeTrue();
        position.Should().Be(new Vector3(203, 200, 0));
    }

    [Test]
    public void TryPickLegalPosition_Placeable_WhenEveryCandidateIsIllegal_ReturnsFalse()
    {
        var zone = BuildingAt(100, 100);
        var occupied = new List<Vector3> { new(300, 300, 0) };

        var picked = SpawnExclusionRules.TryPickLegalPosition(
            zone,
            Sequence(new Vector3(100, 100, 0), new Vector3(101, 101, 0)),
            2,
            out _,
            occupied);

        picked.Should().BeFalse();
    }

    [Test]
    public void TryPickLegalPosition_Placeable_WithNoZone_MatchesTheExistingSpacing()
    {
        var occupied = new List<Vector3> { new(20, 20, 0), new(26, 20, 0) };
        var candidates = new[] { new Vector3(21, 20, 0), new Vector3(23, 20, 0), new Vector3(25, 20, 0) };

        var expected = SpawnSpacing.PickSpacedPosition(Sequence(candidates), occupied, maximumAttempts: 3);
        SpawnExclusionRules.TryPickLegalPosition(
            SpawnExclusionZone.None,
            Sequence(candidates),
            3,
            out var position,
            occupied);

        position.Should().Be(expected);
    }

    // Quest-critical spawns

    [Test]
    public void GetQuestCriticalPosition_OutsideTheZone_StaysPut()
    {
        var zone = BuildingAt(100, 100);
        var original = new Vector3(200, 200, 3);

        var position = SpawnExclusionRules.GetQuestCriticalPosition(zone, original, new[] { new Vector3(210, 200, 0) });

        position.Should().Be(original);
    }

    [Test]
    public void GetQuestCriticalPosition_InsideTheZone_MovesToTheNearestLegalWalkablePoint()
    {
        var zone = BuildingAt(100, 100);
        var original = new Vector3(105, 100, 0);
        var walkable = new[]
        {
            new Vector3(110, 100, 1),   // inside the buffer
            new Vector3(130, 100, 2),   // legal, 25 m away
            new Vector3(120, 100, 4),   // legal, 15 m away and the nearest
            new Vector3(100, 140, 5),   // legal, 40 m away
        };

        var position = SpawnExclusionRules.GetQuestCriticalPosition(zone, original, walkable);

        position.Should().Be(new Vector3(120, 100, 4));
    }

    [Test]
    public void GetQuestCriticalPosition_AcceptsAPointExactlyAtTheSearchLimit()
    {
        var zone = BuildingAt(100, 100);
        var original = new Vector3(100, 100, 0);

        var position = SpawnExclusionRules.GetQuestCriticalPosition(zone, original, new[] { new Vector3(100, 140, 0) });

        // 40 m from the original, but the building buffer reaches only 15 m.
        position.Should().Be(new Vector3(100, 140, 0));
    }

    [Test]
    public void GetQuestCriticalPosition_IgnoresPointsBeyondTheSearchLimit()
    {
        var zone = BuildingAt(100, 100);
        var original = new Vector3(100, 100, 0);

        var position = SpawnExclusionRules.GetQuestCriticalPosition(zone, original, new[] { new Vector3(100, 140.5f, 0) });

        position.Should().Be(original);
    }

    [Test]
    public void GetQuestCriticalPosition_WhenNoWalkablePointIsLegal_KeepsTheOriginalPoint()
    {
        var zone = BuildingAt(100, 100);
        var original = new Vector3(100, 100, 7);
        var walkable = new[] { new Vector3(105, 100, 0), new Vector3(90, 100, 0), new Vector3(100, 110, 0) };

        var position = SpawnExclusionRules.GetQuestCriticalPosition(zone, original, walkable);

        position.Should().Be(original);
    }

    [Test]
    public void GetQuestCriticalPosition_WithNoWalkablePoints_KeepsTheOriginalPoint()
    {
        var zone = BuildingAt(100, 100);
        var original = new Vector3(100, 100, 7);

        SpawnExclusionRules.GetQuestCriticalPosition(zone, original, new List<Vector3>()).Should().Be(original);
    }

    [Test]
    public void GetQuestCriticalPosition_AlsoLeavesTheSettledZone()
    {
        var zone = CityHallAt(160, 160, 3, 320f);
        var original = new Vector3(165, 160, 0);
        var walkable = new[] { new Vector3(170, 160, 0), new Vector3(192, 160, 0), new Vector3(200, 160, 0) };

        var position = SpawnExclusionRules.GetQuestCriticalPosition(zone, original, walkable);

        position.Should().Be(new Vector3(192, 160, 0));
    }

    [Test]
    public void GetQuestCriticalPosition_SearchLimitIsFortyMeters()
    {
        SpawnExclusionRules.QuestCriticalSearchMeters.Should().Be(40f);
    }
}
