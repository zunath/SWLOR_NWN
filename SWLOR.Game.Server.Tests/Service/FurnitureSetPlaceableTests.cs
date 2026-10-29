using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

public class FurnitureSetPlaceableTests
{
    private const string TrophyStandSuffix = "TrophyStand";
    private const string DisplayPlinthSuffix = "DisplayPlinth";

    private static string ResrefOf(StructureType type) =>
        type.GetAttribute<StructureType, StructureAttribute>().Resref;

    [Test]
    public void EveryFurnitureSet_UsesDifferentPlaceablesForTrophyStandAndDisplayPlinth()
    {
        var plinths = Enum.GetValues<StructureType>()
            .Where(t => t.ToString().EndsWith(DisplayPlinthSuffix))
            .ToList();

        plinths.Should().NotBeEmpty();
        foreach (var plinth in plinths)
        {
            var setName = plinth.ToString()[..^DisplayPlinthSuffix.Length];
            var stand = Enum.Parse<StructureType>(setName + TrophyStandSuffix);

            ResrefOf(plinth).Should().NotBe(ResrefOf(stand),
                $"{setName} Trophy Stand and Display Plinth must not look identical");
        }
    }

    [Test]
    public void EveryFurnitureSet_HasItsOwnDisplayPlinthPlaceable()
    {
        var plinths = Enum.GetValues<StructureType>()
            .Where(t => t.ToString().EndsWith(DisplayPlinthSuffix))
            .ToList();

        plinths.Select(ResrefOf).Should().OnlyHaveUniqueItems(
            "each furniture set's Display Plinth should have its own look");
    }
}
