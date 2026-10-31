using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.HuttballService
{
    /// <summary>
    /// The fixed numbers of Huttball: role bonuses, the injury table, and the dice rules the DM ruleset leaves
    /// open (pass and catch target numbers, which way a loose ball bounces).
    /// </summary>
    public static class HuttballRules
    {
        public const int D20 = 20;

        /// <summary>
        /// A shove needs a win by this much on an opposed strength roll.
        /// </summary>
        public const int ShoveMargin = 2;

        /// <summary>
        /// An armour break needs a win by this much on an opposed strength roll.
        /// </summary>
        public const int ArmourBreakMargin = 6;

        /// <summary>
        /// The longest pass, in squares.
        /// </summary>
        public const int PassRange = 4;

        /// <summary>
        /// The agility total a throw or a catch must reach. The ruleset calls for an agility roll without a target
        /// number, so this is the halfway point of a d20.
        /// </summary>
        public const int PassTarget = 10;

        /// <summary>
        /// A dirty move is spotted when the referee rolls below this.
        /// </summary>
        public const int RefereeTarget = 15;

        public const int TurnsPerHalf = 10;
        public const int Halves = 2;
        public const int DefaultBribes = 1;

        /// <summary>
        /// A loose ball bounces at most this many times looking for an empty square before it settles on the
        /// nearest free one.
        /// </summary>
        public const int MaxBounces = 6;

        /// <summary>
        /// The eight directions a loose ball can bounce, one per face of a d8, clockwise from north.
        /// </summary>
        public static readonly IReadOnlyList<HuttballSquare> BounceDirections = new[]
        {
            new HuttballSquare(0, 1),
            new HuttballSquare(1, 1),
            new HuttballSquare(1, 0),
            new HuttballSquare(1, -1),
            new HuttballSquare(0, -1),
            new HuttballSquare(-1, -1),
            new HuttballSquare(-1, 0),
            new HuttballSquare(-1, 1),
        };

        private static readonly Dictionary<HuttballRole, HuttballRoleStats> _stats = new()
        {
            [HuttballRole.Bruiser] = new HuttballRoleStats(5, 0, 3, false),
            [HuttballRole.Catcher] = new HuttballRoleStats(0, 2, 5, false),
            [HuttballRole.Thrower] = new HuttballRoleStats(0, 5, 3, false),
            [HuttballRole.Blocker] = new HuttballRoleStats(5, 0, 2, true),
        };

        public static HuttballRoleStats GetStats(HuttballRole role)
        {
            return _stats[role];
        }

        /// <summary>
        /// Reads a d20 roll on the injury table.
        /// </summary>
        public static HuttballInjury GetInjury(int roll)
        {
            if (roll >= 20)
                return HuttballInjury.Maimed;
            if (roll >= 16)
                return HuttballInjury.SevereInjury;
            if (roll >= 11)
                return HuttballInjury.Injured;

            return HuttballInjury.KnockedDown;
        }

        public static string GetInjuryText(HuttballInjury injury)
        {
            return injury switch
            {
                HuttballInjury.KnockedDown => "Knocked down",
                HuttballInjury.Injured => "Injury! Out until halftime",
                HuttballInjury.SevereInjury => "Severe injury! Out for the match",
                HuttballInjury.Maimed => "Maimed! Out for the match",
                _ => injury.ToString(),
            };
        }

        /// <summary>
        /// Returns the squares a pass flies over between the thrower and the catcher, not counting either end.
        /// </summary>
        public static List<HuttballSquare> GetPassLine(HuttballSquare from, HuttballSquare to)
        {
            var squares = new List<HuttballSquare>();
            var steps = from.DistanceTo(to);
            for (var step = 1; step < steps; step++)
            {
                var t = (float)step / steps;
                var square = new HuttballSquare(
                    (int)MathF.Round(from.X + (to.X - from.X) * t, MidpointRounding.AwayFromZero),
                    (int)MathF.Round(from.Y + (to.Y - from.Y) * t, MidpointRounding.AwayFromZero));
                if (square != from && square != to && !squares.Contains(square))
                    squares.Add(square);
            }

            return squares;
        }

        /// <summary>
        /// Returns the square one step from <paramref name="from"/> directly away from <paramref name="pusher"/>.
        /// </summary>
        public static HuttballSquare GetPushSquare(HuttballSquare pusher, HuttballSquare from)
        {
            return new HuttballSquare(
                from.X + Math.Sign(from.X - pusher.X),
                from.Y + Math.Sign(from.Y - pusher.Y));
        }
    }
}
