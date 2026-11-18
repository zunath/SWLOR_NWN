using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.Formats.TwoDA;

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

    [Test]
    public void FieldCots_EachHaveTheirOwnPlaceable()
    {
        var cots = Enum.GetValues<StructureType>().Where(t => t.ToString().EndsWith("FieldCot")).ToList();
        var otherStructureResrefs = Enum.GetValues<StructureType>().Except(cots).Select(ResrefOf).ToHashSet();

        cots.Should().HaveCount(10);
        cots.Select(ResrefOf).Should().OnlyHaveUniqueItems("each set's Field Cot has its own look");
        cots.Select(ResrefOf).Should().NotContain(ResrefOf(StructureType.Cot),
            "the cots used to share the plain Cot's look");
        cots.Select(ResrefOf).Should().NotIntersectWith(otherStructureResrefs,
            "no Field Cot may look like any other craftable structure");
        cots.ToDictionary(type => type, ResrefOf).Should().BeEquivalentTo(FieldCotResrefs,
            "each cot was chosen for its set's theme, checked in the client");
    }

    [Test]
    public void FieldCots_ShowModelsNoOtherStructureShows()
    {
        var root = FindRoot();
        var placeables = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        string? ModelOf(StructureType type)
        {
            var path = Path.Combine(root, "Module", "utp", ResrefOf(type) + ".utp.json");
            if (!File.Exists(path))
                return null; // A base-game blueprint, such as the plain Cot's plc_cot.
            using var blueprint = JsonDocument.Parse(File.ReadAllText(path));
            var appearance = blueprint.RootElement.GetProperty("Appearance").GetProperty("value").GetInt32();
            return placeables.GetValue(appearance, "ModelName").ToLowerInvariant();
        }

        var cots = FieldCotResrefs.Keys.ToList();
        var cotModels = cots.Select(ModelOf).ToList();
        var otherModels = Enum.GetValues<StructureType>().Except(cots).Select(ModelOf).Where(model => model != null).ToHashSet();

        cotModels.Should().NotContainNulls("every Field Cot uses a module blueprint");
        cotModels.Should().OnlyHaveUniqueItems("two blueprints can show one model");
        cotModels.Should().NotIntersectWith(otherModels, "a different blueprint can still show another structure's model");
    }

    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    // Each set's Field Cot blueprint: a Meshy model (swc_cot_*, resref = model) or an existing bed no other structure uses.
    private static readonly Dictionary<StructureType, string> FieldCotResrefs = new()
    {
        [StructureType.AshmarkFieldCot] = "swc_cot_ash",
        [StructureType.GuttermarkFieldCot] = "swc_cot_gutter",
        [StructureType.TrailmarkFieldCot] = "_mdrn_pl_cotsimp",
        [StructureType.StonewakeFieldCot] = "swc_cot_stone",
        [StructureType.AegislineFieldCot] = "_mdrn_pl_bedsgrd",
        [StructureType.VeilcarvedFieldCot] = "swc_cot_veil",
        [StructureType.SurgewakeFieldCot] = "_mdrn_pl_bedmtl2",
        [StructureType.ResonantFieldCot] = "swc_cot_reson",
        [StructureType.FieldlineFieldCot] = "_mdrn_pl_bedenli",
        [StructureType.CommandlineFieldCot] = "swlor_0622",
    };

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
