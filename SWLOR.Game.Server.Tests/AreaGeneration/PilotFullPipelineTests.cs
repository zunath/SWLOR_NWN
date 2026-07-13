using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.DungeonDefinition;
using SWLOR.Game.Server.Service.AreaGenerationService;

namespace SWLOR.Game.Server.Tests.AreaGeneration;

/// <summary>
/// Acceptance gate for the three base-game pilot tileset profiles (Crypt/tdc01, Dungeon/tde01,
/// City Interior/tin01 -- see BaseGameTilesetProfiles): runs the SAME production pipeline
/// SWLOR.ProcgenReview and SWLOR.ContentBuilder use (DungeonComposition.BuildLayoutParameters +
/// LayoutSolver.Solve's seed-derived retry loop) across many seeds for each pilot paired with the
/// layout profiles this onboarding wave targets (Complex/Tunnel, Halls/OpenLane, Organic), asserting
/// 100% generation+resolution success and per-tile edge self-consistency, mirroring
/// TunnelCorridorTests' AssertEdgeAgreement pattern.
/// </summary>
public class PilotFullPipelineTests
{
    private static TilesetModel LoadTileset(string tilesetResref) => TilesetTestSource.LoadTileset(tilesetResref);

    private static readonly Dictionary<string, DungeonTilesetProfile> TilesetProfiles =
        new BaseGameTilesetProfiles().BuildTilesetProfiles();

    private static readonly Dictionary<string, DungeonLayoutProfile> LayoutProfiles =
        new StandardLayoutProfiles().BuildLayoutProfiles();

    // Every pilot lacks the Alley crosser vocabulary (see the base-game tileset census), so Streets is
    // out of scope for all three (same as every other non-vmr01 tileset) -- Complex (Tunnel mode),
    // Halls (OpenLane), and Organic are the combos this onboarding wave targets as "where coverage allows".
    public static IEnumerable<object[]> PilotLayoutCases()
    {
        foreach (var tilesetKey in new[] { BaseGameTilesetProfiles.Crypt, BaseGameTilesetProfiles.Dungeon, BaseGameTilesetProfiles.CityInterior })
        foreach (var layoutKey in new[] { StandardLayoutProfiles.Complex, StandardLayoutProfiles.Halls, StandardLayoutProfiles.Organic })
            yield return new object[] { tilesetKey, layoutKey };
    }

    /// <summary>
    /// Locks in the pathnode-opening-width audit result (PathNodeOpeningWidthAudit.cs) against the
    /// REAL SWLOR_Haks-resolved tileset data for each pilot: BaseGameTilesetProfiles' configured
    /// MinimumOpeningWidth (all three left at the default 1) must match what the audit computes fresh
    /// from the .set data, so a future SWLOR_Haks edit to these tilesets' pathnodes can't silently
    /// invalidate the profile's assumption.
    /// </summary>
    [TestCase(BaseGameTilesetProfiles.Crypt)]
    [TestCase(BaseGameTilesetProfiles.Dungeon)]
    [TestCase(BaseGameTilesetProfiles.CityInterior)]
    public void MinimumOpeningWidth_MatchesFreshPathNodeAudit(string tilesetKey)
    {
        var profile = TilesetProfiles[tilesetKey];
        var model = LoadTileset(profile.TilesetResref);

        var audited = PathNodeOpeningWidthAudit.DetermineMinimumOpeningWidth(model, profile.PrimaryOpenTerrain);

        audited.Should().Be(profile.MinimumOpeningWidth,
            $"{tilesetKey}'s configured MinimumOpeningWidth must match the pathnode audit computed fresh from '{profile.TilesetResref}'");
    }

