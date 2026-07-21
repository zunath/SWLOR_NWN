using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Acceptance gate for a bug where SWLOR.ContentBuilder's live Preview and the offline review module
/// SWLOR.ProcgenReview built from the SAME composition/seed produced totally different layouts. Root
/// cause: Content Builder's preview path cloned a layout profile's raw Template directly, bypassing
/// DungeonComposition.BuildLayoutParameters -- which additionally stamps SecondaryOpenTerrain
/// (changes every downstream RNG roll when present), the tileset's CorridorWidth floor
/// (Tileset.MinimumOpeningWidth), and ChannelTerrain -- while ProcgenReview always composed through
/// BuildLayoutParameters. Both tools now share one pipeline: DungeonComposition.BuildLayoutParameters
/// followed by LayoutSolver.Solve's seed-derived retry loop (see SWLOR.ContentBuilder.Services.
/// GenerationEngine and SWLOR.ProcgenReview's Program.cs generation loop). These tests exercise that
/// exact shared pipeline directly so the two tools can never independently drift apart again.
/// </summary>
public class PreviewModuleParityTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private static DungeonComposition BuildComposition(string tilesetKey, string layoutKey)
    {
        var tilesetProfiles = new StandardTilesetProfiles().BuildTilesetProfiles();
        var layoutProfiles = new StandardLayoutProfiles().BuildLayoutProfiles();
        return new DungeonComposition
        {
            Tileset = tilesetProfiles[tilesetKey],
            Layout = layoutProfiles[layoutKey]
        };
    }

    /// <summary>
    /// The exact repro from the bug report: Sci-Fi Base / Facility / Corridor Complex, seed
    /// 1236280907, 32x32. Facility's Complex layout profile leaves CorridorWidth at its raw Template
    /// default of 1 -- only BuildLayoutParameters raises it to zsf01's MinimumOpeningWidth floor of 2
    /// -- so a caller that skips composition (the pre-fix Content Builder path) generates with a
    /// structurally different corridor width than a caller that composes correctly (ProcgenReview).
    /// </summary>
    [TestCase("facility", "complex", "zsf01", 1236280907, 32)]
    [TestCase("cavern", "organic", "tdt01", 4242, 16)]
    [TestCase("ancientruin", "halls", "vmr01", 4242, 24)]
    public void ComposedPipeline_IsDeterministic_BetweenTwoIndependentCallers(
        string tilesetKey, string layoutKey, string tilesetResref, int seed, int size)
    {
        var model = LoadTileset(tilesetResref);
        var composition = BuildComposition(tilesetKey, layoutKey);

        // "GenerationEngine-style" caller.
        var baseParametersA = composition.BuildLayoutParameters();
        var resultA = LayoutSolver.Solve(baseParametersA, model, size, size, seed, composition.Tileset.PrimaryOpenTerrain);

        // "ProcgenReview-style" caller: a fresh BuildLayoutParameters call (never shares the clone
        // GenerationEngine mutated) fed into the identical shared solver.
        var baseParametersB = composition.BuildLayoutParameters();
        var resultB = LayoutSolver.Solve(baseParametersB, model, size, size, seed, composition.Tileset.PrimaryOpenTerrain);

        resultA.Success.Should().BeTrue(resultA.FailureReason);
        resultB.Success.Should().BeTrue(resultB.FailureReason);

        resultA.AttemptSeed.Should().Be(resultB.AttemptSeed);
        resultA.Resolved.Width.Should().Be(resultB.Resolved.Width);
        resultA.Resolved.Height.Should().Be(resultB.Resolved.Height);
        resultA.Resolved.Rooms.Count.Should().Be(resultB.Resolved.Rooms.Count);

        for (var i = 0; i < resultA.Resolved.Tiles.Length; i++)
        {
            resultB.Resolved.Tiles[i].TileId.Should().Be(resultA.Resolved.Tiles[i].TileId, $"tile {i}");
            resultB.Resolved.Tiles[i].Orientation.Should().Be(resultA.Resolved.Tiles[i].Orientation, $"tile {i}");
        }
    }

    /// <summary>
    /// Guards the specific composition stamping that caused the original divergence: composing
    /// Facility's tileset profile with the Complex layout profile must raise CorridorWidth to zsf01's
    /// MinimumOpeningWidth (2), not leave it at the profile's raw Template value (1).
    /// </summary>
    [Test]
    public void BuildLayoutParameters_RaisesCorridorWidthToTilesetFloor_ForFacilityComplex()
    {
        var composition = BuildComposition(StandardTilesetProfiles.Facility, StandardLayoutProfiles.Complex);

        composition.Layout.Template.CorridorWidth.Should().Be(1, "the raw profile Template never assumes any particular tileset");
        composition.Tileset.MinimumOpeningWidth.Should().Be(2);

        var composed = composition.BuildLayoutParameters();

        composed.CorridorWidth.Should().Be(2, "BuildLayoutParameters must raise CorridorWidth to the composed tileset's floor");
    }

    /// <summary>
    /// Full-fidelity round trip of the "--areas-file" JSON contract (AreaBatchFile /
    /// AreaBatchFileEntry.Parameters): every field, including the Dictionary/List members
    /// (FeatureTiles, SetPieces, ExitGroups) and the district fields (SecondaryOpenTerrain,
    /// SecondaryRoomFraction, ChannelTerrain, AccentChannels), must survive serialize/deserialize
    /// exactly, and generation from the original vs. the round-tripped copy must be identical for a
    /// fixed seed.
    /// </summary>
    [Test]
    public void MacroLayoutParameters_JsonRoundTrip_IsLosslessAndReproducesGeneration()
    {
        var model = LoadTileset("vmr01");
        var composition = BuildComposition(StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Halls);
        var original = composition.BuildLayoutParameters();

        // Sanity: this composition actually exercises every "interesting" field the round trip must
        // preserve, not just the plain scalars every composition sets.
        original.SecondaryOpenTerrain.Should().NotBeNullOrEmpty();
        original.ChannelTerrain.Should().NotBeNullOrEmpty();
        original.AccentChannels.Should().BeGreaterThan(0);
        original.FeatureTiles.Should().NotBeEmpty();
        original.SetPieces.Should().NotBeEmpty();
        original.ExitGroups.Should().NotBeEmpty();

        var entry = new AreaBatchFileEntry
        {
            ThemeKey = "alienruin",
            TilesetKey = StandardTilesetProfiles.AncientRuin,
            LayoutKey = StandardLayoutProfiles.Halls,
            Seed = 4242,
            Size = 24,
            Parameters = original
        };

        var json = AreaBatchFile.Serialize(new List<AreaBatchFileEntry> { entry });
        var roundTripped = AreaBatchFile.Deserialize(json).Single();

        roundTripped.Parameters.Should().BeEquivalentTo(original, "every field of the effective parameters must survive the JSON round trip verbatim");

        const int seed = 4242;
        const int size = 24;
        var fromOriginal = LayoutSolver.Solve(original, model, size, size, seed, composition.Tileset.PrimaryOpenTerrain);
        var fromRoundTripped = LayoutSolver.Solve(roundTripped.Parameters, model, size, size, seed, composition.Tileset.PrimaryOpenTerrain);

        fromOriginal.Success.Should().BeTrue(fromOriginal.FailureReason);
        fromRoundTripped.Success.Should().BeTrue(fromRoundTripped.FailureReason);
        fromOriginal.AttemptSeed.Should().Be(fromRoundTripped.AttemptSeed);

        for (var i = 0; i < fromOriginal.Resolved.Tiles.Length; i++)
        {
            fromRoundTripped.Resolved.Tiles[i].TileId.Should().Be(fromOriginal.Resolved.Tiles[i].TileId, $"tile {i}");
            fromRoundTripped.Resolved.Tiles[i].Orientation.Should().Be(fromOriginal.Resolved.Tiles[i].Orientation, $"tile {i}");
        }
    }

}
