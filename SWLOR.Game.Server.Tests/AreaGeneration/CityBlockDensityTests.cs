using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Building-density regression coverage for the set-piece room-supply scaling mechanism (see
/// DungeonTilesetProfile.SetPieceRoomSupplyScaling, LayoutParameterConstraints.
/// ApplySetPieceRoomSupplyScaling, and LayoutGroupStamper.Stamp's largest-footprint-first order).
///
/// Reference: hand-built fcx01 city areas measure 0.152 group-tile (building) share
/// (_scratch-harness measurement over the 19 decorated fcx01 areas, July 2026 city-density pass).
/// Before the mechanism, generated 32x32 fcx01 areas measured a FLAT 0.039-0.040 regardless of
/// SetPiece budgets, because every layout style's room supply was constant in area (Complex/Halls
/// hardcode MinRooms=6/MaxRooms=9; PackedRooms reports at most MaxRooms leaves) while every stamped
/// building needs one SetPieceRoomCornerFloor-sized room. After the mechanism (32 seeds/district,
/// 0 solve failures): futcity/packed 0.150, futcity_plaza/complex 0.070 (its honest Tunnel-mode
/// ceiling -- most of a Complex grid is solid mass between rooms), aggregate 0.111.
///
/// Thresholds below sit well under those measured means (roughly 2/3) so ordinary seed variance
/// never flakes the suite, while a return of the room-supply ceiling (which measured 0.039-0.061)
/// still fails clearly.
/// </summary>
public class CityBlockDensityTests
{
    private const int Size = 32;
    private const int SeedBase = 5001;
    private const int SeedCount = 10;

    private static (int Areas, double GroupShare) MeasureGroupShare(string tilesetKey, string layoutKey)
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[tilesetKey];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[layoutKey];
        var model = TilesetTestSource.LoadTileset(tilesetProfile.TilesetResref);

        // Multi-tile groups only: 1x1 set pieces (ramps, transition decals) and road/feature tiles
        // are not "buildings". Mirrors the scratch harness's building classification (group tiles
        // from 2x2+ groups), so these thresholds are directly comparable to the measured shares.
        var buildingTileIds = new HashSet<int>();
        foreach (var group in model.Groups.Where(g => g.Rows * g.Columns >= 4))
        foreach (var tileId in group.TileIds.Where(t => t >= 0))
            buildingTileIds.Add(tileId);

        var areas = 0;
        var buildingTiles = 0L;
        var totalTiles = 0L;

        for (var i = 0; i < SeedCount; i++)
        {
            var seed = SeedBase + i;
            var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
            var result = LayoutSolver.Solve(
                composition.BuildLayoutParameters(), model, Size, Size, seed, tilesetProfile.PrimaryOpenTerrain);

            result.Success.Should().BeTrue(
                $"{tilesetKey}/{layoutKey} seed {seed} must generate at {Size}x{Size} (0/64 failures measured): {result.FailureReason}");

            areas++;
            totalTiles += Size * Size;
            buildingTiles += result.Resolved.Tiles.Count(t => buildingTileIds.Contains(t.TileId));
        }

        return (areas, (double)buildingTiles / totalTiles);
    }

    [Test]
    public void FutCityPacked_At32_ReachesHandBuiltBuildingDensity()
    {
        var (areas, share) = MeasureGroupShare(BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);

        areas.Should().Be(SeedCount);
        // Measured 0.150 mean over 32 seeds (hand-built reference 0.152); threshold at 0.10 --
        // the flat pre-mechanism ceiling measured 0.061 on this composition.
        share.Should().BeGreaterThan(0.10,
            $"futcity/packed at 32x32 should stamp buildings at near hand-built density (got {share:F4})");
    }

    [Test]
    public void FutCityPlazaComplex_At32_ClearsPreMechanismCeiling()
    {
        var (areas, share) = MeasureGroupShare(BaseGameTilesetProfiles.FutCityPlaza, StandardLayoutProfiles.Complex);

        areas.Should().Be(SeedCount);
        // Measured 0.070 mean over 32 seeds; the pre-mechanism ceiling measured 0.017 on this
        // composition (Tunnel mode caps the honest ceiling well below the Packed pairing's -- see
        // class doc comment). Threshold at 0.045.
        share.Should().BeGreaterThan(0.045,
            $"futcity_plaza/complex at 32x32 should stamp buildings well past the flat room-supply ceiling (got {share:F4})");
    }
}
