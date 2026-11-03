using System.Collections.Generic;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service.SpawnService
{
    /// <summary>
    /// Answers whether any object lies within a fixed radius of a point, so random spawn points keep clear of objects.
    /// Positions are bucketed into square cells as wide as the radius, so each query looks at nine cells.
    /// </summary>
    public class SpawnClearanceGrid
    {
        private readonly float _radius;
        private readonly Dictionary<(int X, int Y), List<Vector3>> _cells = new();

        /// <summary>
        /// Creates a grid of object positions.
        /// </summary>
        /// <param name="objectPositions">Positions of the objects spawns must keep clear of.</param>
        /// <param name="radius">Clearance radius in meters, measured in three dimensions.</param>
        public SpawnClearanceGrid(IEnumerable<Vector3> objectPositions, float radius)
        {
            _radius = radius;
            foreach (var position in objectPositions)
            {
                var cell = GetCell(position);
                if (!_cells.TryGetValue(cell, out var positions))
                    _cells[cell] = positions = new List<Vector3>();
                positions.Add(position);
            }
        }

        /// <summary>
        /// Returns true when an object lies within the radius of the point. Pass the point at ground height:
        /// measuring from Z 0 shrinks the radius wherever the floor is raised.
        /// </summary>
        /// <param name="point">The candidate spawn point at ground height.</param>
        public bool HasObjectWithin(Vector3 point)
        {
            var (cellX, cellY) = GetCell(point);
            var radiusSquared = _radius * _radius;
            for (var x = cellX - 1; x <= cellX + 1; x++)
            {
                for (var y = cellY - 1; y <= cellY + 1; y++)
                {
                    if (!_cells.TryGetValue((x, y), out var positions))
                        continue;

                    foreach (var position in positions)
                    {
                        if (Vector3.DistanceSquared(position, point) <= radiusSquared)
                            return true;
                    }
                }
            }

            return false;
        }

        private (int X, int Y) GetCell(Vector3 position) =>
            ((int)MathF.Floor(position.X / _radius), (int)MathF.Floor(position.Y / _radius));
    }
}
