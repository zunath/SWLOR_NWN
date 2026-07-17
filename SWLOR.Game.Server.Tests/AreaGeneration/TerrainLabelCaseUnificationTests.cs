using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Regression coverage for MacroLayoutGenerator's terrain-label case unification (see the
/// unification block in MacroLayoutGenerator.Generate for the full mechanism writeup).
///
/// ttz01's .set file spells the SAME terrain two ways -- [GENERAL] Default=Grass (capital) but
/// [TERRAIN0] Name=grass with lowercase tile-corner labels -- and the Tropical profile declares
/// PrimaryOpenTerrain("grass") with no SolidTerrainOverride, so before the fix LayoutSolver stamped
/// SolidTerrain="Grass" / OpenTerrain="grass": one terrain, two ordinal spellings. Layout styles and
/// ValidateInvariants' open-connectivity check compare corner labels ORDINALLY while group
/// classification/site search/resolution compare case-insensitively, so the intended Solid==Open
/// open-field composition actually generated as a two-label cave, and every stamped set-piece
/// member's lowercase "grass" corners were rewritten to capital "Grass" (the SOLID spelling) by
/// LayoutGroupStamper.WriteMember's Canonicalize (solid checked first) -- physically converting open
/// corners to solid. Door-bearing groups route WallAlcove, whose Eq-based "fully solid" site search
/// accepted OPEN field cells anywhere, so a stamp against the open blob's edge could pinch off a
/// pocket: measured (ProbeTool, retryCount=1) 36-40% single-attempt "disconnected open space"
/// failures per door-bearing group in isolation and 181/300 with all nine wired, on Organic. The
/// door=True discriminator was exact because door-free siblings route OpenSetPiece, whose
/// room-interior + full-margin-ring site can never enclose anything. After unification the
/// composition is a true single-spelling open field and every sweep below measures 0 disconnections.
/// </summary>
public class TerrainLabelCaseUnificationTests
{
    private const int Size = 20;

    /// <summary>
    /// The nine ttz01 door-bearing 1x2/2x2 groups that route SetPieceWallAlcove on the Tropical
    /// grass-open composition (allCornersSolid is trivially true when Solid==Open) -- the exact set
    /// the pre-fix measurement flagged and the profile now wires at maxPerArea 1 each.
    /// </summary>
    private static readonly string[] TropicalDoorBearingGroups =
    {
        "Barn01_2x2", "Barn02_1x2", "Barn03_1x2", "Inn_1x2", "Farm01_2x2",
        "Farm02_1x2", "Farm03_1x2", "Barracks_1x2", "Windmill_2x2"
    };

    private static (int Successes, int Disconnected, int OtherFailures) SweepOrganic(
        DungeonTilesetProfile tilesetProfile, TilesetModel model, int seedCount)
    {
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Organic];
        var successes = 0;
        var disconnected = 0;
        var otherFailures = 0;

        for (var seed = 1; seed <= seedCount; seed++)
        {
            var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            var result = LayoutSolver.Solve(parameters, model, Size, Size, seed, tilesetProfile.PrimaryOpenTerrain, retryCount: 1);
            if (result.Success) successes++;
            else if ((result.FailureReason ?? string.Empty).Contains("disconnected")) disconnected++;
            else otherFailures++;
        }

