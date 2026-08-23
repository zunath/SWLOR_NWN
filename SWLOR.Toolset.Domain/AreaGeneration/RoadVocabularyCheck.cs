#nullable disable
using System;
using System.Linq;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Whole-tileset capability probe for LayoutRoadCarver: true only when the tileset can resolve
    /// every tile SHAPE a street lane can unconditionally emit, mirroring TunnelVocabularyCheck/
    /// LayoutAccentChannelCarver.CanCarve/LayoutFenceCarver's own "probe TileResolver.HasCandidate for
    /// the exact shapes, don't just check crosser-name presence" pattern.
    ///
    /// Unlike a Fence/Bridge/Corridor line, a road never repaints corner terrain or blocks movement --
    /// it is a flat, fully-open-cornered (pathnode A) tile carrying the road crosser on some subset of
    /// its four edges (verified against fcx01's real .set data: TILE207-211/212-216, all four corners
    /// uniformly Cobble/Cobble2, PathNode=A). Because TileResolver's rotation search tries all four
    /// orientations of every candidate, one representative orientation of each of the five possible
    /// nonzero edge-subset SHAPES stands in for every rotation of it a carved lane could ever need:
    ///   - stub     [-,R,-,-]        -- REQUIRED: a lane's dead-end cell (only the edge facing back
    ///     into the lane carries the crosser) -- fcx01's x05 (TILE211/216).
    ///   - straight [-,R,-,L]        -- REQUIRED: the un-bent case -- fcx01's x04 (TILE210/215).
    ///   - turn     [T,-,-,L]        -- REQUIRED: an L-shaped lane's corner cell -- fcx01's x01
    ///     (TILE207/212).
    ///   - T        [T,R,-,L]        -- REQUIRED: two lanes merging at a junction -- fcx01's x02
    ///     (TILE208/213).
    ///   - X        [T,R,B,L]        -- REQUIRED: two lanes crossing -- fcx01's x03 (TILE209/214).
    /// Every non-empty subset of {Top,Right,Bottom,Left} a lane's own edge-writing could ever produce
    /// is one of these five shapes under some rotation (1-edge subsets are all stub rotations, 2-edge
    /// adjacent subsets are all turn rotations, 2-edge opposite subsets are all straight rotations,
    /// 3-edge subsets are all T rotations, and the 4-edge subset is X), so these five checks are a
    /// complete, not partial, coverage probe.
    /// </summary>
    public static class RoadVocabularyCheck
    {
        /// <summary>
        /// True when <paramref name="tileset"/> can resolve every shape a street lane carved across
        /// <paramref name="openTerrain"/> using <paramref name="roadCrosser"/> needs. Returns false
        /// immediately (no probing) when <paramref name="roadCrosser"/> is empty -- the "tileset never
        /// declared road vocabulary" default.
        /// </summary>
        public static bool SupportsRoads(TilesetModel tileset, string openTerrain, string roadCrosser)
        {
            if (tileset == null) throw new ArgumentNullException(nameof(tileset));
            if (string.IsNullOrEmpty(openTerrain) || string.IsNullOrEmpty(roadCrosser)) return false;

            if (!tileset.Crossers.Contains(roadCrosser, StringComparer.OrdinalIgnoreCase)) return false;

            bool Shape(string top, string right, string bottom, string left) =>
                TileResolver.HasCandidate(tileset, openTerrain, openTerrain, openTerrain, openTerrain, top, right, bottom, left);

            if (!Shape("", roadCrosser, "", "")) return false; // stub
            if (!Shape("", roadCrosser, "", roadCrosser)) return false; // straight
            if (!Shape(roadCrosser, "", "", roadCrosser)) return false; // turn
            if (!Shape(roadCrosser, roadCrosser, "", roadCrosser)) return false; // T
            if (!Shape(roadCrosser, roadCrosser, roadCrosser, roadCrosser)) return false; // X

            return true;
        }
    }
}
