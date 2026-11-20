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

    // Template pieces that every set once showed with one shared model, which a plain craftable also used.
    private static readonly string[] OwnLookPieces =
        { "FieldCot", "SignalLamp", "LowTable", "WallBanner", "SupplyLocker", "FloorMat", "DataConsole", "WorkStool" };

    private static List<StructureType> PiecesOf(string piece) =>
        Enum.GetValues<StructureType>().Where(t => t.ToString().EndsWith(piece)).ToList();

    [TestCaseSource(nameof(OwnLookPieces))]
    public void SetPieces_EachHaveTheirOwnPlaceable(string piece)
    {
        var pieces = PiecesOf(piece);
        var otherStructureResrefs = Enum.GetValues<StructureType>().Except(pieces).Select(ResrefOf).ToHashSet();

        pieces.Should().HaveCount(10);
        pieces.Select(ResrefOf).Should().OnlyHaveUniqueItems($"each set's {piece} has its own look");
        pieces.Select(ResrefOf).Should().NotIntersectWith(otherStructureResrefs,
            $"no {piece} may look like any other craftable structure, such as the plain one the sets used to share");
        pieces.ToDictionary(type => type, ResrefOf).Should().BeEquivalentTo(
            SetPieceResrefs.Where(entry => pieces.Contains(entry.Key)).ToDictionary(entry => entry.Key, entry => entry.Value),
            "each piece was chosen for its set's theme, checked in the client");
    }

    [TestCaseSource(nameof(OwnLookPieces))]
    public void SetPieces_ShowModelsNoOtherStructureShows(string piece) =>
        AssertShowModelsNoOtherStructureShows(PiecesOf(piece), piece);

    [Test]
    public void DisplayPieces_ShowModelsNoOtherStructureShows() =>
        AssertShowModelsNoOtherStructureShows(DisplayPieceResrefs.Keys.ToList(), "display piece");

    // Each piece shows its own placeables.2da model, which no other structure shows through any blueprint.
    private static void AssertShowModelsNoOtherStructureShows(List<StructureType> pieces, string label)
    {
        var root = FindRoot();
        var placeables = TwoDAReader.Read(Path.Combine(root, "SWLOR_Haks", "sw_2da", "placeables.2da"));
        var baseGame = ReadBaseGameFixture(root);
        string? ModelOf(StructureType type)
        {
            var resref = ResrefOf(type).Trim();
            if (resref.Length == 0)
                return null; // StructureType.Invalid
            int appearance;
            using (var blueprint = ReadBlueprint(root, resref))
            {
                if (blueprint != null)
                    appearance = blueprint.RootElement.GetProperty("Appearance").GetProperty("value").GetInt32();
                else if (baseGame.TryGetValue(resref, out var baseAppearance))
                    appearance = baseAppearance; // A base-game blueprint, such as the plain Footstool's plc_footstool.
                else
                    throw new AssertionException(
                        $"{type} uses {resref}, which is neither a module blueprint nor in {BaseGameFixtureName}; add its appearance row");
            }
            return placeables.GetValue(appearance, "ModelName").ToLowerInvariant();
        }

        var pieceModels = pieces.Select(ModelOf).ToList();
        var otherModels = Enum.GetValues<StructureType>().Except(pieces).Select(ModelOf).Where(model => model != null).ToHashSet();

        pieceModels.Should().NotContainNulls($"every {label} resolves to a model");
        pieceModels.Should().OnlyHaveUniqueItems("two blueprints can show one model");
        pieceModels.Should().NotIntersectWith(otherModels, "a different blueprint can still show another structure's model");
    }

    [Test]
    public void WorkStools_CanBeSatOn()
    {
        var root = FindRoot();
        foreach (var stool in PiecesOf("WorkStool"))
        {
            using var document = ReadBlueprint(root, ResrefOf(stool));
            document.Should().NotBeNull($"{stool} uses a module blueprint");
            var blueprint = document!.RootElement;
            blueprint.GetProperty("Useable").GetProperty("value").GetInt32().Should().Be(1, $"{stool} must stay usable to sit on");
            blueprint.GetProperty("OnUsed").GetProperty("value").GetString().Should().BeOneOf(
                new[] { "x2_plc_used_sit", "zep_use_chair", "x0_o2_use_chair" }, $"{stool} sits its user when used");
        }
    }

    [Test]
    public void BaseGameFixture_CoversEveryBaseGameStructureBlueprint()
    {
        var root = FindRoot();
        var fixture = ReadBaseGameFixture(root);
        var baseGameResrefs = Enum.GetValues<StructureType>().Select(type => ResrefOf(type).Trim())
            .Where(resref => resref.Length > 0 && !File.Exists(Path.Combine(root, "Module", "utp", resref + ".utp.json")))
            .Select(resref => resref.ToLowerInvariant()).ToHashSet();

        fixture.Keys.Should().BeEquivalentTo(baseGameResrefs,
            $"{BaseGameFixtureName} lists exactly the base-game blueprints that structures use, so model comparisons need no NWN:EE install");
    }

    [Test]
    public void BaseGameFixture_MatchesTheInstalledGame()
    {
        var installed = BaseGamePlaceableBlueprints.AppearanceByResref;
        if (installed == null)
            Assert.Ignore("No NWN:EE install found to check the fixture against.");

        foreach (var (resref, appearance) in ReadBaseGameFixture(FindRoot()))
            installed!.Should().ContainKey(resref).WhoseValue.Should().Be(appearance, $"{resref}'s appearance row in the base game");
    }

    private const string BaseGameFixtureName = "BaseGameStructureBlueprintAppearances.json";

    // Appearance rows of the base-game blueprints that structures use, read from the NWN:EE templates (checked above).
    private static Dictionary<string, int> ReadBaseGameFixture(string root) =>
        new(JsonSerializer.Deserialize<Dictionary<string, int>>(
                File.ReadAllText(Path.Combine(root, "SWLOR.Game.Server.Tests", "Service", BaseGameFixtureName)))!,
            StringComparer.OrdinalIgnoreCase);

    private static JsonDocument? ReadBlueprint(string root, string resref)
    {
        var path = Path.Combine(root, "Module", "utp", resref + ".utp.json");
        return File.Exists(path) ? JsonDocument.Parse(File.ReadAllText(path)) : null;
    }

    private static string FindRoot()
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln"))) root = root.Parent;
        return root?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }

    // Each set's blueprint for the pieces in OwnLookPieces: an existing placeable no other structure shows, a Meshy Field Cot
    // or Work Stool or Wall Banner (swc_cot_*, swc_stl_*, swc_ban_*: resref = model), or a sittable seat blueprint for an existing
    // model (swc_seat_*).
    private static readonly Dictionary<StructureType, string> SetPieceResrefs = new()
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

        [StructureType.AshmarkSignalLamp] = "zep_firebowl001",
        [StructureType.GuttermarkSignalLamp] = "_mdrn_pl_lampd05",
        [StructureType.TrailmarkSignalLamp] = "_mdrn_pl_lamp5",
        [StructureType.StonewakeSignalLamp] = "zep_ostnlntrn002",
        [StructureType.AegislineSignalLamp] = "aswtor_130",
        [StructureType.VeilcarvedSignalLamp] = "_mdrn_pl_brazier",
        [StructureType.SurgewakeSignalLamp] = "_mdrn_pl_lamp3",
        [StructureType.ResonantSignalLamp] = "zep_ostnlntrn004",
        [StructureType.FieldlineSignalLamp] = "_mdrn_pl_lghtpl1",
        [StructureType.CommandlineSignalLamp] = "_mdrn_pl_lampd08",

        [StructureType.AshmarkLowTable] = "_mdrn_pl_tableir",
        [StructureType.GuttermarkLowTable] = "_mdrn_pl_rustedt",
        [StructureType.TrailmarkLowTable] = "zep_table012",
        [StructureType.StonewakeLowTable] = "swp_table03",
        [StructureType.AegislineLowTable] = "swp_table04",
        [StructureType.VeilcarvedLowTable] = "_mdrn_pl_table07",
        [StructureType.SurgewakeLowTable] = "swp_table01",
        [StructureType.ResonantLowTable] = "_mdrn_pl_df_tblr",
        [StructureType.FieldlineLowTable] = "_mdrn_pl_table08",
        [StructureType.CommandlineLowTable] = "_mdrn_pl_table05",

        [StructureType.AshmarkWallBanner] = "swp_banner0001",
        [StructureType.GuttermarkWallBanner] = "swc_ban_gutter",
        [StructureType.TrailmarkWallBanner] = "swc_ban_trail",
        [StructureType.StonewakeWallBanner] = "_mdrn_pl_bannr02",
        [StructureType.AegislineWallBanner] = "swd3_ban005",
        [StructureType.VeilcarvedWallBanner] = "_mdrn_pl_bannr05",
        [StructureType.SurgewakeWallBanner] = "swc_ban_surge",
        [StructureType.ResonantWallBanner] = "swc_ban_reson",
        [StructureType.FieldlineWallBanner] = "swd3_ban006",
        [StructureType.CommandlineWallBanner] = "swlor_1846",

        [StructureType.AshmarkSupplyLocker] = "_mdrn_pl_mtllkrs",
        [StructureType.GuttermarkSupplyLocker] = "_mdrn_pl_mtllkgg",
        [StructureType.TrailmarkSupplyLocker] = "_mdrn_pl_canten2",
        [StructureType.StonewakeSupplyLocker] = "_mdrn_pl_chest04",
        [StructureType.AegislineSupplyLocker] = "_mdrn_pl_conta48",
        [StructureType.VeilcarvedSupplyLocker] = "daf_sw309",
        [StructureType.SurgewakeSupplyLocker] = "aswtor_134",
        [StructureType.ResonantSupplyLocker] = "_mdrn_pl_rustdcr",
        [StructureType.FieldlineSupplyLocker] = "_mdrn_pl_mtllkcr",
        [StructureType.CommandlineSupplyLocker] = "swd_ward01",

        [StructureType.AshmarkFloorMat] = "zep_skin001",
        [StructureType.GuttermarkFloorMat] = "zep_throwrug050",
        [StructureType.TrailmarkFloorMat] = "wookiee_rug",
        [StructureType.StonewakeFloorMat] = "zep_throwrug058",
        [StructureType.AegislineFloorMat] = "_mdrn_pl_matpurp",
        [StructureType.VeilcarvedFloorMat] = "zep_throwrug052",
        [StructureType.SurgewakeFloorMat] = "_mdrn_pl_carpt03",
        [StructureType.ResonantFloorMat] = "zep_throwrug054",
        [StructureType.FieldlineFloorMat] = "_mdrn_pl_carpt04",
        [StructureType.CommandlineFloorMat] = "_mdrn_pl_carpt02",

        [StructureType.AshmarkDataConsole] = "_mdrn_pl_conso05",
        [StructureType.GuttermarkDataConsole] = "swlor_1119",
        [StructureType.TrailmarkDataConsole] = "swlor_1080",
        [StructureType.StonewakeDataConsole] = "_mdrn_pl_conso32",
        [StructureType.AegislineDataConsole] = "swd2_comp005",
        [StructureType.VeilcarvedDataConsole] = "daf_sw315",
        [StructureType.SurgewakeDataConsole] = "swd3_upter01",
        [StructureType.ResonantDataConsole] = "_mdrn_pl_conso33",
        [StructureType.FieldlineDataConsole] = "_mdrn_pl_conso07",
        [StructureType.CommandlineDataConsole] = "swd3_comp009",

        [StructureType.AshmarkWorkStool] = "swc_stl_ash",
        [StructureType.GuttermarkWorkStool] = "_mdrn_pl_stool05",
        [StructureType.TrailmarkWorkStool] = "_mdrn_pl_stool04",
        [StructureType.StonewakeWorkStool] = "_mdrn_pl_stool",
        [StructureType.AegislineWorkStool] = "swc_seat_steel",
        [StructureType.VeilcarvedWorkStool] = "swc_stl_veil",
        [StructureType.SurgewakeWorkStool] = "swc_stl_surge",
        [StructureType.ResonantWorkStool] = "zep_logsit001",
        [StructureType.FieldlineWorkStool] = "swc_stl_field",
        [StructureType.CommandlineWorkStool] = "swc_stl_cmd",
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
        [StructureType.StonewakeStoneAltar] = "zep_stones018",
        [StructureType.AegislineSwordPedestal] = "swc_ped_aegis",
        [StructureType.VeilcarvedRunePillar] = "_mdrn_pl_pillr13",
        [StructureType.SurgewakeHoloProjector] = "_mdrn_pl_holocon",
        [StructureType.ResonantCrystal] = "x2_plc_crystal",
        [StructureType.FieldlineDisplayCase] = "zep_displaycase1",
        [StructureType.CommandlineBust] = "swc_bust_cmd",
    };
}
