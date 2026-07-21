using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Placement-rate regression coverage for LayoutGroupStamper.TryClassifyReliefPiece's round-4
/// exterior-tail-closure generalization: a ReliefPiece (raised 1x1 group) now tolerates a door slot
/// (never spawns a door object, matching WallAlcove/OpenSetPiece/WallRoom's own precedent) and edges
/// that ALL equal the composition's own declared RampCrosser (matching
/// TileCoverageCensusTests.IsTerrainReliefReachable's identical ungrouped-tile rule), instead of
/// rejecting any crosser or door slot outright.
///
/// This closes ttf01's "Cave" and raised gate-tower/breach/moss-wall family, ttd01's "SmallCave", and
/// tdm01's "[City/Cave/Desert/Organic] Cave Entrance" -- each isolated here (the same isolation
/// technique OpenSetPiecePlacementRateTests uses) to prove the mechanism genuinely places the piece in
/// real generation output, not just that the census credits it as structurally reachable.
///
/// IMPORTANT: only StandardLayoutProfiles.Complex requests ElevationRegions/PoolRegions/ReliefRegions
/// today (Halls/Organic leave every height knob at its 0 default) -- these tests deliberately pair
/// every tileset with Complex, the only layout style where LayoutElevationPainter/LayoutReliefPainter
/// ever paint a non-trivial field for a ReliefPiece to match against. Measured control rate (the
/// pre-existing, unmodified crosser-free/doorless "Ramp"/"[Cave] Ramp" pieces): 485/500 and 462/500.
/// Measured target rate (the newly door/crosser-tolerant pieces this round closes): "Cave" 485/500,
/// "SmallCave" 485/500 -- both statistically indistinguishable from the control, confirming the
/// door/RampCrosser tolerance costs nothing in placement rate.
///
/// The one shape this generalization does NOT reach -- ttf01's 2x2 "City Gate - Forest/Cobbles" GROUPS
/// -- stays documented-exempt: LayoutReliefPainter.TrySpliceReliefLane only ever carves a lane exactly
/// ONE cell wide along a single axis, so the "2-wide wall mass" field these groups need (two footprint
/// columns independently touching the network, plus a shared interior seam) is never painted. Measured
/// directly: 0/450 successful Complex generations ever contain a matching site at orientation 0 (see
/// BaseGameTilesetProfiles.Forest's own doc comment).
/// </summary>
public class ReliefPiecePlacementRateTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private const int Size = 20;

    /// <summary>
    /// Isolates one named 1x1 group as the composition's ONLY configured SetPiece and returns how many
    /// of <paramref name="seedCount"/> single-attempt (retryCount=1) seeds place it -- mirrors
    /// OpenSetPiecePlacementRateTests.MeasureIsolatedGroupHits exactly, just against a ReliefPiece-kind
    /// group instead of an OpenSetPiece-kind one.
    /// </summary>
    private static (int Successes, int Hits) MeasureIsolatedGroupHits(
        DungeonTilesetProfile tilesetProfile, DungeonLayoutProfile layoutProfile, TilesetModel model,
        string groupName, int maxPerArea, int seedBase, int seedCount, int seedStride = 13)
    {
        tilesetProfile.SetPieces = new Dictionary<string, int> { [groupName] = maxPerArea };

        var group = model.Groups.Find(g => string.Equals(g.Name, groupName, System.StringComparison.OrdinalIgnoreCase));
        group.Should().NotBeNull($"'{groupName}' must exist in {model.Resref}'s real .set data");
        var anchorTileId = group.TileIds[0];

        var successes = 0;
        var hits = 0;
        for (var i = 0; i < seedCount; i++)
        {
            var seed = seedBase + i * seedStride;
            var composition = new DungeonComposition { Tileset = tilesetProfile, Layout = layoutProfile };
            var result = LayoutSolver.Solve(composition.BuildLayoutParameters(), model, Size, Size, seed, tilesetProfile.PrimaryOpenTerrain, retryCount: 1);
            if (!result.Success) continue;
            successes++;

            // PinnedTiles values are (TileId, Orientation, PlacementHeight) tuples -- orientation and
            // placementHeight vary per site, so match on TileId alone.
            var hit = false;
            foreach (var pinned in result.Layout.PinnedTiles.Values)
            {
                if (pinned.TileId == anchorTileId) { hit = true; break; }
            }
            if (hit) hits++;
        }

        return (successes, hits);
    }

    [Test]
    public void CaveOnForestComplex_PlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Forest];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Cave", maxPerArea: 5, seedBase: 96000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        hits.Should().BeGreaterThan(0, "ttf01's raised, door-bearing 'Cave' ReliefPiece must place on at least some of the successful seeds now that door slots are tolerated");
    }

    [Test]
    public void SmallCaveOnDesertComplex_PlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Desert];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "SmallCave", maxPerArea: 5, seedBase: 96000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        hits.Should().BeGreaterThan(0, "ttd01's raised, door-bearing 'SmallCave' ReliefPiece must place on at least some of the successful seeds now that door slots are tolerated");
    }

    [Test]
    public void CaveEntranceOnMinesAndCavernsComplex_PlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MinesAndCaverns];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "[Cave] Cave Entrance", maxPerArea: 5, seedBase: 96000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        hits.Should().BeGreaterThan(0, "tdm01's raised, door-bearing '[Cave] Cave Entrance' ReliefPiece must place on at least some of the successful seeds now that door slots are tolerated");
    }

    [Test]
    public void WallDoorCityForestOnForestCityWallComplex_PlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.ForestCityWall];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Wall - Door, City/Forest", maxPerArea: 5, seedBase: 96000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        hits.Should().BeGreaterThan(0, "ttf01's raised, door-bearing, CityWall-crossered 'Wall - Door, City/Forest' ReliefPiece must place on at least some of the successful seeds now that door slots and the composition's own RampCrosser are tolerated");
    }

    [Test]
    public void RampCityWallOnForestCityWallComplex_PlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.ForestCityWall];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Ramp - City Wall", maxPerArea: 5, seedBase: 96000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        hits.Should().BeGreaterThan(0, "ttf01's raised, doorless, CityWall-crossered 'Ramp - City Wall' ReliefPiece must place on at least some of the successful seeds");
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.FrozenWastes' "Cave" (door-bearing) and "Ramp"
    /// (doorless) ReliefPieces -- both all-Floor 1x1 groups, no RampCrosser declared at all (tti01 has
    /// 0 crossers total; MaxElevationRegions/MaxReliefRegions still paint raised Floor rim edges via
    /// corner-height alone). Measured (seedBase 95000, 150 seeds each, Complex, all successes=150):
    /// "Cave" 97.3% (146/150), "Ramp" 97.3% (146/150) -- both statistically in line with the other
    /// tilesets' own crosser-free "Ramp"/"[Cave] Ramp" control rate this file's own doc comment cites.
    /// </summary>
    [Test]
    public void CaveAndRampOnFrozenWastesComplex_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.FrozenWastes];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "Cave", "Ramp" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"FrozenWastes' '{groupName}' ReliefPiece must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.Tropical's "Cave"/"DwarfCave"/"Ramp" ReliefPieces --
    /// all-grass 1x1 groups, no RampCrosser declared (ttz01's 4 crossers are stream/wall1/wall2/road,
    /// none a dedicated ramp lane; MaxReliefRegions still paints raised Grass rim edges via corner-
    /// height alone). Measured (seedBase 95000, 150 seeds each, Complex, all successes=150): all three
    /// 77.3% (116/150).
    /// </summary>
    [Test]
    public void CaveDwarfCaveAndRampOnTropicalComplex_PlaceInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Tropical];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        foreach (var groupName in new[] { "Cave", "DwarfCave", "Ramp" })
        {
            var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, groupName, maxPerArea: 5, seedBase: 95000, seedCount: 150);

            successes.Should().BeGreaterThan(140);
            hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
                $"Tropical's '{groupName}' ReliefPiece must place on a meaningful share of the {successes} successful seeds (got {hits})");
        }
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.Underdark's "Cave" ReliefPiece (1x1, non-flat
    /// [Floor 1,1,0,0], crosser-free, one door slot -- the identical shape as tdm01's "[Cave] Cave
    /// Entrance"). Measured (seedBase 95000, 150 seeds, Complex, successes=150): 97.3% (146/150) -- in
    /// line with FrozenWastes' own identically-shaped "Cave" ReliefPiece rate this file's own doc
    /// comment cites.
    /// </summary>
    [Test]
    public void CaveOnUnderdarkComplex_PlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.Underdark];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "Cave", maxPerArea: 5, seedBase: 95000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
            $"Underdark's 'Cave' ReliefPiece must place on a meaningful share of the {successes} successful seeds (got {hits})");
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.EarlyWinter's "HillCave1" ReliefPiece (1x1, non-flat
    /// [Grass 1,1,0,0], crosser-free, one door slot -- the identical shape as tdm01's "[Cave] Cave
    /// Entrance"/Underdark's own "Cave"). Measured (seedBase 95000, 150 seeds, Complex, successes=150):
    /// 77.3% (116/150).
    /// </summary>
    [Test]
    public void HillCave1OnEarlyWinterComplex_PlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.EarlyWinter];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "HillCave1", maxPerArea: 5, seedBase: 95000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
            $"EarlyWinter's 'HillCave1' ReliefPiece must place on a meaningful share of the {successes} successful seeds (got {hits})");
    }

    /// <summary>
    /// Placement proof for BaseGameTilesetProfiles.MedievalRural's "HillCave1" ReliefPiece (1x1,
    /// non-flat [Grass 1,1,0,0], crosser-free, one door slot -- the IDENTICAL shape as trs02's own
    /// "HillCave1", literally the same group name). Measured (seedBase 95000, 150 seeds, Complex,
    /// successes=150): 77.3% (116/150) -- the exact same rate EarlyWinter's own HillCave1 measures (same
    /// tile geometry, same shape).
    /// </summary>
    [Test]
    public void HillCave1OnMedievalRuralComplex_PlacesInIsolation()
    {
        var tilesetProfile = new BaseGameTilesetProfiles().BuildTilesetProfiles()[BaseGameTilesetProfiles.MedievalRural];
        var layoutProfile = new StandardLayoutProfiles().BuildLayoutProfiles()[StandardLayoutProfiles.Complex];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var (successes, hits) = MeasureIsolatedGroupHits(tilesetProfile, layoutProfile, model, "HillCave1", maxPerArea: 5, seedBase: 95000, seedCount: 150);

        successes.Should().BeGreaterThan(140);
        hits.Should().BeGreaterOrEqualTo((int)(successes * 0.5),
            $"MedievalRural's 'HillCave1' ReliefPiece must place on a meaningful share of the {successes} successful seeds (got {hits})");
    }
}
