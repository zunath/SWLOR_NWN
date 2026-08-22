#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using SWLOR.Toolset.Domain.AreaGeneration.Decoration;

namespace SWLOR.Toolset.Domain.AreaGeneration.Frontage
{
    /// <summary>
    /// Plans the structural BUILDING-PLACEABLE FRONTAGE pass for urban tilesets that declare
    /// <see cref="DungeonTilesetProfile.FrontageBuildings"/>: skyscraper/tower placeables erected
    /// along open-area perimeter edges and street margins so streets and plazas read as canyons
    /// walled by building mass -- the hand-built promenade-family mechanism.
    ///
    /// Evidence (_scratch_decor/r11_mine_buildings.py over pw_ar_narpromena / pw_ar_narscorpd /
    /// pw_ar_nsshipyard): the 12x12 flagship promenade walls its plaza with 30 swd_build*
    /// placeables standing on flat cobble (ZERO building tiles) -- swd_build007 rows at 9.8-10.1m
    /// center pitch (one per 10m tile edge, footprints overlapping laterally into a continuous
    /// wall), 100% cardinal-quantized bearings, centers concentrated on tile boundaries
    /// (x/y mod 10 at 9/0), building NN median 9.9m. Model mix is dominant-plus-accent:
    /// build007 carries ~46-61%% of a wall line with build001/003/004/005/006 and the swd2_elev002
    /// elevator tower as accents (7 distinct models on the 12x12 flagship).
    ///
    /// Placement contract (the walkable-clearance gate):
    ///  - a building's anchor cell is always a NON-WALKABLE margin cell (not a room tile, not a
    ///    stamped structure tile, not a transition/door cell),
    ///  - its face sits <see cref="FaceIntrusion"/> proud of the open-cell boundary, and its
    ///    footprint may penetrate any walkable cell by at most <see cref="MaxOpenIntrusion"/>
    ///    (min-dimension of the overlap rectangle), so no street lane or room interior is ever
    ///    blocked,
    ///  - on a chasm-bearing tileset (<see cref="DungeonTilesetProfile.ChasmTerrains"/>), the
    ///    footprint must satisfy the mined support envelope against the resolved corner-terrain
    ///    plan (see <see cref="FrontageSupportRule"/>) -- a deep model that would hang over the
    ///    visible abyss is rejected for that slot and a shallower fitting model takes it,
    ///  - every placement carries a SUPPORT ANCHOR just inside its fronted open cell
    ///    (<see cref="Decoration.PlannedDecoration.GroundAnchor"/>), so live grounding samples the
    ///    platform surface rather than the chasm floor under the footprint center,
    ///  - bearing = the fronted face's outward normal, cardinal-quantized.
    ///
    /// Composition: occupied margin cells are published as
    /// <see cref="ResolvedLayout.PlaceableStructureCells"/>, so WallFlush cargo and
    /// structure-frontage dressing anchor against placeable buildings exactly as against stamped
    /// tile structures (hand-built narscorpd stacks its flush cargo against swd_build bases). At
    /// 12x12-20x20 this is the primary canyon mechanism; at 24-32 it complements the tile-block
    /// mechanism, walling the margins the tile blocks leave open.
    ///
    /// Determinism: seeded from layout.Seed with pass-local salts, independent of the decoration
    /// planner's main RNG stream, so the existing dressing mechanisms keep their exact sequences.
    /// </summary>
    public static class BuildingFrontagePlanner
    {
        private const float TileSize = 10f;
        private const float TileHalf = 5f;

        /// <summary>How far the building face sits PROUD of the open-cell boundary (into the
        /// walkable cell) -- flush with the cobble edge like the hand-built facades, small enough
        /// that WallFlush cargo (0.4m inside the boundary) and wall-run dressing (1.5m inside)
        /// stand against the face rather than inside it.</summary>
        internal const float FaceIntrusion = 0.6f;

        /// <summary>Maximum min-dimension penetration of a building footprint into any walkable
        /// (room) cell -- the walkable-clearance contract. 2.6m off a 10m tile leaves a 7.4m lane
        /// and never reaches a carved road ribbon's center strip.</summary>
        internal const float MaxOpenIntrusion = 2.6f;

        /// <summary>Maximum min-dimension penetration into a transition/door cell -- door frames
        /// and exit anchors stay visually clear.</summary>
        private const float MaxExcludedIntrusion = 0.5f;

        /// <summary>Min-dimension coverage at which a non-walkable cell under a building footprint
        /// counts as occupied structure (published to PlaceableStructureCells and skipped as a
        /// later frontage anchor).</summary>
        private const float OccupiedCoverage = 3.0f;

