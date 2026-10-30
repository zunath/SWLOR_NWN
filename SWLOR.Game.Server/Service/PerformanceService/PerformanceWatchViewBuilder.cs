using System.Collections.Generic;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>Builds what the Watching window shows from one watcher's tick. Pure; the caller supplies the names.</summary>
    public static class PerformanceWatchViewBuilder
    {
        /// <param name="candidates">Every performer whose range covers the watcher.</param>
        /// <param name="dancer">The followed dancer, or null.</param>
        /// <param name="musician">The followed musician, or null.</param>
        /// <param name="plan">What the watcher earns this tick.</param>
        /// <param name="continuousMinutes">Minutes watched without a break, including this tick.</param>
        /// <param name="remainingSeconds">The Inspired the watcher holds after this tick.</param>
        /// <param name="xpDebt">The watcher's XP debt after this tick, or -1 when unknown.</param>
        /// <param name="debtCleared">XP debt this tick cleared.</param>
        /// <param name="composition">The Inspired the followed performers give.</param>
        /// <param name="nameOf">The name the watcher knows a performer by.</param>
        public static PerformanceWatchView Build(
            IReadOnlyList<PerformerSnapshot> candidates,
            PerformerSnapshot dancer,
            PerformerSnapshot musician,
            PerformanceWatchPlan plan,
            int continuousMinutes,
            double remainingSeconds,
            int xpDebt,
            int debtCleared,
            InspiredEffectData composition,
            Func<uint, string> nameOf)
        {
            var remainingMinutes = remainingSeconds / 60.0;
            var rate = GetRatePerMinute(plan);
            var debtPercent = Math.Max(0, dancer?.DebtClearPercent ?? 0) + Math.Max(0, musician?.DebtClearPercent ?? 0);
            var warmupLeft = Math.Max(0, PerformanceMath.WarmupMinutes - continuousMinutes);

            return new PerformanceWatchView
            {
                HeaderText = PerformanceWatchText.Header(
                    candidates.Count, PerformanceMath.TroupeBonus(PerformanceAudienceRules.TroupeCount(candidates))),
                Rows = BuildRows(candidates, dancer, musician, nameOf),
                BankedFraction = PerformanceWatchText.BankedFraction(remainingMinutes, plan.CapMinutes),
                BankedText = PerformanceWatchText.Banked(remainingMinutes, plan.CapMinutes, rate, plan.IsPaying, warmupLeft),
                DebtText = PerformanceWatchText.Debt(xpDebt, debtPercent, debtCleared),
                LeaveWithText = PerformanceWatchText.LeaveWith(composition)
            };
        }

        /// <summary>Minutes of Inspired the watcher banks each minute; the warm-up payout is three minutes' worth at once.</summary>
        public static double GetRatePerMinute(PerformanceWatchPlan plan)
        {
            if (!plan.IsPaying)
                return 0.0;

            return plan.WarmupCompletesNow ? plan.BankedMinutes / PerformanceMath.WarmupMinutes : plan.BankedMinutes;
        }

        private static List<PerformanceWatchRow> BuildRows(
            IReadOnlyList<PerformerSnapshot> candidates,
            PerformerSnapshot dancer,
            PerformerSnapshot musician,
            Func<uint, string> nameOf)
        {
            var rows = new List<PerformanceWatchRow>(candidates.Count);
            AddRows(rows, candidates, PerformanceKind.Dance, dancer, musician, nameOf);
            AddRows(rows, candidates, PerformanceKind.Music, dancer, musician, nameOf);
            return rows;
        }

        private static void AddRows(
            List<PerformanceWatchRow> rows,
            IReadOnlyList<PerformerSnapshot> candidates,
            PerformanceKind kind,
            PerformerSnapshot dancer,
            PerformerSnapshot musician,
            Func<uint, string> nameOf)
        {
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (candidate.Kind != kind)
                    continue;

                var followed = candidate.Performer == (dancer?.Performer ?? 0u) ||
                               candidate.Performer == (musician?.Performer ?? 0u);
                rows.Add(new PerformanceWatchRow(
                    candidate.Performer,
                    nameOf(candidate.Performer),
                    PerformanceWatchText.RowDetail(DescribeProgram(candidate), candidate.Quality),
                    followed));
            }
        }

        // The routines a dancer performs, or the sets a musician plays, as the audience will receive them.
        private static string DescribeProgram(PerformerSnapshot performer)
        {
            if (performer.Kind == PerformanceKind.Dance)
            {
                var first = PerformanceRiderText.GetName(performer.Routine);
                return performer.HasCapstone && performer.SecondRoutine != PerformanceRoutineType.CantinaShuffle
                    ? $"{first} + {PerformanceRiderText.GetName(performer.SecondRoutine)}"
                    : first;
            }

            var set = PerformanceRiderText.GetName(performer.Set);
            return performer.HasCapstone && performer.SecondSet != PerformanceSetType.CantinaStandards
                ? $"{set} + {PerformanceRiderText.GetName(performer.SecondSet)}"
                : set;
        }
    }
}
