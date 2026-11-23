using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.SpawnService
{
    /// <summary>
    /// Answers whether a creature can walk a straight line over an area's tile walkmesh, from a grid of samples taken
    /// on demand and kept. A sample is the ground's walkability and height at a cell center; a line is walkable when
    /// every cell it crosses, and the cells beside it, are walkable and the ground never steps more than
    /// <see cref="MaximumStep"/> between neighboring samples. Cave walls are not walkable ground, and cliffs are steps.
    /// </summary>
    public class WalkableGrid
    {
        /// <summary>
        /// Width of a grid cell, in meters.
        /// </summary>
        public const float CellSize = 0.5f;

        /// <summary>
        /// Largest height change, in meters, between two samples a creature walks across.
        /// </summary>
        public const float MaximumStep = 1.0f;

        private const float SampleSpacing = 0.25f;
        private readonly Func<float, float, (bool Walkable, float Height)> _sample;
        private readonly float _width;
        private readonly float _height;
        private readonly Dictionary<(int X, int Y), (bool Walkable, float Height)> _cells = new();

        /// <summary>
        /// Creates a grid over an area.
        /// </summary>
        /// <param name="width">Area width in meters.</param>
        /// <param name="height">Area height in meters.</param>
        /// <param name="sample">Returns the ground's walkability and height at a point inside the area.</param>
        public WalkableGrid(float width, float height, Func<float, float, (bool Walkable, float Height)> sample)
        {
            _width = width;
            _height = height;
            _sample = sample;
        }

        /// <summary>
        /// Returns true when the straight line from (fromX, fromY) to (toX, toY) crosses only walkable ground with
        /// half a cell of walkable ground on either side, and without a step higher than <see cref="MaximumStep"/>.
        /// </summary>
        public bool IsLineWalkable(float fromX, float fromY, float toX, float toY)
        {
            var length = MathF.Sqrt((toX - fromX) * (toX - fromX) + (toY - fromY) * (toY - fromY));
            var steps = Math.Max(1, (int)MathF.Ceiling(length / SampleSpacing));
            float? previousHeight = null;
            for (var step = 0; step <= steps; step++)
            {
                var t = (float)step / steps;
                var x = fromX + (toX - fromX) * t;
                var y = fromY + (toY - fromY) * t;
                var cellX = (int)MathF.Floor(x / CellSize);
                var cellY = (int)MathF.Floor(y / CellSize);

                // The cell and its four neighbors keep a creature's body clear of the edge of the walkable ground.
                if (!IsWalkable(cellX, cellY, out var height) ||
                    !IsWalkable(cellX + 1, cellY, out _) || !IsWalkable(cellX - 1, cellY, out _) ||
                    !IsWalkable(cellX, cellY + 1, out _) || !IsWalkable(cellX, cellY - 1, out _))
                    return false;

                if (previousHeight.HasValue && MathF.Abs(height - previousHeight.Value) > MaximumStep)
                    return false;
                previousHeight = height;
            }

            return true;
        }

        private bool IsWalkable(int cellX, int cellY, out float height)
        {
            height = 0f;
            var x = (cellX + 0.5f) * CellSize;
            var y = (cellY + 0.5f) * CellSize;
            if (x < 0f || y < 0f || x > _width || y > _height)
                return false;

            if (!_cells.TryGetValue((cellX, cellY), out var cell))
                _cells[(cellX, cellY)] = cell = _sample(x, y);

            height = cell.Height;
            return cell.Walkable;
        }
    }
}