        /// <summary>Chance each further slot of a frontage run repeats the run's dominant model
        /// rather than drawing from the accent deck -- hand-built wall lines are dominant-model
        /// runs (build007 x4-5 consecutive) with occasional accents. Retuned 0.7 -> 0.6 in the
        /// round-15 salience pass: with dominants restricted to the three workhorse models
        /// (round-14 rotated the election across the whole pool, so 0.7 spread across many
        /// models), 0.7 concentrated small areas on one workhorse past the hand-built texture --
        /// packed-12 seeds measured model entropy under the mined 2.11-4.15 canyon band and
        /// top-model share past the narpromena flagship's 0.462; at 0.6 the sweep measures
        /// H 2.4-3.7 and top share 0.30-0.62 with the non-workhorse share inside the hand-built
        /// 0.34-0.57 band, keeping the dominant-run wall texture.</summary>
        internal const double DominantShare = 0.6;

        /// <summary>Hard ceiling on CONSECUTIVE same-model placements along one frontage run --
        /// the round-14 variety pass. Mined (_scratch_decor/r14_mine_variety.py over the 24
        /// hand-built fcx01 areas): hand-built collinear building lines top out at same-model runs
        /// of 4 (narpromena/nsshipyard/narcatwalk/randoncity all max 4; narscorpd carries the
        /// single 5 outlier), broken by accent models -- while ungated generated areas ran 6-9
        /// consecutive clones, the reported "same tower repeated along an edge". When the cap is
        /// hit the slot re-rolls among the OTHER fitting models; a slot gap (no fit / occupied)
        /// resets the count exactly as a gap breaks the visual row.</summary>
        internal const int MaxSameModelRun = 4;

        /// <summary>Softening divisor for the per-run DOMINANT pick (round-14): a model's
        /// dominant-pick weight is divided by (1 + timesAlreadyDominant / this), so different
        /// streets rotate toward different dominant models -- the hand-built pattern where
        /// build007 anchors one area's lines but pillr03 the shipyard's and kyru12 the commercial
        /// district's -- while the highest-weight workhorse still leads overall. Tuned against the
        /// mined per-area dominant share (build007 carries 37-46%% of hand-built canyon walls) and
        /// the mined distinct-model band (12-17 models on comparable-mass hand-built areas).
        /// Round 15: only <see cref="BuildingFrontageEntry.DominantEligible"/> workhorse models
        /// enter the election at all -- round-14's rotation could legally elect the neon-clad
        /// build003 as a street dominant and repeat it across the plaza, statistically in band but
        /// visually the reported clone city.</summary>
        internal const double DominantRotationDamping = 3.0;

        /// <summary>Frontage budget ceiling: buildings erected per open floor tile, the round-15
        /// count calibration. Mined (_scratch_decor/r15_mine_salience.py + promenade_benchmark.py
        /// handbuilt over the placeable-canyon fcx01 areas): hand-built per-OPEN-TILE building
        /// density follows the layout's edge/open shape -- plaza-like areas run 0.15-0.39
        /// (narpromena 0.275, nsshipyard 0.392, narscorpd 0.293) while the edge-heaviest
        /// hand-built layout, pw_ar_narcatwalk (edge/open 0.82), runs 0.589 -- and what stays
        /// nearly constant across ALL of them is buildings per open-boundary EDGE (0.70-0.89:
        /// narpromena 0.696, narcatwalk 0.718, nsshipyard 0.750, narscorpd 0.891). Generated
        /// city layouts are edge-heavier than any hand-built city (edge/open 1.2-1.6), so their
        /// fit-limited fill measures 0.23-0.50 per edge -- UNDER the hand rim-coverage band --
        /// while per-open density lands at the narcatwalk pole (the reviewed halls-20 showcase:
        /// 0.590 vs narcatwalk 0.589). Verdict: the erected COUNT was already inside hand-built
        /// practice for edge-heavy layouts; the clone-city perception came from salience
        /// clustering, fixed by the histogram/spacing mechanisms. This ceiling pins the densest
        /// hand-built precedent (+5%% jitter headroom) so no pathological layout can exceed it --
        /// the round-14 12x12 packed showcase measured 0.726, ABOVE every hand-built area, and is
        /// pulled back to the narcatwalk pole. Runs fill longest-first so when the ceiling binds,
        /// plaza rims and main streets keep their full canyon walls and the pruning lands on
        /// short alley stubs.</summary>
        internal const float MaxBuildingsPerOpenTile = 0.62f;

