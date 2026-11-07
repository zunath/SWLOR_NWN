using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The decisions of the NPC ship role behaviors (plan section 11): who each role targets, when it acts and
    /// what it does. Pure functions, no engine calls; <see cref="Space"/> runs them on the ship AI tick with
    /// the numbers in <see cref="ShipNpcRoleTuning"/>.
    /// </summary>
    public static class ShipNpcRoleRules
    {
        /// <summary>The role named by a preset's Role text ("Electronic Warfare", "Gunship" and the rest).</summary>
        public static ShipNpcRole ParseRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return ShipNpcRole.Invalid;

            return Enum.TryParse<ShipNpcRole>(role.Replace(" ", string.Empty), true, out var parsed) &&
                   Enum.IsDefined(parsed)
                ? parsed
                : ShipNpcRole.Invalid;
        }

        /// <summary>Whether an action last taken at <paramref name="last"/> may run again.</summary>
        public static bool IsReady(DateTime now, DateTime last, double cooldownSeconds)
        {
            return now >= last.AddSeconds(cooldownSeconds);
        }

        /// <summary>
        /// The tank's target: the enemy holding the most threat across its squad's threat tables (the player doing
        /// the most damage to the squad). Ties go to the lowest object id so the choice is stable.
        /// </summary>
        public static uint PickTopDamageEnemy(IEnumerable<IReadOnlyDictionary<uint, int>> squadThreatTables, Func<uint, bool> isEnemy)
        {
            var totals = new Dictionary<uint, long>();
            foreach (var table in squadThreatTables)
            {
                foreach (var (enemy, threat) in table)
                {
                    if (threat <= 0 || !isEnemy(enemy))
                        continue;

                    totals[enemy] = totals.GetValueOrDefault(enemy) + threat;
                }
            }

            return totals.Count == 0
                ? OBJECT_INVALID
                : totals.OrderByDescending(x => x.Value).ThenBy(x => x.Key).First().Key;
        }

        /// <summary>
        /// Whether the tank's Shield Hardener should be on: on below half hull, and kept on until the hull is
        /// whole again (repaired or reset), so it does not flicker at the line.
        /// </summary>
        public static bool ShouldHarden(int hull, int maxHull, bool isOn)
        {
            if (maxHull <= 0)
                return false;

            return isOn
                ? hull < maxHull
                : hull < maxHull * ShipNpcRoleTuning.TankHardenBelowHullShare;
        }

        /// <summary>
        /// The ally a Logistics repairer locks onto: the one missing the largest share of the pool it repairs,
        /// once at least <see cref="ShipNpcRoleTuning.RepairMissingShare"/> is missing. Ties go to the larger
        /// missing amount. Invalid when no ally needs it.
        /// </summary>
        public static uint PickRepairTarget(IEnumerable<(uint Ship, int Missing, int Max)> allies)
        {
            var best = allies
                .Where(x => x.Max > 0 && x.Missing >= x.Max * ShipNpcRoleTuning.RepairMissingShare)
                .OrderByDescending(x => (double)x.Missing / x.Max)
                .ThenByDescending(x => x.Missing)
                .ThenBy(x => x.Ship)
                .ToList();

            return best.Count == 0 ? OBJECT_INVALID : best[0].Ship;
        }

        /// <summary>Whether a locked repairer has finished: its target is whole again.</summary>
        public static bool IsRepairDone(int missing)
        {
            return missing <= 0;
        }

        /// <summary>
        /// What an EW ship does to its target: jam it while it is not jammed, otherwise paint it while it is not
        /// painted, otherwise nothing.
        /// </summary>
        public static ShipNpcEwarAction ChooseEwarAction(bool jammed, bool painted)
        {
            if (!jammed)
                return ShipNpcEwarAction.Jam;

            return painted ? ShipNpcEwarAction.None : ShipNpcEwarAction.Paint;
        }

        /// <summary>
        /// Whether an enemy ship is fleeing an interceptor: it moved away since the last decision while below the
        /// flee hull share or beyond the interceptor's weapon range. A first sighting (no previous distance) is not.
        /// </summary>
        public static bool IsFleeing(float? previousDistance, float distance, int hull, int maxHull, float weaponRange)
        {
            if (previousDistance == null || distance < previousDistance.Value + ShipNpcRoleTuning.FleeMinimumStepMeters)
                return false;

            var lowHull = maxHull > 0 && hull < maxHull * ShipNpcRoleTuning.FleeHullShare;
            return lowHull || distance > weaponRange;
        }

        /// <summary>The interceptor's quarry: the nearest fleeing enemy, or invalid when none flees.</summary>
        public static uint PickQuarry(
            IEnumerable<(uint Ship, float? PreviousDistance, float Distance, int Hull, int MaxHull)> enemies,
            float weaponRange)
        {
            var fleeing = enemies
                .Where(x => IsFleeing(x.PreviousDistance, x.Distance, x.Hull, x.MaxHull, weaponRange))
                .OrderBy(x => x.Distance)
                .ThenBy(x => x.Ship)
                .ToList();

            return fleeing.Count == 0 ? OBJECT_INVALID : fleeing[0].Ship;
        }

        /// <summary>
        /// The bomber's target: the biggest hull within its weapon range, by size and then maximum hull. Invalid when
        /// no enemy is in range (the bomber then keeps its threat target).
        /// </summary>
        public static uint PickBiggestHull(IEnumerable<(uint Ship, ShipSize Size, int MaxHull, float Distance)> enemies, float weaponRange)
        {
            var inRange = enemies
                .Where(x => x.Distance <= weaponRange)
                .OrderByDescending(x => (int)x.Size)
                .ThenByDescending(x => x.MaxHull)
                .ThenBy(x => x.Distance)
                .ThenBy(x => x.Ship)
                .ToList();

            return inRange.Count == 0 ? OBJECT_INVALID : inRange[0].Ship;
        }

        /// <summary>The distance a sniper holds from its target: just inside its longest weapon's range.</summary>
        public static float KiteDistance(float longestWeaponRange)
        {
            return Math.Max(0f, longestWeaponRange * ShipNpcRoleTuning.KiteRangeShare);
        }

        /// <summary>
        /// How many fighters a carrier launches now: a wave once the wave cooldown is up, while it has waves left,
        /// trimmed so no more than <see cref="ShipNpcRoleTuning.MaxAliveFighters"/> fly at once. Zero otherwise.
        /// </summary>
        public static int FightersToLaunch(DateTime now, DateTime lastWave, int wavesLaunched, int aliveFighters)
        {
            if (wavesLaunched >= ShipNpcRoleTuning.MaxWaves ||
                !IsReady(now, lastWave, ShipNpcRoleTuning.WaveSeconds))
            {
                return 0;
            }

            return Math.Max(0, Math.Min(ShipNpcRoleTuning.WaveSize, ShipNpcRoleTuning.MaxAliveFighters - aliveFighters));
        }

        /// <summary>
        /// The fighter preset a carrier launches: the Fighter of its faction tier ("t3carrier" launches "t3fighter"),
        /// or a Rift Raider fighter from a Rift Raider carrier. Empty for a tag with no tier prefix.
        /// </summary>
        public static string FighterTag(string carrierTag)
        {
            if (WildSpaceRules.IsRiftRaider(carrierTag))
                return "rrfighter";

            if (string.IsNullOrWhiteSpace(carrierTag) || carrierTag.Length < 2 ||
                carrierTag[0] != 't' || !char.IsDigit(carrierTag[1]))
            {
                return string.Empty;
            }

            return "t" + carrierTag[1] + "fighter";
        }

        /// <summary>Whether a minelayer drops a mine now: its cooldown is up and fewer than the maximum are out.</summary>
        public static bool CanDropMine(DateTime now, DateTime lastMine, int activeMines)
        {
            return activeMines < ShipNpcRoleTuning.MaxMines &&
                   IsReady(now, lastMine, ShipNpcRoleTuning.MineSeconds);
        }

        /// <summary>Whether a mine has run out its life.</summary>
        public static bool IsMineExpired(DateTime now, DateTime laidAt)
        {
            return now >= laidAt.AddSeconds(ShipNpcRoleTuning.MineLifetimeSeconds);
        }
    }
}