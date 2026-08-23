#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration.Frontage
{
    /// <summary>
    /// Footprint-support rule for frontage buildings on chasm-bearing tilesets (see
    /// <see cref="DungeonTilesetProfile.ChasmTerrains"/> -- fcx01's "holes"): a candidate
    /// building's footprint must stand on real platform surface the way every hand-built
    /// platform-level tower does, instead of hanging over the visible abyss.
    ///
    /// PLATFORM MODEL (identical to the offline support audit): the resolved corner-terrain plan
    /// (<see cref="ResolvedLayout.CornerTerrains"/>) partitions the grid into 5x5m corner-owned
    /// quadrants -- the square [cx*10-5, cy*10-5, cx*10+5, cy*10+5] clipped to the grid belongs to
    /// corner (cx, cy) -- and a quadrant is PLATFORM iff its corner label is not a declared chasm
    /// terrain. Space beyond the grid is EXEMPT (not void): hand-built rim towers hang off the
    /// area edge freely (off-grid footprint shares up to 0.58 on the flagship family) because
    /// there is no visible drop past the edge, only skyline.
    ///
    /// ENVELOPE (street-coherence rule): buildings must be EFFECTIVELY
    /// FULLY platform-supported -- total in-grid chasm share (interior abyss AND the map-edge
    /// moat, i.e. chasm quadrants owned by grid-border corners) at most
    /// <see cref="MaxChasmShare"/> = 0.05, overhang at most <see cref="MaxChasmOverhang"/> = 2m,
    /// and NO free map-edge moat allowance. Only space fully beyond the grid stays exempt (there
    /// is nothing rendered past the area edge, only skyline).
    ///
    /// FOR THE RECORD -- the previously mined hand-built envelope this override replaces
    /// (r16_mine_support.py, 476 building placeables across the 19 hand-built fcx01 city areas,
    /// platform-level frontage-pool population n=280):
    ///  - in-grid chasm footprint share: median 0, p90 0.050, p95 0.125, p98 0.253, p99 0.458;
    ///    interior ceiling was 0.36 (admitting the narcatwalk chasm-lip build003 at 0.356), with
    ///    a separate 0.50 TOTAL ceiling exempting the map-edge moat (pw_ar_nsshipyard's rim
    ///    towers at 0.458-0.493).
    ///  - in-grid chasm overhang: median 0, p90 0.9m, p95 5.0m, p98 8.7m; ceiling was 9.0m
    ///    (admitting the narcatwalk build007 chasm-lip rows at 6.2-7.2m).
    /// Those tolerances were statistically hand-faithful but the DELIVERED result still read as
    /// "buildings hanging off the side" in user review (rim tanks and a wedge tower over the
    /// moat) -- design authority overrode the mined numbers with the near-zero envelope above.
    /// Note the new 0.05/2m gates sit at the mined distribution's p90, so the overwhelming
    /// majority of hand placements still conform; what is dropped is the hand-built rim-overhang
    /// PRACTICE, deliberately. Enclosure is preserved instead by
    /// LayoutPlatformApronPainter.ApronDepth paving real platform under the frontage band.
    /// </summary>
    public static class FrontageSupportRule
    {
        private const float TileSize = 10f;
        private const float QuadrantHalf = 5f;

        /// <summary>Maximum fraction of a building footprint over ANY in-grid chasm quadrant --
        /// interior abyss and map-edge moat alike (user override; see the class doc
        /// comment).</summary>
        public const float MaxChasmShare = 0.05f;

        /// <summary>Maximum distance (m) any in-grid footprint point may sit from the nearest
        /// platform quadrant (user override; see the class doc comment).</summary>
        public const float MaxChasmOverhang = 2.0f;

        /// <summary>
        /// True when the footprint satisfies the support envelope, or when the layout carries
        /// no corner semantics / the tileset declares no chasm terrain (rule inactive -- every
        /// non-chasm family keeps its exact previous behavior).
        /// </summary>
        public static bool IsSupported(
            (float MinX, float MinY, float MaxX, float MaxY) box,
            ResolvedLayout layout,
            IReadOnlyList<string> chasmTerrains)
        {
            if (layout?.CornerTerrains == null || chasmTerrains == null || chasmTerrains.Count == 0)
                return true;

            var (_, totalShare, overhang) = Evaluate(box, layout, chasmTerrains);
            return totalShare <= MaxChasmShare &&
                   overhang <= MaxChasmOverhang;
        }

        /// <summary>
        /// Computes (interior in-grid chasm share, total in-grid chasm share, max in-grid chasm
        /// overhang) for a world-space footprint rectangle. Exposed for tests and the planner;
        /// pure geometry, no RNG.
        /// </summary>
        public static (float InteriorChasmShare, float TotalChasmShare, float Overhang) Evaluate(
            (float MinX, float MinY, float MaxX, float MaxY) box,
            ResolvedLayout layout,
            IReadOnlyList<string> chasmTerrains)
        {
            var corners = layout.CornerTerrains;
            var gridMaxX = layout.Width * TileSize;
            var gridMaxY = layout.Height * TileSize;

            var area = (box.MaxX - box.MinX) * (box.MaxY - box.MinY);
            if (area <= 0f)
                return (0f, 0f, 0f);

            // Corner-owned quadrants are pairwise disjoint (they are the corner-ownership Voronoi
            // cells of the grid), so summing per-quadrant overlaps is exact. Chasm quadrants owned
            // by grid-BORDER corners are the map-edge moat -- still reported separately in the
            // interior share for the audit record, but the GATE reads the total (the moat
            // exemption was removed by user override; see the class doc comment).
            var minCx = Math.Max(0, (int)MathF.Floor((box.MinX - QuadrantHalf) / TileSize));
            var maxCx = Math.Min(layout.Width, (int)MathF.Ceiling((box.MaxX + QuadrantHalf) / TileSize));
            var minCy = Math.Max(0, (int)MathF.Floor((box.MinY - QuadrantHalf) / TileSize));
            var maxCy = Math.Min(layout.Height, (int)MathF.Ceiling((box.MaxY + QuadrantHalf) / TileSize));

            var supported = 0f;
            var moat = 0f;
            for (var cx = minCx; cx <= maxCx; cx++)
            for (var cy = minCy; cy <= maxCy; cy++)
            {
                if (IsChasmCorner(corners.Labels[cx, cy], chasmTerrains))
                {
                    var isBorder = cx == 0 || cy == 0 || cx == layout.Width || cy == layout.Height;
                    if (isBorder)
                        moat += OverlapArea(box, QuadrantRect(cx, cy, gridMaxX, gridMaxY));
                    continue;
                }

                supported += OverlapArea(box, QuadrantRect(cx, cy, gridMaxX, gridMaxY));
            }

            var inGrid = OverlapArea(box, (0f, 0f, gridMaxX, gridMaxY));
            var chasmArea = Math.Max(0f, inGrid - supported);
            var totalShare = chasmArea / area;
            var interiorShare = Math.Max(0f, chasmArea - moat) / area;
            if (chasmArea <= 0.01f)
                return (interiorShare, totalShare, 0f);

            // Max in-grid overhang: 1m sample grid over the in-grid part of the footprint, exact
            // rectangle distance to the nearest platform quadrant, searched over expanding corner
            // rings from the sample's own cell.
            var overhang = 0f;
            var clipped = (
                MinX: Math.Max(box.MinX, 0f), MinY: Math.Max(box.MinY, 0f),
                MaxX: Math.Min(box.MaxX, gridMaxX), MaxY: Math.Min(box.MaxY, gridMaxY));
            var nx = Math.Max(2, (int)(clipped.MaxX - clipped.MinX) + 1);
            var ny = Math.Max(2, (int)(clipped.MaxY - clipped.MinY) + 1);
            for (var ix = 0; ix <= nx; ix++)
            {
                var px = clipped.MinX + (clipped.MaxX - clipped.MinX) * ix / nx;
                for (var iy = 0; iy <= ny; iy++)
                {
                    var py = clipped.MinY + (clipped.MaxY - clipped.MinY) * iy / ny;
                    var d = DistanceToPlatform(px, py, layout, chasmTerrains, gridMaxX, gridMaxY);
                    if (d > overhang)
                        overhang = d;
                }
            }

            return (interiorShare, totalShare, overhang);
        }

        private static bool IsChasmCorner(string label, IReadOnlyList<string> chasmTerrains)
        {
            if (label == null)
                return true;
            for (var i = 0; i < chasmTerrains.Count; i++)
            {
                if (string.Equals(label, chasmTerrains[i], StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static (float MinX, float MinY, float MaxX, float MaxY) QuadrantRect(
            int cx, int cy, float gridMaxX, float gridMaxY)
        {
            return (
                Math.Max(cx * TileSize - QuadrantHalf, 0f),
                Math.Max(cy * TileSize - QuadrantHalf, 0f),
                Math.Min(cx * TileSize + QuadrantHalf, gridMaxX),
                Math.Min(cy * TileSize + QuadrantHalf, gridMaxY));
        }

        private static float OverlapArea(
            (float MinX, float MinY, float MaxX, float MaxY) a,
            (float MinX, float MinY, float MaxX, float MaxY) b)
        {
            var w = Math.Min(a.MaxX, b.MaxX) - Math.Max(a.MinX, b.MinX);
            var h = Math.Min(a.MaxY, b.MaxY) - Math.Max(a.MinY, b.MinY);
            return w > 0f && h > 0f ? w * h : 0f;
        }

        private static float RectDistance(
            (float MinX, float MinY, float MaxX, float MaxY) rect, float px, float py)
        {
            var dx = Math.Max(Math.Max(rect.MinX - px, 0f), px - rect.MaxX);
            var dy = Math.Max(Math.Max(rect.MinY - py, 0f), py - rect.MaxY);
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        private static float DistanceToPlatform(
            float px, float py, ResolvedLayout layout, IReadOnlyList<string> chasmTerrains,
            float gridMaxX, float gridMaxY)
        {
            var corners = layout.CornerTerrains;
            var ccx = (int)MathF.Round(px / TileSize);
            var ccy = (int)MathF.Round(py / TileSize);
            var maxRing = Math.Max(layout.Width, layout.Height) + 1;

            var best = float.MaxValue;
            for (var ring = 2; ring <= maxRing; ring *= 2)
            {
                for (var cx = Math.Max(0, ccx - ring); cx <= Math.Min(layout.Width, ccx + ring); cx++)
                for (var cy = Math.Max(0, ccy - ring); cy <= Math.Min(layout.Height, ccy + ring); cy++)
                {
                    if (IsChasmCorner(corners.Labels[cx, cy], chasmTerrains))
                        continue;
                    var d = RectDistance(QuadrantRect(cx, cy, gridMaxX, gridMaxY), px, py);
                    if (d < best)
                        best = d;
                }

                // The searched window covers every corner within (ring - 1) tiles; a hit closer
                // than that bound cannot be beaten by a corner outside the window.
                if (best <= (ring - 1) * TileSize || ring >= maxRing)
                    break;
            }

            return best == float.MaxValue ? 999f : best;
        }
    }
}
