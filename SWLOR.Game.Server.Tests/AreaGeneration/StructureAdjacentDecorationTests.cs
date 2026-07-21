using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;
using SWLOR.Game.Server.Service.AreaGenerationService.Decoration;
using SWLOR.Game.Server.Service.AreaGenerationService.Tileset;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Regression coverage for the StructureAdjacent decoration context (see
/// DungeonDecorationPlanner.IsStructureAdjacent and DecorationContext.StructureAdjacent): the fix
/// for reported sign panels/barriers free-standing next to knee-high dividers on generated fcx01
/// areas. Evidence (July 2026 city-density pass, hand-built fcx01 building adjacency at
/// Chebyshev&lt;=1): the sign panels themselves are NOT building dressing (swd_build007 2%,
/// swd2_fence010 0% building-adjacent -- they line roads, so they moved to CorridorSide); the items
/// hand-built builders genuinely anchor against buildings are wall lamps (_mdrn_pl_lamp4 52%),
/// building lights (_mdrn_pl_bldlit 41%), frontage containers (swd_conta003 51%), and base debris
/// (_mdrn_pl_df_chb 100%), which are curated as the StructureAdjacent bucket. Generated
/// measurement: 742-819 StructureAdjacent placements per 20 areas at 32x32, 100% within one tile of
/// a stamped structure footprint.
/// </summary>
public class StructureAdjacentDecorationTests
{
    private const int Size = 32;
    private const int SeedBase = 5001;

    private static readonly string[] StructureBucketResrefs =
    {
        "_mdrn_pl_lamp4", "_mdrn_pl_bldlit", "swd_conta003", "_mdrn_pl_df_chb"
    };

    private static (DungeonDetail Detail, DungeonTilesetProfile Tileset, DungeonLayoutProfile Layout, TilesetModel Model)
        Composition(string tilesetKey, string layoutKey)
    {
        var themes = new MineCaveDungeonDefinition().BuildDungeons();
        var tilesets = new BaseGameTilesetProfiles().BuildTilesetProfiles();
        foreach (var (k, v) in new StandardTilesetProfiles().BuildTilesetProfiles())
            tilesets.TryAdd(k, v);
        DungeonTilesetPaletteInheritance.Apply(tilesets);
        var layouts = new StandardLayoutProfiles().BuildLayoutProfiles();

        var tileset = tilesets[tilesetKey];
        return (themes.Values.First(), tileset, layouts[layoutKey], TilesetTestSource.LoadTileset(tileset.TilesetResref));
    }

    private static (ResolvedLayout Layout, List<PlannedDecoration> Plan) PlanFor(
        (DungeonDetail Detail, DungeonTilesetProfile Tileset, DungeonLayoutProfile Layout, TilesetModel Model) c, int seed)
    {
        var composition = new DungeonComposition { Content = c.Detail, Tileset = c.Tileset, Layout = c.Layout };
        var result = LayoutSolver.Solve(
            composition.BuildLayoutParameters(), c.Model, Size, Size, seed, c.Tileset.PrimaryOpenTerrain);
        result.Success.Should().BeTrue($"seed {seed} must solve: {result.FailureReason}");

        return (result.Resolved, DungeonDecorationPlanner.Plan(result.Resolved, c.Tileset, c.Detail, 100));
    }

    [Test]
    public void FutCity_StructureBucketItems_AlwaysAnchorAgainstStampedStructures()
    {
        var c = Composition(BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);
        var structurePlacements = 0;

        for (var i = 0; i < 8; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i);
            layout.StampedStructureTiles.Should().NotBeEmpty("fcx01/packed at 32x32 always stamps buildings");

            foreach (var placement in plan.Where(p => p.Context == DecorationContext.StructureAdjacent))
            {
                structurePlacements++;
                var tile = ((int)(placement.Position.X / 10f), (int)(placement.Position.Y / 10f));
                DungeonDecorationPlanner.IsStructureAdjacent(tile, layout).Should().BeTrue(
                    $"{placement.Resref} at tile {tile} must sit within one tile of a stamped structure footprint");
            }
        }

        // Measured 742/20 areas; a comfortable floor so the bucket can't silently die.
        structurePlacements.Should().BeGreaterThan(50,
            $"fcx01 building frontages should carry structure-anchored dressing (got {structurePlacements})");
    }

    [Test]
    public void FutCity_StructureBucketResrefs_NeverPlaceFreeStanding()
    {
        var c = Composition(BaseGameTilesetProfiles.FutCity, StandardLayoutProfiles.Packed);

        for (var i = 0; i < 8; i++)
        {
            var (layout, plan) = PlanFor(c, SeedBase + i);

            // The four curated StructureAdjacent resrefs exist ONLY in that bucket, so every
            // occurrence must be structure-anchored -- the "never free-stands in the open" contract.
            foreach (var placement in plan.Where(p => StructureBucketResrefs.Contains(p.Resref)))
            {
                placement.Context.Should().Be(DecorationContext.StructureAdjacent);
                var tile = ((int)(placement.Position.X / 10f), (int)(placement.Position.Y / 10f));
                DungeonDecorationPlanner.IsStructureAdjacent(tile, layout).Should().BeTrue();
            }
        }
    }

    [Test]
    public void NonCuratedTileset_WithStampedStructures_NeverEmitsStructureAdjacent()
    {
        // vmr01 stamps real OpenSetPieces but curates no StructureAdjacent bucket -- its tiles must
        // keep routing to their curated CorridorSide/WallAdjacent buckets exactly as before the
        // context existed (the gate in DungeonDecorationPlanner.TryResolveContext).
        var c = Composition(StandardTilesetProfiles.AncientRuin, StandardLayoutProfiles.Halls);

        for (var i = 0; i < 4; i++)
        {
            var (_, plan) = PlanFor(c, SeedBase + i);
            plan.Should().NotContain(p => p.Context == DecorationContext.StructureAdjacent,
                "StructureAdjacent only exists for palettes that curate the bucket");
        }
    }
}
