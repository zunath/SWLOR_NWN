namespace SWLOR.Game.Server.Service.PerformanceService
{
    /// <summary>
    /// What one watcher earns this tick from the two performers they follow. Pure: it takes the minutes they have
    /// watched without a break (counting this tick) and the followed performers, and returns the numbers to apply.
    /// </summary>
    public readonly struct PerformanceWatchPlan
    {
        private PerformanceWatchPlan(
            bool isPaying,
            bool warmupCompletesNow,
            double bankedMinutes,
            int debtClearPercent,
            int capMinutes,
            int venue,
            PerformanceQuality quality)
        {
            IsPaying = isPaying;
            WarmupCompletesNow = warmupCompletesNow;
            BankedMinutes = bankedMinutes;
            DebtClearPercent = debtClearPercent;
            CapMinutes = capMinutes;
            Venue = venue;
            Quality = quality;
        }

        /// <summary>False during the 3-minute warm-up, when nothing is banked and no debt is cleared.</summary>
        public bool IsPaying { get; }

        /// <summary>True on the tick that ends the warm-up, which banks three minutes' worth at once.</summary>
        public bool WarmupCompletesNow { get; }

        public double BankedMinutes { get; }
        public int DebtClearPercent { get; }
        public int CapMinutes { get; }

        /// <summary>The better venue of the two followed performers.</summary>
        public int Venue { get; }

        /// <summary>The better quality of the two followed performers.</summary>
        public PerformanceQuality Quality { get; }

        /// <param name="continuousMinutes">Minutes watched without a break, including this tick.</param>
        /// <param name="dancer">The followed dancer, or null.</param>
        /// <param name="musician">The followed musician, or null.</param>
        /// <param name="troupeCount">Every performer whose range covers the watcher, followed or not.</param>
        public static PerformanceWatchPlan Create(
            int continuousMinutes,
            PerformerSnapshot dancer,
            PerformerSnapshot musician,
            int troupeCount)
        {
            var venue = PerformanceMath.ClampVenue(Math.Max(dancer?.Venue ?? 0, musician?.Venue ?? 0));
            var quality = BestQuality(dancer, musician);
            var capMinutes = PerformanceMath.InspiredCapMinutes(venue, dancer?.CapBonusMinutes ?? 0);

            if (continuousMinutes < PerformanceMath.WarmupMinutes || dancer == null && musician == null)
                return new PerformanceWatchPlan(false, false, 0.0, 0, capMinutes, venue, quality);

            var harmony = musician != null && musician.HarmonyActive ? musician.BankingBonusPercent : 0;
            var rate = PerformanceMath.BankingMinutes(
                quality.GetBankingMultiplier(),
                venue,
                troupeCount,
                dancer?.BankingBonusPercent ?? 0,
                harmony);

            var warmupCompletesNow = continuousMinutes == PerformanceMath.WarmupMinutes;
            var banked = warmupCompletesNow ? PerformanceMath.WarmupPayout(rate) : rate;
            var debtPercent = Math.Max(0, dancer?.DebtClearPercent ?? 0) + Math.Max(0, musician?.DebtClearPercent ?? 0);
            return new PerformanceWatchPlan(true, warmupCompletesNow, banked, debtPercent, capMinutes, venue, quality);
        }

        private static PerformanceQuality BestQuality(PerformerSnapshot dancer, PerformerSnapshot musician)
        {
            if (dancer == null)
                return musician?.Quality ?? PerformanceQuality.Steady;
            if (musician == null)
                return dancer.Quality;

            return dancer.Quality >= musician.Quality ? dancer.Quality : musician.Quality;
        }
    }
}
