using System;
using System.Numerics;

namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// Where a Huttball pitch lies in the area: a fixed-size pitch laid over the battle grid's squares around its
    /// center, captured when the match was created so moving the grid later does not move the squares under the
    /// players.
    /// </summary>
    public readonly record struct HuttballPitch(Vector3 Center, float SquareSize, int Width, int Length)
    {
        /// <summary>
        /// Lays a <see cref="HuttballRules.PitchWidth"/> x <see cref="HuttballRules.PitchLength"/> pitch over a
        /// battle grid, one Huttball square per grid square. Even a one-section grid (20 x 20) holds it.
        /// </summary>
        public static HuttballPitch FromGrid(Vector3 center, BattleGridSettings settings)
        {
            return At(center, settings.SquareSize);
        }

        /// <summary>
        /// Lays the pitch around a point with no battle grid, as when a player captain sets up a match where they
        /// stand.
        /// </summary>
        public static HuttballPitch At(Vector3 center, float squareSize)
        {
            return new HuttballPitch(center, squareSize, HuttballRules.PitchWidth, HuttballRules.PitchLength);
        }

        /// <summary>
        /// True when the pitch, its flags, and both benches lie inside an area of the given size in meters.
        /// </summary>
        public bool FitsWithin(float areaWidth, float areaHeight)
        {
            var southWest = GetPoint(-4f, -2f);
            var northEast = GetPoint(Width + 4f, Length + 2f);
            return southWest.X >= 0f && southWest.Y >= 0f && northEast.X <= areaWidth && northEast.Y <= areaHeight;
        }

        // The grid's center sits on a square corner, so the pitch edges stay on grid lines even when a side has
        // an odd number of squares.
        private float West => Center.X - Width / 2 * SquareSize;
        private float South => Center.Y - Length / 2 * SquareSize;

        public Vector3 GetSquareCenter(HuttballSquare square)
        {
            return GetPoint(square.X + 0.5f, square.Y + 0.5f);
        }

        /// <summary>
        /// Returns the world point at a position measured in squares from the pitch's south-west corner, so 0 is
        /// the west or south edge and <see cref="Width"/> or <see cref="Length"/> the far edge.
        /// </summary>
        public Vector3 GetPoint(float squaresEast, float squaresNorth)
        {
            return new Vector3(West + squaresEast * SquareSize, South + squaresNorth * SquareSize, Center.Z);
        }

        /// <summary>
        /// Returns the square under a position. Positions off the pitch give squares off the pitch.
        /// </summary>
        public HuttballSquare GetSquare(Vector3 position)
        {
            return new HuttballSquare(
                (int)MathF.Floor((position.X - West) / SquareSize),
                (int)MathF.Floor((position.Y - South) / SquareSize));
        }

        /// <summary>
        /// A bench spot beside the pitch, clear of the sideline flags: west of it for Home, east of it for Away,
        /// one per bench slot.
        /// </summary>
        public Vector3 GetBenchPosition(HuttballTeam team, int slot)
        {
            var column = team == HuttballTeam.Home ? -3 : Width + 2;
            var row = Length / 2 - 3 + slot % Math.Max(1, Length);
            return GetSquareCenter(new HuttballSquare(column, row));
        }
    }
}
