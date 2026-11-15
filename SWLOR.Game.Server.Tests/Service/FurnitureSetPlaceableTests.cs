using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
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
        var renamed = DisplayPieceResrefs.Keys.ToList();

        renamed.Should().HaveCount(10);
        renamed.Select(ResrefOf).Should().OnlyHaveUniqueItems("each set's display piece has its own look");
        renamed.Select(ResrefOf).Should().NotContain(ResrefOf(StructureType.VeilcarvedTrophyStand));
        renamed.ToDictionary(type => type, ResrefOf).Should().BeEquivalentTo(DisplayPieceResrefs,
            "each name was chosen for the placeable it shows, checked in the client");
    }

    [Test]
    public void TrophyStands_EachHaveTheirOwnPlaceable()
    {
        var stands = Enum.GetValues<StructureType>().Where(t => t.ToString().EndsWith(TrophyStandSuffix)).ToList();

        stands.Should().HaveCount(10);
        stands.Select(ResrefOf).Should().OnlyHaveUniqueItems("each set's Trophy Stand has its own look");
        stands.Select(ResrefOf).Should().NotContain(ResrefOf(StructureType.Pedestal),
            "the stands used to share the plain Pedestal's look");
        stands.Select(ResrefOf).Should().NotIntersectWith(DisplayPieceResrefs.Values,
            "no Trophy Stand may look like any set's display piece");
        stands.ToDictionary(type => type, ResrefOf).Should().BeEquivalentTo(TrophyStandResrefs,
            "each stand has its own Meshy model for its set's theme, checked in the client");
    }

    // Each set's Trophy Stand blueprint; the blueprint resref is also its model and placeables.2da model name.
    private static readonly Dictionary<StructureType, string> TrophyStandResrefs = new()
    {
        [StructureType.AshmarkTrophyStand] = "swc_tro_ash",
        [StructureType.GuttermarkTrophyStand] = "swc_tro_gutter",
        [StructureType.TrailmarkTrophyStand] = "swc_tro_trail",
        [StructureType.StonewakeTrophyStand] = "swc_tro_stone",
        [StructureType.AegislineTrophyStand] = "swc_tro_aegis",
        [StructureType.VeilcarvedTrophyStand] = "swc_tro_veil",
        [StructureType.SurgewakeTrophyStand] = "swc_tro_surge",
        [StructureType.ResonantTrophyStand] = "swc_tro_reson",
        [StructureType.FieldlineTrophyStand] = "swc_tro_field",
        [StructureType.CommandlineTrophyStand] = "swc_tro_cmd",
    };

    // The placeable each renamed display piece was named after.
    private static readonly Dictionary<StructureType, string> DisplayPieceResrefs = new()
    {
        [StructureType.AshmarkAltar] = "_mdrn_pl_altarsi",
        [StructureType.GuttermarkEasel] = "x3_plc_stand001",
        [StructureType.TrailmarkWaystone] = "x0_runestone1",
        [StructureType.StonewakeStoneAltar] = "x0_altar",
        [StructureType.AegislineSwordPedestal] = "x3_plc_pedsword",
        [StructureType.VeilcarvedRunePillar] = "x0_runepillar",
        [StructureType.SurgewakeHoloProjector] = "_mdrn_pl_holobas",
        [StructureType.ResonantCrystal] = "x2_plc_crystal",
        [StructureType.FieldlineDisplayCase] = "zep_displaycase1",
        [StructureType.CommandlineBust] = "_mdrn_pl_bust",
    };
}
