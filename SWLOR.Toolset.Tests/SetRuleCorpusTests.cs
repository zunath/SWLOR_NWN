using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.Lookups;
using SWLOR.Toolset.Domain.GameData.Resources;
using SWLOR.Toolset.Domain.GameData.Tilesets;
using SWLOR.Toolset.Domain.Workspace;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// WP7.1 acceptance gate: every adjacent tile pair in every one of the 438 corpus areas must be
    /// consistent under <see cref="TileAdjacency"/> - shared corners name the same terrain, and
    /// shared edges have compatible crossers. This validates our reading of the SET corner/edge +
    /// orientation rules against real, toolset-authored data BEFORE the WP7.2 matcher relies on it:
    /// if the orientation mapping were wrong, thousands of real adjacencies would appear broken.
    ///
    /// The only tolerated inconsistency is a small, explicitly documented exception derived from the
    /// corpus itself: the <c>fcx01</c> tileset's special "holes" gap terrain abuts cobble terrain
    /// (112 shared corners across the corpus). Every other tileset is perfectly corner-consistent,
    /// and - with the blank-tolerant crosser rule - edge-consistent everywhere. About 16 corpus
    /// areas use base-game (not SWLOR-hak) tilesets, so this gate needs a local NWN install for the
    /// base layer and skips gracefully without one (matching the other full-corpus gates).
    /// </summary>
    public class SetRuleCorpusTests
    {
        private static string RepoRoot
        {
            get
            {
                var c = new DirectoryInfo(AppContext.BaseDirectory);
                while (c != null)
                {
                    if (File.Exists(Path.Combine(c.FullName, "Build", "hakbuilder.json")) &&
                        Directory.Exists(Path.Combine(c.FullName, "SWLOR_Haks")))
                        return c.FullName;
                    c = c.Parent;
                }
                throw new DirectoryNotFoundException("Could not locate the repository root from the test context.");
            }
        }

        private sealed record Mismatch(string Area, string Tileset, int Col, int Row, string Dir, string Kind, string A, string B);

        [Test]
        public void EveryAdjacentTilePair_IsRuleConsistent_ExceptDocumentedExceptions()
        {
            var installPath = NwnInstallLocator.Locate();
            if (installPath == null)
            {
                Assert.Ignore("No local NWN:EE install found; ~16 corpus areas use base-game tilesets, so skipping the SET-rule corpus gate.");
                return;
            }

            var index = ResourceIndex.FromHakBuilderConfig(
                Path.Combine(RepoRoot, "Build", "hakbuilder.json"),
                Path.Combine(RepoRoot, "SWLOR_Haks"),
                KeyBifCatalog.Load(Path.Combine(installPath, "data")));
            var catalog = new TilesetCatalog(index);
            var workspace = new ModuleWorkspace(CorpusLocator.ModuleDirectory);

            var areasChecked = 0;
            var areasUnresolved = 0;
            var cornerCompares = 0;
            var allowedExceptions = 0;
            var unexpected = new List<Mismatch>();

            foreach (var resRef in workspace.EnumerateAreaResRefs())
            {
                var (are, _, _) = workspace.LoadArea(resRef);
                var tsRef = are.Tileset ?? "";
                if (!catalog.TryGetTileset(tsRef, out var ts) || ts == null)
                {
                    areasUnresolved++;
                    continue;
                }

                int w = are.Width ?? 0, h = are.Height ?? 0;
                var tiles = are.Tiles;
                if (w <= 0 || h <= 0 || tiles.Count != w * h)
                    continue;

                areasChecked++;

                int Field(int idx, string name) => tiles[idx].TryGet(name, out var f) ? (int)f.GetInteger() : -1;
                TileDefinition? Def(int idx)
                {
                    var id = Field(idx, "Tile_ID");
                    return id >= 0 && id < ts.Tiles.Count ? ts.Tiles[id] : null;
                }
                int Or(int idx) { var o = Field(idx, "Tile_Orientation"); return o < 0 ? 0 : o; }

                void Check(int ia, int ib, TileEdge edgeA, int col, int row, string dir)
                {
                    var a = Def(ia);
                    var b = Def(ib);
                    if (a == null || b == null)
                        return;

                    int ra = Or(ia), rb = Or(ib);
                    var edgeB = TileAdjacency.OppositeEdge(edgeA);
                    var (nearA, farA) = TileAdjacency.SharedCorners(edgeA);
                    var (nearB, farB) = TileAdjacency.SharedCorners(edgeB);

                    foreach (var (ca, cb) in new[] { (nearA, nearB), (farA, farB) })
                    {
                        cornerCompares++;
                        var ta = TileAdjacency.WorldCornerTerrain(a, ra, ca);
                        var tb = TileAdjacency.WorldCornerTerrain(b, rb, cb);
                        if (TileAdjacency.CornerTerrainsMatch(ta, tb))
                            continue;
                        if (IsDocumentedException(tsRef, ta, tb))
                        {
                            allowedExceptions++;
                            continue;
                        }

                        unexpected.Add(new Mismatch(resRef, tsRef, col, row, dir, "corner", ta, tb));
                    }

                    var ea = TileAdjacency.WorldEdgeCrosser(a, ra, edgeA);
                    var eb = TileAdjacency.WorldEdgeCrosser(b, rb, edgeB);
                    if (!TileAdjacency.EdgeCrossersMatch(ea, eb))
                        unexpected.Add(new Mismatch(resRef, tsRef, col, row, dir, "edge", ea, eb));
                }

                for (var row = 0; row < h; row++)
                for (var col = 0; col < w; col++)
                {
                    var i = row * w + col;
                    if (col + 1 < w) Check(i, i + 1, TileEdge.East, col, row, "E");
                    if (row + 1 < h) Check(i, i + w, TileEdge.North, col, row, "N");
                }
            }

            areasUnresolved.Should().Be(0, "every corpus area's tileset should resolve from the SWLOR haks");
            // A floor rather than an exact count: since WP7.3 the toolset can create areas, so the
            // module is a living corpus that legitimately grows. The real strength of this gate is
            // the cornerCompares floor below plus unexpected == 0, not the area tally.
            areasChecked.Should().BeGreaterThanOrEqualTo(438, "the module corpus has at least the 438 original areas");
            cornerCompares.Should().BeGreaterThan(300_000, "the adjacency model must be genuinely exercised, not trivially satisfied");
            allowedExceptions.Should().BeInRange(100, 130,
                "the documented fcx01 'holes' corner exception should still be present - guards against a dead allowlist");

            unexpected.Should().BeEmpty(
                "every adjacent tile pair must be corner/edge consistent under TileAdjacency (validates the SET orientation rule-reading); unexpected:\n"
                + string.Join("\n", unexpected.Take(40)
                    .Select(m => $"[{m.Tileset}] {m.Area} ({m.Col},{m.Row})->{m.Dir} {m.Kind}: '{m.A}' vs '{m.B}'")));
        }

        /// <summary>
        /// The one corpus-derived exception: the fcx01 tileset's "holes" gap terrain legitimately
        /// abuts cobble terrain (Cobble / Cobble2). Everything else must match exactly.
        /// </summary>
        private static bool IsDocumentedException(string tileset, string a, string b) =>
            string.Equals(tileset, "fcx01", StringComparison.OrdinalIgnoreCase) &&
            (string.Equals(a, "holes", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(b, "holes", StringComparison.OrdinalIgnoreCase));
    }
}
