using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

public class FurnitureSetPlaceableTests
{
    private const string TrophyStandSuffix = "TrophyStand";

    private static string ResrefOf(StructureType type) =>
        type.GetAttribute<StructureType, StructureAttribute>().Resref;

    private static string[] SetNames() => Enum.GetValues<StructureType>()
        .Select(t => t.ToString())
        .Where(name => name.EndsWith(TrophyStandSuffix))
        .Select(name => name[..^TrophyStandSuffix.Length])
        .ToArray();

    [Test]
    public void EveryFurnitureSet_UsesADifferentPlaceableForEachPiece()
    {
        var sets = SetNames();
        sets.Should().HaveCount(10);
        foreach (var set in sets)
        {
            var pieces = Enum.GetValues<StructureType>().Where(t => t.ToString().StartsWith(set)).ToList();
            pieces.Should().HaveCount(10, $"{set} has ten furniture pieces");
            pieces.GroupBy(ResrefOf).Where(group => group.Count() > 1)
                .Should().BeEmpty($"no two {set} pieces should look identical");
        }
    }

    [Test]
    public void RenamedDisplayPieces_EachHaveTheirOwnPlaceable()
    {
        var renamed = FurnitureRenameMigration.RecipeRenames.Values
            .Select(recipe => Enum.Parse<StructureType>(recipe.ToString()))
            .ToList();

        renamed.Should().HaveCount(10);
        renamed.Select(ResrefOf).Should().OnlyHaveUniqueItems("each set's display piece has its own look");
        renamed.Select(ResrefOf).Should().NotContain(ResrefOf(StructureType.VeilcarvedTrophyStand));
    }
}
