#nullable disable
using System;
using System.Collections.Generic;
using SWLOR.Toolset.Domain.AreaGeneration.Tileset;

namespace SWLOR.Toolset.Domain.AreaGeneration
{
    /// <summary>
    /// Edge-crosser plan for the edges BETWEEN tile cells (and on the map border) of a WxH tile grid.
    /// Complements <see cref="CornerTerrainGrid"/>: where Corners labels the 4 terrain corners a tile
    /// touches, this grid labels the crosser (Corridor, Doorway, Bridge, Fence, Alley, ... or "" for
    /// blank) that must agree between two tiles sharing that edge.
    ///
    /// Storage is split by axis so a shared edge is a single cell, never duplicated:
    /// - Vertical edges (a cell's Left/Right, i.e. edges between horizontally adjacent cells) are
    ///   (Width+1) x Height — index x in [0..Width], x=0 is the map's west border edge, x=Width is
    ///   the east border edge.
    /// - Horizontal edges (a cell's Top/Bottom, i.e. edges between vertically adjacent cells) are
    ///   Width x (Height+1) — index y in [0..Height], y=0 is the map's south border edge, y=Height is
    ///   the north border edge.
    /// Setting cell (x,y)'s Right edge writes the same storage cell as neighbor (x+1,y)'s Left edge.
    /// Everything initializes to "" (blank).
    /// </summary>
    public class EdgeCrosserGrid
    {
        public int Width { get; }
        public int Height { get; }

        // Vertical edges: [x, y] with x in [0..Width]. VerticalEdges[x, y] is cell (x, y)'s Left edge
        // and cell (x-1, y)'s Right edge.
        private readonly string[,] _vertical;

        // Horizontal edges: [x, y] with y in [0..Height]. HorizontalEdges[x, y] is cell (x, y)'s Bottom
        // edge and cell (x, y-1)'s Top edge.
        private readonly string[,] _horizontal;

        public EdgeCrosserGrid(int width, int height)
        {
            Width = width;
            Height = height;

            _vertical = new string[width + 1, height];
            _horizontal = new string[width, height + 1];

            for (var x = 0; x <= width; x++)
            for (var y = 0; y < height; y++)
                _vertical[x, y] = string.Empty;

            for (var x = 0; x < width; x++)
            for (var y = 0; y <= height; y++)
                _horizontal[x, y] = string.Empty;
        }

        public string GetEdge(int cellX, int cellY, int slot)
        {
            switch (slot)
            {
                case EdgeSlot.Left: return _vertical[cellX, cellY];
                case EdgeSlot.Right: return _vertical[cellX + 1, cellY];
                case EdgeSlot.Bottom: return _horizontal[cellX, cellY];
                case EdgeSlot.Top: return _horizontal[cellX, cellY + 1];
                default: throw new ArgumentOutOfRangeException(nameof(slot), slot, "Expected an EdgeSlot value.");
            }
        }

        public void SetEdge(int cellX, int cellY, int slot, string crosser)
        {
            var value = crosser ?? string.Empty;
            switch (slot)
            {
                case EdgeSlot.Left: _vertical[cellX, cellY] = value; break;
                case EdgeSlot.Right: _vertical[cellX + 1, cellY] = value; break;
                case EdgeSlot.Bottom: _horizontal[cellX, cellY] = value; break;
                case EdgeSlot.Top: _horizontal[cellX, cellY + 1] = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(slot), slot, "Expected an EdgeSlot value.");
            }
        }
    }
}