        /// <summary>
        /// Ceiling on the AREA-WIDE non-workhorse (accent) share of placed frontage buildings --
        /// the mined hand-built comparable-mass maximum (r15 salience evidence: hand areas top out
        /// at 0.571 non-workhorse; the perceived clone-city/accent-soup poles both live outside
        /// that band). When an accent draw would push the running share past this ceiling and a
        /// workhorse fits the slot, the highest-weight fitting workhorse takes it instead
        /// (deterministic, no extra RNG draw). Small areas are where this binds: at ~26 buildings
        /// a handful of unlucky accent draws breached 0.65 while the 30-seed sweep's own
        /// distribution sits at 0.42-0.59 -- the guard trims exactly that tail. The same-model
        /// run cap below still applies to the substituted workhorse, so the anti-clone rule wins
        /// when the two conflict.
        /// </summary>
        internal const double MaxNonWorkhorseShare = 0.571;

        /// <summary>Frontage scale-jitter band (see
        /// <see cref="DungeonTilesetProfile.FrontageScaleJitter"/>): subtle enough that footprint
        /// and roofline stay recognizably the model, wide enough that adjacent same-model towers
        /// stop reading as clones. JUDGMENT CALL (documented, not mined). The rolled scale feeds
        /// the footprint used by the walkable-clearance fit check, so the contract holds exactly
        /// for the scaled silhouette.</summary>
        internal const float MinScaleJitter = 0.94f;
        internal const float MaxScaleJitter = 1.08f;

        /// <summary>Wall mounts float this far off the face plane (proud of the face) so sign
        /// panels never z-fight the facade art.</summary>
        internal const float MountProudOffset = 0.35f;

        /// <summary>Ceiling on the per-face-slot chance of hanging a facade mount; the effective
        /// chance is min(this, mount budget / slot count) -- see <see cref="MountBudgetShare"/>.
        /// Wide faces roll a second mount below.</summary>
        internal const double MountChance = 0.85;

        /// <summary>
        /// Facade-mount budget as a fraction of the rest of the plan (ground dressing +
        /// frontage buildings). RECALIBRATED against the elevated-inventory COMPOSITION (July 2026
        /// street-dressing pass): the hand-built dense-city elevated band (0.13-0.23 of
        /// decoratives above Z 0.5m) is dominated by NON-LIT stacked cargo (nsshipyard 319 non-lit
        /// elevated vs 20 lit; narscorpd 225 vs 36; ns_comrcial_ka 92 vs 14) -- lit sign-family
        /// elevated counts run only 8-36 per hand-built area. The original 0.22 budget realized the
        /// whole band from holo signage alone, pushing generated lit-model share (0.31-0.45) past
        /// the hand-built dressed maximum (0.3243, narpromena). 0.115 converges mounts on
        /// ~0.10x the plan while the stacked-cargo layer (see DungeonDecorationEntry.StackHeight /
        /// DungeonDecorationPlanner.StackCargo) supplies the non-lit elevated mass, keeping the
        /// TOTAL elevated share inside the 0.13-0.23 band with a hand-built-composition mix
        /// (measured 0.138-0.146 mean at packed 20/24 with the per-area caps enforced on stacks).
        /// </summary>
        internal const double MountBudgetShare = 0.115;

        /// <summary>Faces at least this wide roll a second, independently-jittered mount.</summary>
        internal const float WideFaceWidth = 18f;

        private const int FrontageSeedSalt = unchecked((int)0x0BF0A11E);
        private const int MountSeedSalt = unchecked((int)0x00FACADE);

