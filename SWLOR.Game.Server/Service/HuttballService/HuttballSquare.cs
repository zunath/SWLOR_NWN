using System;

namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// A square of the pitch. X runs west to east, Y runs south to north.
    /// </summary>
    public readonly record struct HuttballSquare(int X, int Y)
    {
        /// <summary>
        /// Squares between two squares when moving in any of the eight directions.
        /// </summary>
        public int DistanceTo(HuttballSquare other)
        {
            return Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));
        }

        /// <summary>
        /// Returns true for the eight squares touching this one.
        /// </summary>
        public bool IsAdjacentTo(HuttballSquare other)
        {
            return DistanceTo(other) == 1;
        }

        /// <summary>
        /// Shows the square as 1-based column and row, the way players count them.
        /// </summary>
        public override string ToString()
        {
            return $"({X + 1}, {Y + 1})";
        }
    }
}