        return (successes, disconnected, otherFailures);
    }

    /// <summary>
    /// The production Tropical wiring (all nine door-bearing WallAlcove groups at maxPerArea 1 each,
    /// alongside the nine door-free groups and the relief pieces) against Organic: measured 181/300
    /// single-attempt disconnections before the unification fix, 0/300 after (all 300 resolve).
    /// </summary>
    [Test]
    public void TropicalOrganic_ProductionWiring_NeverDisconnects()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Tropical];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in TropicalDoorBearingGroups)
        {
            tilesetProfile.SetPieces.Keys.Should().Contain(
                k => string.Equals(k, groupName, StringComparison.OrdinalIgnoreCase),
                $"the Tropical profile must wire door-bearing group '{groupName}' (the pre-fix KNOWN GAP is resolved)");
        }

        var (successes, disconnected, otherFailures) = SweepOrganic(tilesetProfile, model, seedCount: 300);

        disconnected.Should().Be(0,
            "the terrain-label case unification makes Tropical a true single-spelling open field, so WallAlcove stamps are label no-ops and can never sever open space");
        otherFailures.Should().Be(0);
        successes.Should().Be(300);
    }

    /// <summary>
    /// Each door-bearing group in isolation (maxPerArea 5, the pre-fix measurement condition that
    /// produced 36-40% disconnections per group across these exact seeds): 0 disconnections each.
    /// </summary>
    [Test]
    public void TropicalOrganic_IsolatedDoorBearingGroups_NeverDisconnect()
    {
        var profiles = new BaseGameTilesetProfiles();
        var model = TilesetTestSource.LoadTileset("ttz01");

        foreach (var groupName in TropicalDoorBearingGroups)
        {
            var tilesetProfile = profiles.BuildTilesetProfiles()[BaseGameTilesetProfiles.Tropical];
            tilesetProfile.SetPieces = new Dictionary<string, int> { [groupName] = 5 };

            var (_, disconnected, otherFailures) = SweepOrganic(tilesetProfile, model, seedCount: 100);

            disconnected.Should().Be(0, $"isolated '{groupName}' measured 36-40% single-attempt disconnections before the unification fix");
            otherFailures.Should().Be(0, $"isolated '{groupName}' must not introduce non-disconnection failures either");
        }
    }

    /// <summary>
    /// The structurally identical sibling compositions that were ALREADY safe before the fix (their
    /// Solid/Open labels agree ordinally, so unification is gated off for them) must stay at 0:
    /// ttr01/RuralGrass's tile-for-tile identical door-bearing groups and ttz01's own sand-open
    /// recomposition. Guards the unification's no-split no-op gate as much as the fix itself.
    /// </summary>
    [Test]
    public void AgreeingSiblingCompositions_StayDisconnectionFree()
    {
        var profiles = new BaseGameTilesetProfiles();
        var ttr01 = TilesetTestSource.LoadTileset("ttr01");
        var ttz01 = TilesetTestSource.LoadTileset("ttz01");

        foreach (var (profileKey, model, groupName) in new[]
                 {
                     (BaseGameTilesetProfiles.RuralGrass, ttr01, "Barn 1 (2x2)"),
                     (BaseGameTilesetProfiles.RuralGrass, ttr01, "Windmill (2x2)"),
                     (BaseGameTilesetProfiles.RuralGrass, ttr01, "Barracks (1x2)"),
                     (BaseGameTilesetProfiles.TropicalSand, ttz01, "Barracks_1x2(sand)"),
                 })
        {
            var tilesetProfile = profiles.BuildTilesetProfiles()[profileKey];
            tilesetProfile.SetPieces = new Dictionary<string, int> { [groupName] = 5 };

            var (_, disconnected, otherFailures) = SweepOrganic(tilesetProfile, model, seedCount: 100);

            disconnected.Should().Be(0, $"'{groupName}' on '{profileKey}' measured 0 disconnections before the fix and must stay 0 after it");
            otherFailures.Should().Be(0);
        }
    }

    /// <summary>
    /// Mechanism-level pin: a generated Tropical/Organic layout's corner grid must contain ONLY the
    /// tileset's declared [TERRAIN] spellings (ordinal comparison). Before the fix the grid mixed
    /// "Grass" (the .set GENERAL Default spelling, stamped as SolidTerrain) with "grass" (the
    /// declared [TERRAIN0]/profile spelling), which is exactly the split that turned the intended
    /// open field into a two-label mixed regime.
    /// </summary>
    [Test]
    public void TropicalOrganic_CornerGrid_UsesOnlyDeclaredTerrainSpellings()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Tropical];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Organic];

        var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
        var parameters = composition.BuildLayoutParameters();
        parameters.EntranceCount = 1;
        parameters.ExitCount = 1;
        parameters.DoorTransitions = true;

        var result = LayoutSolver.Solve(parameters, model, Size, Size, seed: 3, tilesetProfile.PrimaryOpenTerrain, retryCount: 1);
        result.Success.Should().BeTrue(result.FailureReason);

        var declaredSpellings = new HashSet<string>(model.Terrains, StringComparer.Ordinal);
        var corners = result.Layout.Corners;
        var undeclared = new List<string>();
        for (var x = 0; x <= corners.Width; x++)
        {
            for (var y = 0; y <= corners.Height; y++)
            {
                var label = corners.Labels[x, y];
                if (!declaredSpellings.Contains(label))
                    undeclared.Add($"({x},{y})='{label}'");
            }
        }

        undeclared.Should().BeEmpty(
            "every corner label must use the tileset's own declared [TERRAIN] spelling -- a case-split label (e.g. 'Grass' vs declared 'grass') recreates the mixed-regime bug");
    }
}