        private static readonly (int Dx, int Dy)[] CardinalDirections =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1)
        };

        /// <summary>One planned frontage building plus its face geometry (for the facade-mount
        /// pass and the benchmark's enclosure/alignment metrics).</summary>
        public sealed class FrontagePlacement
        {
            public PlannedDecoration Decoration { get; set; }
            /// <summary>Midpoint of the fronted open/margin boundary edge (world units).</summary>
            public Vector2 FaceCenter { get; set; }
            /// <summary>Outward normal of the fronted face (toward the open cell).</summary>
            public (int Dx, int Dy) Outward { get; set; }
            public float FaceWidth { get; set; }
            public float Depth { get; set; }
            /// <summary>The margin cell the building anchors on.</summary>
            public (int X, int Y) AnchorCell { get; set; }
            /// <summary>Per-instance uniform visual scale applied to this placement (1 = none);
            /// FaceWidth/Depth above are already the SCALED extents.</summary>
            public float Scale { get; set; } = 1f;
        }

        public sealed class FrontageResult
        {
            public List<FrontagePlacement> Placements { get; } = new();
            /// <summary>Non-walkable cells covered by placed building mass (see
            /// <see cref="ResolvedLayout.PlaceableStructureCells"/>).</summary>
            public HashSet<(int X, int Y)> OccupiedCells { get; } = new();
        }

        /// <summary>
        /// Plans the frontage pass. Deterministic for (layout.Seed, tileset). Returns an empty
        /// result when the tileset declares no frontage buildings.
        /// </summary>
        public static FrontageResult PlanFrontage(
            ResolvedLayout layout, DungeonTilesetProfile tileset, HashSet<(int X, int Y)> excluded,
            string roadCrosser)
        {
            var result = new FrontageResult();
            var entries = tileset?.FrontageBuildings;
            if (layout == null || entries == null || entries.Count == 0)
                return result;

            var openCells = new HashSet<(int X, int Y)>();
            var frontableCells = new HashSet<(int X, int Y)>();
            foreach (var room in layout.Rooms)
            foreach (var tile in room.Tiles)
            {
                openCells.Add(tile);
                if (!room.IsSetPiece)
                    frontableCells.Add(tile);
            }

            var stamped = layout.StampedStructureTiles ?? new HashSet<(int X, int Y)>();
            excluded ??= new HashSet<(int X, int Y)>();

            // Candidate anchors: margin cells with at least one cardinal frontable-open neighbor,
            // grouped into runs (shared face direction, same face line, contiguous along it) --
            // the flush-line composition the hand-built canyon walls use.
            var candidates = new List<((int X, int Y) Cell, (int Dx, int Dy) Dir)>();
            for (var y = 0; y < layout.Height; y++)
            for (var x = 0; x < layout.Width; x++)
            {
                var cell = (X: x, Y: y);
                if (openCells.Contains(cell) || stamped.Contains(cell) || excluded.Contains(cell))
                    continue;
                // Road integrity: a margin cell whose own edges carry the road crosser is where a
                // carved lane runs off the walkable grid (toward a transition or the map rim) --
                // a building there would wall the road off. The lane stays clear.
                if (DungeonDecorationPlanner.TileCarriesRoadEdge(cell, layout, roadCrosser))
                    continue;

                foreach (var dir in CardinalDirections)
                {
                    var fronted = (X: x + dir.Dx, Y: y + dir.Dy);
                    if (!frontableCells.Contains(fronted) || excluded.Contains(fronted))
                        continue;

                    candidates.Add((cell, dir));
                }
            }

            // Deterministic run grouping: for a horizontal outward normal the run advances along Y
            // (a vertical face line), and vice versa. Runs fill LONGEST-FIRST (round-15 budget
            // pass): plaza rims and main streets keep their full canyon walls while the budget
            // pruning lands on short alley stubs; ties keep the (direction, face line) order.
            var runs = candidates
                .GroupBy(c => (c.Dir, Line: c.Dir.Dx != 0 ? c.Cell.X : c.Cell.Y))
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key.Dir.Dx).ThenBy(g => g.Key.Dir.Dy).ThenBy(g => g.Key.Line)
                .Select(g => g.OrderBy(c => c.Dir.Dx != 0 ? c.Cell.Y : c.Cell.X).ToList())
                .ToList();

            // Round-15 building budget (see MaxBuildingsPerOpenTile): hand-built dense plaza
            // areas erect at most ~0.39 buildings per open floor tile; the ungated fill-every-slot
            // pass ran up to 0.73.
            var budget = Math.Max(1, (int)Math.Round(frontableCells.Count * MaxBuildingsPerOpenTile));

            var rng = new System.Random(layout.Seed ^ FrontageSeedSalt);
            var workhorsePlaced = 0;
            var usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var familyUsage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var dominantUses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var placedCenters = new Dictionary<string, List<Vector2>>(StringComparer.OrdinalIgnoreCase);

            // Round-15 deal-without-replacement accent deck: non-dominant slots draw the first
            // FITTING entry of a weighted-shuffled deck of the ACCENT pool (non-workhorse
            // entries; workhorse mass comes from the dominant channel), so accents cycle through
            // the pool before any model repeats -- weighted-with-replacement rolls clumped the
            // same accent on nearby slots even under caps. Falls back to the whole pool when no
            // accent fits.
            var accentPool = entries.Where(e => !e.DominantEligible).ToList();
            if (accentPool.Count == 0)
                accentPool = entries;
            var deck = new List<BuildingFrontageEntry>();

            void RefillDeck()
            {
                var remaining = accentPool.ToList();
                while (remaining.Count > 0)
                {
                    var pick = PickWeighted(remaining, rng);
                    remaining.Remove(pick);
                    deck.Add(pick);
                }
            }

            BuildingFrontageEntry DrawFromDeck(List<BuildingFrontageEntry> fitting)
            {
                for (var pass = 0; pass < 2; pass++)
                {
                    for (var i = 0; i < deck.Count; i++)
                    {
                        if (!fitting.Contains(deck[i]))
                            continue;
                        var picked = deck[i];
                        deck.RemoveAt(i);
                        return picked;
                    }

                    if (pass == 0)
                        RefillDeck();
                }

                return PickWeighted(fitting, rng);
            }

            foreach (var run in runs)
            {
                if (result.Placements.Count >= budget)
                    break;

                BuildingFrontageEntry dominant = null;
                string lastResref = null;
                var consecutive = 0;

                foreach (var (cell, dir) in run)
                {
                    if (result.Placements.Count >= budget)
                        break;

                    if (result.OccupiedCells.Contains(cell))
                    {
                        // A hole in the wall line breaks the visual row -- the same-model run
                        // count restarts (see MaxSameModelRun).
                        lastResref = null;
                        consecutive = 0;
                        continue;
                    }

                    // One scale roll per attempted slot (flag-gated; only city families declare
                    // FrontageScaleJitter) so every candidate model is fit-checked against the
                    // same jittered silhouette the placement will render with.
                    var scale = tileset.FrontageScaleJitter
                        ? MinScaleJitter + (float)rng.NextDouble() * (MaxScaleJitter - MinScaleJitter)
                        : 1f;

                    var fitting = entries
                        .Where(e => e.MaxPerArea <= 0 || usage.GetValueOrDefault(e.Resref) < e.MaxPerArea)
                        .Where(e => e.FamilyMaxPerArea <= 0 || string.IsNullOrEmpty(e.FamilyKey) ||
                                    familyUsage.GetValueOrDefault(e.FamilyKey) < e.FamilyMaxPerArea)
                        .Where(e => HasSameModelClearance(e, cell, dir, scale, placedCenters))
                        .Where(e => Fits(e, cell, dir, scale, layout, openCells, stamped, excluded))
                        .Where(e => FrontageSupportRule.IsSupported(
                            Footprint(e, cell, dir, scale), layout, tileset.ChasmTerrains))
                        .ToList();
                    if (fitting.Count == 0)
                    {
                        lastResref = null;
                        consecutive = 0;
                        continue;
                    }

                    if (dominant == null)
                    {
                        // Damped dominant pick (see DominantRotationDamping) among the WORKHORSE
                        // models only (see BuildingFrontageEntry.DominantEligible): different
                        // streets rotate toward different dominants, but a distinctive accent
                        // tower can never anchor a run. A run whose slots never fit a workhorse
                        // stays dominant-less and draws pure accent-deck interleave.
                        var eligible = fitting.Where(e => e.DominantEligible).ToList();
                        if (eligible.Count > 0)
                        {
                            dominant = PickDominant(eligible, rng, dominantUses);
                            dominantUses[dominant.Resref] = dominantUses.GetValueOrDefault(dominant.Resref) + 1;
                        }
                    }

                    var entry = dominant != null && fitting.Contains(dominant) && rng.NextDouble() < DominantShare
                        ? dominant
                        : DrawFromDeck(fitting);

                    // Area-wide salience floor (see MaxNonWorkhorseShare): if this accent draw
                    // would push the placed non-workhorse share past the mined hand-built maximum
                    // and a workhorse fits the slot, the highest-weight fitting workhorse takes it
                    // -- hand-builders keep plain towers in the majority on every comparable-mass
                    // area. Deterministic (no extra RNG); the run cap below still applies.
                    if (!entry.DominantEligible)
                    {
                        var placed = result.Placements.Count;
                        var nonWorkhorsePlaced = placed - workhorsePlaced;
                        if ((nonWorkhorsePlaced + 1) / (double)(placed + 1) > MaxNonWorkhorseShare)
                        {
                            BuildingFrontageEntry bestWorkhorse = null;
                            foreach (var candidate in fitting)
                            {
                                if (candidate.DominantEligible &&
                                    (bestWorkhorse == null || candidate.Weight > bestWorkhorse.Weight))
                                    bestWorkhorse = candidate;
                            }

                            if (bestWorkhorse != null)
                                entry = bestWorkhorse;
                        }
                    }

                    // Same-model run cap (see MaxSameModelRun): at the mined ceiling the slot
                    // re-rolls among the other fitting models -- the hand-built accent interleave.
                    if (entry.Resref.Equals(lastResref, StringComparison.OrdinalIgnoreCase) &&
                        consecutive >= MaxSameModelRun)
                    {
                        var alternatives = fitting
                            .Where(e => !e.Resref.Equals(lastResref, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                        if (alternatives.Count > 0)
                            entry = DrawFromDeck(alternatives);
                    }

                    Place(entry, cell, dir, scale, layout, result);
                    if (entry.DominantEligible)
                        workhorsePlaced++;
                    usage[entry.Resref] = usage.GetValueOrDefault(entry.Resref) + 1;
                    if (!string.IsNullOrEmpty(entry.FamilyKey))
                        familyUsage[entry.FamilyKey] = familyUsage.GetValueOrDefault(entry.FamilyKey) + 1;
                    if (!placedCenters.TryGetValue(entry.Resref, out var centers))
                        placedCenters[entry.Resref] = centers = new List<Vector2>();
                    centers.Add(Center(entry, cell, dir, scale));

                    if (entry.Resref.Equals(lastResref, StringComparison.OrdinalIgnoreCase))
                    {
                        consecutive++;
                    }
                    else
                    {
                        lastResref = entry.Resref;
                        consecutive = 1;
                    }
                }
            }

            return result;
        }

        /// <summary>World-space center of an entry anchored at (cell, dir) with a per-instance
        /// scale: on the face's inward normal at scaledDepth/2 - FaceIntrusion behind the
        /// boundary, so the face stays flush regardless of the rolled scale.</summary>
        private static Vector2 Center(
            BuildingFrontageEntry entry, (int X, int Y) cell, (int Dx, int Dy) dir, float scale)
        {
            var face = FaceCenter(cell, dir);
            var depth = entry.Depth * scale;
            return new Vector2(
                face.X - dir.Dx * (depth / 2f - FaceIntrusion),
                face.Y - dir.Dy * (depth / 2f - FaceIntrusion));
        }

        /// <summary>Axis-aligned footprint rectangle for an entry anchored at (cell, dir) at a
        /// per-instance scale (the SCALED silhouette is what the clearance contract governs).</summary>
        private static (float MinX, float MinY, float MaxX, float MaxY) Footprint(
            BuildingFrontageEntry entry, (int X, int Y) cell, (int Dx, int Dy) dir, float scale)
        {
            var center = Center(entry, cell, dir, scale);
            var depth = entry.Depth * scale;
            var width = entry.FaceWidth * scale;
            var halfX = dir.Dx != 0 ? depth / 2f : width / 2f;
            var halfY = dir.Dx != 0 ? width / 2f : depth / 2f;
            return (center.X - halfX, center.Y - halfY, center.X + halfX, center.Y + halfY);
        }

        /// <summary>Same-model spacing rule (see
        /// <see cref="BuildingFrontageEntry.MinSameModelSpacing"/>): the candidate center must sit
        /// at least the entry's declared distance from every earlier placement of the same model.</summary>
        private static bool HasSameModelClearance(
            BuildingFrontageEntry entry, (int X, int Y) cell, (int Dx, int Dy) dir, float scale,
            Dictionary<string, List<Vector2>> placedCenters)
        {
            if (entry.MinSameModelSpacing <= 0f)
                return true;
            if (!placedCenters.TryGetValue(entry.Resref, out var centers))
                return true;

            var candidate = Center(entry, cell, dir, scale);
            var minSq = entry.MinSameModelSpacing * entry.MinSameModelSpacing;
            foreach (var placed in centers)
            {
                if (Vector2.DistanceSquared(placed, candidate) < minSq)
                    return false;
            }

            return true;
        }

        private static Vector2 FaceCenter((int X, int Y) cell, (int Dx, int Dy) dir)
        {
            return new Vector2(
                cell.X * TileSize + TileHalf + dir.Dx * TileHalf,
                cell.Y * TileSize + TileHalf + dir.Dy * TileHalf);
        }

        /// <summary>Min-dimension of the overlap rectangle between a footprint and one grid cell
        /// (0 when disjoint) -- the penetration metric of the walkable-clearance contract.</summary>
        internal static float CellPenetration(
            (float MinX, float MinY, float MaxX, float MaxY) box, int cellX, int cellY)
        {
            var overlapX = Math.Min(box.MaxX, cellX * TileSize + TileSize) - Math.Max(box.MinX, cellX * TileSize);
            var overlapY = Math.Min(box.MaxY, cellY * TileSize + TileSize) - Math.Max(box.MinY, cellY * TileSize);
            if (overlapX <= 0f || overlapY <= 0f)
                return 0f;
            return Math.Min(overlapX, overlapY);
        }

        private static bool Fits(
            BuildingFrontageEntry entry, (int X, int Y) cell, (int Dx, int Dy) dir, float scale,
            ResolvedLayout layout, HashSet<(int X, int Y)> openCells,
            HashSet<(int X, int Y)> stamped, HashSet<(int X, int Y)> excluded)
        {
            var box = Footprint(entry, cell, dir, scale);
            var minCellX = (int)MathF.Floor(box.MinX / TileSize);
            var maxCellX = (int)MathF.Floor((box.MaxX - 0.01f) / TileSize);
            var minCellY = (int)MathF.Floor(box.MinY / TileSize);
            var maxCellY = (int)MathF.Floor((box.MaxY - 0.01f) / TileSize);

            for (var cy = minCellY; cy <= maxCellY; cy++)
            for (var cx = minCellX; cx <= maxCellX; cx++)
            {
                // Cells beyond the grid are free margin -- hand-built rim buildings overhang the
                // area edge (flagship edge distances 0.2-0.8m).
                if (cx < 0 || cy < 0 || cx >= layout.Width || cy >= layout.Height)
                    continue;

                var penetration = CellPenetration(box, cx, cy);
                if (penetration <= 0f)
                    continue;

                if (openCells.Contains((cx, cy)) && penetration > MaxOpenIntrusion)
                    return false;
                if (excluded.Contains((cx, cy)) && penetration > MaxExcludedIntrusion)
                    return false;
                if (stamped.Contains((cx, cy)) && penetration > MaxOpenIntrusion)
                    return false;
            }

            return true;
        }

        private static void Place(
            BuildingFrontageEntry entry, (int X, int Y) cell, (int Dx, int Dy) dir, float scale,
            ResolvedLayout layout, FrontageResult result)
        {
            var face = FaceCenter(cell, dir);
            var center = Center(entry, cell, dir, scale);

            // SUPPORT ANCHOR (see PlannedDecoration.GroundAnchor): grounding must sample the
            // platform the face stands flush with, not the footprint center -- a deep tower's
            // center hangs over the margin, and on a chasm-bearing tileset the ground there is the
            // chasm floor far below. 1m inside the fronted open cell is always real platform
            // walkmesh (past the 0.6m face intrusion, well short of any opposite boundary).
            var anchor = new Vector2(face.X + dir.Dx * 1f, face.Y + dir.Dy * 1f);
            var fronted = (X: cell.X + dir.Dx, Y: cell.Y + dir.Dy);
            var groundZ = 0f;
            if (layout != null &&
                fronted.X >= 0 && fronted.X < layout.Width && fronted.Y >= 0 && fronted.Y < layout.Height)
            {
                groundZ = layout.GetTile(fronted.X, fronted.Y).Height * layout.HeightTransition;
            }

            result.Placements.Add(new FrontagePlacement
            {
                Decoration = new PlannedDecoration
                {
                    Resref = entry.Resref,
                    Position = new Vector3(center.X, center.Y, 0f),
                    Facing = DungeonDecorationPlanner.CardinalFacing(dir.Dx, dir.Dy),
                    Context = DecorationContext.BuildingFrontage,
                    VisualScale = scale,
                    GroundAnchor = anchor,
                    GroundZ = groundZ
                },
                FaceCenter = face,
                Outward = dir,
                FaceWidth = entry.FaceWidth * scale,
                Depth = entry.Depth * scale,
                AnchorCell = cell,
                Scale = scale
            });

            // Publish covered margin cells as structure so flush cargo/frontage dressing anchor
            // against the facade and later runs cannot double-place on them.
            var box = Footprint(entry, cell, dir, scale);
            var minCellX = (int)MathF.Floor(box.MinX / TileSize);
            var maxCellX = (int)MathF.Floor((box.MaxX - 0.01f) / TileSize);
            var minCellY = (int)MathF.Floor(box.MinY / TileSize);
            var maxCellY = (int)MathF.Floor((box.MaxY - 0.01f) / TileSize);
            for (var cy = minCellY; cy <= maxCellY; cy++)
            for (var cx = minCellX; cx <= maxCellX; cx++)
            {
                if (cx < 0 || cy < 0 || cx >= layout.Width || cy >= layout.Height)
                    continue;

                if (CellPenetration(box, cx, cy) >= OccupiedCoverage)
                    result.OccupiedCells.Add((cx, cy));
            }

            result.OccupiedCells.Add(cell);
        }

        /// <summary>
        /// Plans the wall-mounted facade dressing pass: sign/holo placeables hung on building
        /// faces -- every placed frontage building's face plus every stamped structure tile face
        /// that fronts an open room cell -- at the entry's mined height band, slightly proud of
        /// the face, bearing = the face's outward normal. See
        /// <see cref="DungeonTilesetProfile.FacadeMounts"/>.
        /// </summary>
        public static List<PlannedDecoration> PlanFacadeMounts(
            ResolvedLayout layout, DungeonTilesetProfile tileset, FrontageResult frontage,
            int restOfPlanCount)
        {
            var plan = new List<PlannedDecoration>();
            var entries = tileset?.FacadeMounts;
            if (layout == null || entries == null || entries.Count == 0)
                return plan;

            // Every mountable face slot, in deterministic order: frontage-building faces first
            // (placement order), then stamped tile-structure faces fronting open room cells
            // (cell order). Collected up front so the budgeted per-slot chance (see
            // MountBudgetShare) is known before any RNG draw.
            var slots = new List<(Vector2 FaceCenter, (int Dx, int Dy) Outward, float FaceWidth, float Proud)>();
            if (frontage != null)
            {
                foreach (var placement in frontage.Placements)
                    slots.Add((placement.FaceCenter, placement.Outward, placement.FaceWidth,
                        FaceIntrusion + MountProudOffset));
            }

            var stamped = layout.StampedStructureTiles;
            if (stamped is { Count: > 0 })
            {
                var openCells = new HashSet<(int X, int Y)>();
                foreach (var room in layout.Rooms)
                {
                    if (room.IsSetPiece)
                        continue;
                    foreach (var tile in room.Tiles)
                        openCells.Add(tile);
                }

                foreach (var cell in stamped.OrderBy(c => c.Y).ThenBy(c => c.X))
                foreach (var dir in CardinalDirections)
                {
                    if (!openCells.Contains((cell.X + dir.Dx, cell.Y + dir.Dy)))
                        continue;

                    slots.Add((FaceCenter(cell, dir), dir, TileSize, MountProudOffset));
                }
            }

            var opportunities = slots.Sum(s => s.FaceWidth >= WideFaceWidth ? 2 : 1);
            if (opportunities == 0)
                return plan;

            var budget = MountBudgetShare * Math.Max(restOfPlanCount, 0);
            var chance = Math.Min(MountChance, budget / opportunities);
            if (chance <= 0.0)
                return plan;

            var rng = new System.Random(layout.Seed ^ MountSeedSalt);
            foreach (var slot in slots)
                MountOnFace(plan, entries, slot.FaceCenter, slot.Outward, slot.FaceWidth, slot.Proud, chance, rng);

            return plan;
        }

        private static void MountOnFace(
            List<PlannedDecoration> plan, List<FacadeMountEntry> entries,
            Vector2 faceCenter, (int Dx, int Dy) outward, float faceWidth, float proud,
            double chance, System.Random rng)
        {
            var mounts = faceWidth >= WideFaceWidth ? 2 : 1;
            for (var i = 0; i < mounts; i++)
            {
                if (rng.NextDouble() >= chance)
                    continue;

                var entry = PickWeighted(entries, rng);
                var jitterRange = Math.Max(0f, faceWidth / 2f - 1.5f);
                var jitter = (float)(rng.NextDouble() * 2.0 - 1.0) * Math.Min(3.5f, jitterRange);
                var alongX = outward.Dx == 0 ? 1f : 0f;
                var alongY = outward.Dx == 0 ? 0f : 1f;
                var z = entry.MinHeight + (float)rng.NextDouble() * (entry.MaxHeight - entry.MinHeight);

                plan.Add(new PlannedDecoration
                {
                    Resref = entry.Resref,
                    Position = new Vector3(
                        faceCenter.X + outward.Dx * proud + alongX * jitter,
                        faceCenter.Y + outward.Dy * proud + alongY * jitter,
                        z),
                    Facing = DungeonDecorationPlanner.CardinalFacing(outward.Dx, outward.Dy),
                    Context = DecorationContext.FacadeMount
                });
            }
        }

        /// <summary>Weighted dominant pick with rotation damping (see
        /// <see cref="DominantRotationDamping"/>): a model already dominant on earlier runs picks
        /// with weight / (1 + uses / damping), rotating dominants across streets while the
        /// heaviest workhorse still leads overall.</summary>
        private static BuildingFrontageEntry PickDominant(
            List<BuildingFrontageEntry> entries, System.Random rng, Dictionary<string, int> dominantUses)
        {
            var weights = new double[entries.Count];
            var total = 0.0;
            for (var i = 0; i < entries.Count; i++)
            {
                weights[i] = entries[i].Weight /
                             (1.0 + dominantUses.GetValueOrDefault(entries[i].Resref) / DominantRotationDamping);
                total += weights[i];
            }

            if (total <= 0.0)
                return entries[0];

            var roll = rng.NextDouble() * total;
            var cumulative = 0.0;
            for (var i = 0; i < entries.Count; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative)
                    return entries[i];
            }

            return entries[^1];
        }

        private static BuildingFrontageEntry PickWeighted(List<BuildingFrontageEntry> entries, System.Random rng)
        {
            var total = entries.Sum(e => e.Weight);
            if (total <= 0)
                return entries[0];

            var roll = rng.Next(total);
            var cumulative = 0;
            foreach (var entry in entries)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                    return entry;
            }

            return entries[^1];
        }

        private static FacadeMountEntry PickWeighted(List<FacadeMountEntry> entries, System.Random rng)
        {
            var total = entries.Sum(e => e.Weight);
            if (total <= 0)
                return entries[0];

            var roll = rng.Next(total);
            var cumulative = 0;
            foreach (var entry in entries)
            {
                cumulative += entry.Weight;
                if (roll < cumulative)
                    return entry;
            }

            return entries[^1];
        }
    }
}
