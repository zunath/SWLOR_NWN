using System;
using System.IO;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The city level a mayor and the citizen count support, as the weekly upkeep check applies it and the election window's
/// City Management warning.
/// </summary>
public class CityLevelRulesTests
{
    // The citizens each level needs, as Property keeps them.
    private static readonly Dictionary<int, int> Required = new()
    {
        { 1, 5 }, { 2, 10 }, { 3, 15 }, { 4, 20 }, { 5, 25 }
    };

    /// <summary>The weekly check as it was written before the calculation moved here.</summary>
    private static int OriginalLevelAfterDrop(int currentLevel, int? cityManagementRank, long citizenCount)
    {
        var mayorLevel = cityManagementRank.HasValue ? cityManagementRank.Value + 1 : 1;
        if (mayorLevel < currentLevel)
            currentLevel = mayorLevel;

        var maxLevelThisCycle = 1;
        for (var level = 1; level <= 5; level++)
        {
            if (level > mayorLevel)
                break;

            if (citizenCount >= Required[level])
                maxLevelThisCycle = level;
        }

        if (currentLevel > maxLevelThisCycle)
            currentLevel = maxLevelThisCycle;

        return currentLevel;
    }

    [TestCase(null, 1)]
    [TestCase(0, 1)]
    [TestCase(1, 2)]
    [TestCase(4, 5)]
    public void TheMayorLevelIsOneMoreThanTheCityManagementRank(int? rank, int expected)
    {
        CityLevelRules.GetMayorLevel(rank).Should().Be(expected);
    }

    [TestCase(5, 0, 1)]
    [TestCase(5, 4, 1)]
    [TestCase(5, 5, 1)]
    [TestCase(5, 10, 2)]
    [TestCase(5, 17, 3)]
    [TestCase(5, 20, 4)]
    [TestCase(5, 99, 5)]
    [TestCase(2, 99, 2)]
    [TestCase(1, 99, 1)]
    [TestCase(3, 12, 2)]
    public void TheMaxLevelNeedsBothTheMayorAndTheCitizens(int mayorLevel, int citizens, int expected)
    {
        CityLevelRules.GetMaxSupportedLevel(mayorLevel, citizens, Required).Should().Be(expected);
    }

    [Test]
    public void TheLevelAfterTheCheckMatchesTheOriginalCalculationForEveryCombination()
    {
        foreach (var currentLevel in new[] { 1, 2, 3, 4, 5 })
        {
            foreach (var rank in new int?[] { null, 0, 1, 2, 3, 4 })
            {
                for (var citizens = 0; citizens <= 30; citizens++)
                {
                    var mayorLevel = CityLevelRules.GetMayorLevel(rank);
                    var maxLevel = CityLevelRules.GetMaxSupportedLevel(mayorLevel, citizens, Required);

                    CityLevelRules.GetLevelAfterDrop(currentLevel, mayorLevel, maxLevel)
                        .Should().Be(OriginalLevelAfterDrop(currentLevel, rank, citizens), $"level {currentLevel}, rank {rank}, {citizens} citizens");
                }
            }
        }
    }

    [Test]
    public void TheCheckNeverRaisesTheLevel()
    {
        CityLevelRules.GetLevelAfterDrop(2, 5, 5).Should().Be(2);
    }

    [Test]
    public void PropertyKeepsTheCitizenRequirementsTheseRulesAreTestedWith()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server", "Service", "Property.cs")))
        {
            directory = directory.Parent;
        }

        directory.Should().NotBeNull();
        var source = File.ReadAllText(Path.Combine(directory.FullName, "SWLOR.Game.Server", "Service", "Property.cs"));
        var start = source.IndexOf("_citizensRequired = new()", StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0);
        var table = source.Substring(start, source.IndexOf("};", start, StringComparison.Ordinal) - start);

        foreach (var (level, citizens) in Required)
        {
            table.Should().Contain($"{{ {level}, {citizens} }}");
        }
    }
}