    [TestCaseSource(nameof(PilotLayoutCases))]
    public void FullPipelineSucceedsAcrossManySeeds(string tilesetKey, string layoutKey)
    {
        var tilesetProfile = TilesetProfiles[tilesetKey];
        var layoutProfile = LayoutProfiles[layoutKey];
        var model = LoadTileset(tilesetProfile.TilesetResref);

        var composition = new DungeonComposition { Content = null, Tileset = tilesetProfile, Layout = layoutProfile };
        var failures = new List<string>();

        const int size = 20;
        for (var seed = 6000; seed < 6015; seed++)
        {
            var parameters = composition.BuildLayoutParameters();
            parameters.EntranceCount = 1;
            parameters.ExitCount = 1;
            parameters.DoorTransitions = true;

            var solved = LayoutSolver.Solve(parameters, model, size, size, seed, tilesetProfile.PrimaryOpenTerrain);
            if (!solved.Success)
            {
                failures.Add($"seed {seed}: generation failed -- {solved.FailureReason}");
                continue;
            }

            AssertEdgeSelfConsistency(model, solved.Resolved, seed, failures);
        }

        failures.Should().BeEmpty(
            $"{tilesetKey}/{layoutKey} must generate+resolve successfully with self-consistent edges across every seed");
    }

    /// <summary>
    /// Per-tile edge self-consistency: every resolved tile's oriented edge must equal the oriented
    /// edge its neighbor presents back across the shared boundary (both directions), and a tile's own
    /// declared edge must be internally well-formed (empty or a name present in this tileset's crosser
    /// vocabulary). This doesn't require the original macro crosser plan (LayoutSolver's public surface
    /// only exposes the resolved layout) -- unlike TunnelCorridorTests.AssertEdgeAgreement, which
    /// diffs against the plan directly, this checks the resolved tiles agree with EACH OTHER, which is
    /// exactly the invariant that makes seams render and path correctly in the engine.
    /// </summary>
    private static void AssertEdgeSelfConsistency(TilesetModel model, ResolvedLayout resolved, int seed, List<string> failures)
    {
        var tilesById = model.Tiles.ToDictionary(t => t.TileId);
        var knownCrossers = new HashSet<string>(model.Crossers, StringComparer.OrdinalIgnoreCase);

        for (var y = 0; y < resolved.Height; y++)
        {
            for (var x = 0; x < resolved.Width; x++)
            {
                var tile = resolved.GetTile(x, y);
                var record = tilesById[tile.TileId];

                var top = record.GetEdgeAt(tile.Orientation, EdgeSlot.Top) ?? string.Empty;
                var right = record.GetEdgeAt(tile.Orientation, EdgeSlot.Right) ?? string.Empty;
                var bottom = record.GetEdgeAt(tile.Orientation, EdgeSlot.Bottom) ?? string.Empty;
                var left = record.GetEdgeAt(tile.Orientation, EdgeSlot.Left) ?? string.Empty;

                foreach (var edge in new[] { top, right, bottom, left })
                {
                    if (edge.Length != 0 && !knownCrossers.Contains(edge))
                        failures.Add($"seed {seed}: cell ({x},{y}) TILE{tile.TileId} o={tile.Orientation} has unknown crosser '{edge}'");
                }

                if (x + 1 < resolved.Width)
                {
                    var neighbor = resolved.GetTile(x + 1, y);
                    var neighborRecord = tilesById[neighbor.TileId];
                    var neighborLeft = neighborRecord.GetEdgeAt(neighbor.Orientation, EdgeSlot.Left) ?? string.Empty;
                    if (!string.Equals(right, neighborLeft, StringComparison.OrdinalIgnoreCase))
                        failures.Add($"seed {seed}: cell ({x},{y})|({x + 1},{y}) edge mismatch: '{right}' vs '{neighborLeft}'");
                }

                if (y + 1 < resolved.Height)
                {
                    var neighbor = resolved.GetTile(x, y + 1);
                    var neighborRecord = tilesById[neighbor.TileId];
                    var neighborBottom = neighborRecord.GetEdgeAt(neighbor.Orientation, EdgeSlot.Bottom) ?? string.Empty;
                    if (!string.Equals(top, neighborBottom, StringComparison.OrdinalIgnoreCase))
                        failures.Add($"seed {seed}: cell ({x},{y})|({x},{y + 1}) edge mismatch: '{top}' vs '{neighborBottom}'");
                }
            }
        }
    }
}
