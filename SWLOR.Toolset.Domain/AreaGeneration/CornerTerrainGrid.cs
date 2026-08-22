#nullable disable
using System;
using System.Collections.Generic;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Corner-granularity terrain plan produced by the macro layout stage.
    /// Labels is (Width+1) x (Height+1), indexed [x, y] with y = 0 at the south (bottom) edge,
    /// matching NWN tile indexing where tile index 0 is the bottom-left tile.
    /// A tile at (tx, ty) touches corners (tx, ty), (tx+1, ty), (tx, ty+1), (tx+1, ty+1).
    /// </summary>
    public class CornerTerrainGrid
    {
        public int Width { get; }
        public int Height { get; }
        public string[,] Labels { get; }

        /// <summary>
        /// Corner-granularity elevation plan, parallel to <see cref="Labels"/> ((Width+1) x
        /// (Height+1), same [x, y] indexing). All-zero by default — no layout style paints this yet;
        /// this grid exists so heights are representable, resolvable (see TileResolver), and emitted
        /// (see AreaSynthesizer/ProcgenReview), independent of the corner-terrain label at that corner
        /// (a corner's identity for matching purposes is the (terrain, height) pair — empirically
        /// confirmed against hand-built areas: every terrain label sampled appears at multiple
        /// heights). A world corner's absolute elevation is Tile_Height (ResolvedTile.Height) plus the
        /// placed tile's own corner height offset at that slot (TileRecord.GetCornerHeightAt) — the
        /// same formula TileOrientationConsistencyTests/HeightResolutionTests pin against real areas.
        /// </summary>
        public int[,] Heights { get; }

        public CornerTerrainGrid(int width, int height, string fillTerrain)
        {
            Width = width;
            Height = height;
            Labels = new string[width + 1, height + 1];
            Heights = new int[width + 1, height + 1];

            for (var x = 0; x <= width; x++)
            {
                for (var y = 0; y <= height; y++)
                {
                    Labels[x, y] = fillTerrain;
                }
            }
        }

        /// <summary>
        /// True when any corner in the grid carries a nonzero height. Cheap (checked once per
        /// resolve, not per cell): TileResolver uses this to decide between the legacy flat-only
        /// candidate lookup (byte-identical to pre-height behavior) and the height-aware lookup, so
        /// every existing all-zero-height caller keeps its exact pools/RNG sequence untouched.
        /// </summary>
        public bool HasAnyHeight()
        {
            for (var x = 0; x <= Width; x++)
            {
                for (var y = 0; y <= Height; y++)
                {
                    if (Heights[x, y] != 0) return true;
                }
            }

            return false;
        }
    }
}
