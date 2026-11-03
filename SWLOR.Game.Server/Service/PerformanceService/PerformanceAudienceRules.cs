using System.Collections.Generic;
using Vector3 = System.Numerics.Vector3;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// The audience's pure rules and constants: range, XP rank values, Flourish payouts, Standing Ovation
    /// and the frozen Inspired countdown. Nothing here calls the engine, so all of it is unit tested.
    /// </summary>
    public static class PerformanceAudienceRules
    {
        public const float BaseRangeMeters = 20f;
        public const double TickSeconds = 60.0;

        /// <summary>How early a tick may run and still count its full minute of watching.</summary>
        public const double TickToleranceSeconds = 5.0;
        public const int MaxRoutineRiders = 2;
        public const int MaxSetRiders = 2;

        public const int FlourishBonusMinutes = 2;
        public const int FlourishCapstoneBonusMinutes = 5;
        public const double FlourishWatcherCooldownSeconds = 120.0;
        public const double FlourishSyncWindowSeconds = 2.0;

        /// <summary>A performer in a live show holds Inspired for at least this long, so it outlasts a late tick and ends soon after the show.</summary>
        public const double PerformerInspiredSeconds = 120.0;

        public const int StandingOvationHealPercent = 10;
        public const int StandingOvationLowHealthPercent = 25;
        public const double StandingOvationCooldownSeconds = 600.0;

        /// <summary>
        /// Whole minutes watched since the first tick that saw the watcher, so a watcher who arrives just before a tick is
        /// credited nothing until a full minute has passed.
        /// </summary>
        public static int WatchedMinutes(DateTime sinceUtc, DateTime nowUtc)
        {
            var seconds = Math.Max(0.0, (nowUtc - sinceUtc).TotalSeconds);
            return (int)Math.Floor((seconds + TickToleranceSeconds) / TickSeconds);
        }

        public static float GetRange(int rangeBonusMeters)
        {
            return BaseRangeMeters + Math.Max(0, rangeBonusMeters);
        }

        public static bool IsWithinRange(Vector3 a, Vector3 b, float rangeMeters)
        {
            return Vector3.DistanceSquared(a, b) <= rangeMeters * rangeMeters;
        }

        /// <summary>Two performers are in each other's range when each reaches the other.</summary>
        public static bool AreInEachOthersRange(PerformerSnapshot a, PerformerSnapshot b)
        {
            return IsWithinRange(a.Position, b.Position, Math.Min(a.RangeMeters, b.RangeMeters));
        }

        /// <summary>
        /// The most a performer's XP bonus reaches at full SOC scaling: Dance and Play Instrument grant 6, 12, 18 or 24,
        /// which SOC raises toward 8, 15, 22 or 30. The rank is read from the stat the perk granted.
        /// </summary>
        public static int XpBonusMaximum(int baseBonus)
        {
            if (baseBonus >= 24) return 30;
            if (baseBonus >= 18) return 22;
            if (baseBonus >= 12) return 15;
            if (baseBonus >= 6) return 8;
            return Math.Max(0, baseBonus);
        }

        /// <summary>Every performer whose range covers a watcher counts toward the troupe bonus, followed or not.</summary>
        public static int TroupeCount(IReadOnlyList<PerformerSnapshot> candidates)
        {
            return candidates.Count;
        }

        /// <summary>A show is live while at least one non-performing watcher is in range; otherwise it is practice.</summary>
        public static bool IsLiveShow(int watcherCount)
        {
            return watcherCount > 0;
        }

        public static int FlourishBaseMinutes(bool hasCapstone)
        {
            return hasCapstone ? FlourishCapstoneBonusMinutes : FlourishBonusMinutes;
        }

        /// <summary>Two Flourishes within two seconds of each other double the minutes for both audiences.</summary>
        public static bool AreFlourishesSynchronized(DateTime flourishUtc, DateTime otherFlourishUtc)
        {
            var seconds = Math.Abs((flourishUtc - otherFlourishUtc).TotalSeconds);
            return seconds <= FlourishSyncWindowSeconds;
        }

        public static bool IsFreshFlourishBank(DateTime? lastBankUtc, DateTime nowUtc)
        {
            return !lastBankUtc.HasValue || (nowUtc - lastBankUtc.Value).TotalSeconds >= FlourishWatcherCooldownSeconds;
        }

        /// <summary>
        /// Minutes a Flourish adds for one watcher now. A watcher banks one Flourish per two minutes; when the second
        /// performer of a synchronized pair lands within two seconds of the first, the watcher is topped up to the doubled amount.
        /// </summary>
        public static double FlourishMinutesToBank(
            int baseMinutes,
            bool synchronized,
            DateTime? lastBankUtc,
            double lastBankedMinutes,
            DateTime nowUtc)
        {
            if (baseMinutes <= 0)
                return 0.0;

            var target = baseMinutes * (synchronized ? 2.0 : 1.0);
            if (IsFreshFlourishBank(lastBankUtc, nowUtc))
                return target;

            var seconds = (nowUtc - lastBankUtc.Value).TotalSeconds;
            if (synchronized && seconds <= FlourishSyncWindowSeconds)
                return Math.Max(0.0, target - lastBankedMinutes);

            return 0.0;
        }

        /// <summary>What the watcher has banked from the Flourish group after paying <paramref name="minutes"/> now.</summary>
        public static double FlourishBankedTotal(DateTime? lastBankUtc, double lastBankedMinutes, double minutes, DateTime nowUtc)
        {
            return IsFreshFlourishBank(lastBankUtc, nowUtc) ? minutes : lastBankedMinutes + minutes;
        }

        /// <summary>Standing Ovation fires when a hit that does not kill leaves the holder below 25% of their maximum HP.</summary>
        public static bool StandingOvationTriggers(int currentHP, int damage, int maxHP)
        {
            if (maxHP <= 0 || damage <= 0)
                return false;

            var remaining = currentHP - damage;
            return remaining > 0 && remaining * 100L < (long)StandingOvationLowHealthPercent * maxHP;
        }

        public static bool IsStandingOvationReady(DateTime? lastProcUtc, DateTime nowUtc)
        {
            return !lastProcUtc.HasValue ||
                   (nowUtc - lastProcUtc.Value).TotalSeconds >= StandingOvationCooldownSeconds;
        }

        public static int StandingOvationHeal(int maxHP)
        {
            return Math.Max(1, (int)((long)Math.Max(0, maxHP) * StandingOvationHealPercent / 100));
        }

        /// <summary>
        /// Inspired does not count down while its holder watches: each tick puts the remaining time back to what the
        /// previous tick set. Without a stored value (or without the effect) the actual remaining time stands.
        /// </summary>
        public static double FrozenRemainingSeconds(double actualSeconds, bool hasStored, double storedSeconds)
        {
            var actual = Math.Max(0.0, actualSeconds);
            return hasStored ? Math.Max(actual, storedSeconds) : actual;
        }

        /// <summary>
        /// The duration to put on a frozen Inspired effect. It must outlast the gap to the next tick, or a short remainder
        /// would expire while its holder is still watching; the stored frozen value stays what the next tick counts from.
        /// </summary>
        public static double HeldSeconds(double frozenSeconds)
        {
            return Math.Max(frozenSeconds, TickSeconds + TickToleranceSeconds);
        }

        /// <summary>
        /// The remaining seconds after banking minutes, held to the cap. A lower cap in a plainer venue never takes away
        /// time that was already banked.
        /// </summary>
        public static double BankedRemainingSeconds(double remainingSeconds, double bankedMinutes, int capMinutes)
        {
            var capped = PerformanceMath.AddInspiredMinutes(remainingSeconds / 60.0, bankedMinutes, capMinutes) * 60.0;
            return Math.Max(remainingSeconds, capped);
        }
    }
}
