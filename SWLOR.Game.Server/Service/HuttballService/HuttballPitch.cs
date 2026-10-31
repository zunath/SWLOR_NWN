using System;
using System.Numerics;

namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// Where a Huttball pitch lies in the area: the battle grid it was laid on, captured when the match was created
    /// so moving the grid later does not move the squares under the players.
    /// </summary>
    public readonly record struct HuttballPitch(Vector3 Center, float SquareSize, int Width, int Length)
    {
        /// <summary>
        /// Lays a pitch over a battle grid, one Huttball square per grid square.
        /// </summary>
        public static HuttballPitch FromGrid(Vector3 center, BattleGridSettings settings)
        {
            return new HuttballPitch(
                center,
                settings.SquareSize,
                settings.SectionsWide * BattleGrid.SquaresPerSection,
                settings.SectionsLong * BattleGrid.SquaresPerSection);
        }

        private float West => Center.X - Width * SquareSize / 2f;
        private float South => Center.Y - Length * SquareSize / 2f;

        public Vector3 GetSquareCenter(HuttballSquare square)
        {
            return new Vector3(
                West + (square.X + 0.5f) * SquareSize,
                South + (square.Y + 0.5f) * SquareSize,
                Center.Z);
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
        /// A bench spot beside the pitch: west of it for Home, east of it for Away, one per bench slot.
        /// </summary>
        public Vector3 GetBenchPosition(HuttballTeam team, int slot)
        {
            var column = team == HuttballTeam.Home ? -2 : Width + 1;
            var row = Length / 2 - 3 + slot % Math.Max(1, Length);
            return GetSquareCenter(new HuttballSquare(column, row));
        }
    }
}
