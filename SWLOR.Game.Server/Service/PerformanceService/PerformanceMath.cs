namespace SWLOR.Game.Server.Service.PerformanceService
{
    public static class PerformanceMath
    {
        public const int WarmupMinutes = 3;
        public const double BaseBankingMinutesPerMinute = 4.0;
        public const double VenueBankingBonusPerPoint = 0.10;
        public const double TroupeBankingBonusPerPerformer = 0.10;
        public const double TroupeBankingBonusCap = 0.30;
        public const int BaseInspiredCapMinutes = 60;
        public const int InspiredCapMinutesPerVenuePoint = 12;
        public const int MaxVenueRating = 5;
        public const double CrowdFactorPerExtraWatcher = 0.25;
        public const double MaxCrowdFactor = 2.0;
        public const double PracticeXPMultiplier = 0.25;
        public const int PerformanceXPPerMinute = 120;
        public const int QualityWindowSize = 20;
        public const int QualityMinimumRecorded = 5;
        public const int FlourishMeterMax = 16;
        public const double IdleTimeoutSeconds = 60.0;
        public const float MaxMoveDistanceMeters = 1.0f;

        private const double SteadyAccuracy = 0.50;
        private const double GreatAccuracy = 0.80;
        private const double DazzlingAccuracy = 0.95;

        public static PerformanceQuality QualityFromAccuracy(double accuracy)
        {
            if (accuracy >= DazzlingAccuracy) return PerformanceQuality.Dazzling;
            if (accuracy >= GreatAccuracy) return PerformanceQuality.Great;
            if (accuracy >= SteadyAccuracy) return PerformanceQuality.Steady;
            return PerformanceQuality.Rough;
        }

        /// <summary>Quality of the last scored cues; stays Steady until enough cues have been recorded.</summary>
        public static PerformanceQuality QualityFromRecord(int hits, int recorded)
        {
            if (recorded < QualityMinimumRecorded)
                return PerformanceQuality.Steady;

            // Integer percent comparison keeps the 50/80/95 boundaries exact.
            var scaled = (long)hits * 100;
            if (scaled >= 95L * recorded) return PerformanceQuality.Dazzling;
            if (scaled >= 80L * recorded) return PerformanceQuality.Great;
            if (scaled >= 50L * recorded) return PerformanceQuality.Steady;
            return PerformanceQuality.Rough;
        }

        /// <summary>Extra banking from other performers in range; the count includes every performer covering the watcher, followed ones too.</summary>
        public static double TroupeBonus(int troupeCount)
        {
            return Math.Clamp(TroupeBankingBonusPerPerformer * (troupeCount - 1), 0.0, TroupeBankingBonusCap);
        }

        /// <summary>Minutes of Inspired a watcher banks for each minute watched.</summary>
        public static double BankingMinutes(
            double qualityMultiplier,
            int venue,
            int troupeCount,
            int crowdPleaserPercent,
            int harmonyPercent)
        {
            var bonus = VenueBankingBonusPerPoint * ClampVenue(venue) +
                        TroupeBonus(troupeCount) +
                        Math.Max(0, crowdPleaserPercent) / 100.0 +
                        Math.Max(0, harmonyPercent) / 100.0;

            return BaseBankingMinutesPerMinute * Math.Max(0.0, qualityMultiplier) * (1.0 + bonus);
        }

        public static int InspiredCapMinutes(int venue, int encoreMinutes)
        {
            return BaseInspiredCapMinutes + InspiredCapMinutesPerVenuePoint * ClampVenue(venue) + Math.Max(0, encoreMinutes);
        }

        public static double AddInspiredMinutes(double remainingMinutes, double bankedMinutes, int capMinutes)
        {
            return Math.Min(capMinutes, Math.Max(0.0, remainingMinutes) + Math.Max(0.0, bankedMinutes));
        }

        /// <summary>Banked all at once when a watcher's warm-up ends.</summary>
        public static double WarmupPayout(double bankingMinutesPerMinute)
        {
            return WarmupMinutes * Math.Max(0.0, bankingMinutesPerMinute);
        }

        /// <summary>Whole XP points of debt cleared in one minute; rounds down.</summary>
        public static int DebtCleared(int debt, int percent)
        {
            if (debt <= 0 || percent <= 0)
                return 0;

            // Round down, but always clear at least 1 so a small debt does not get stuck.
            var cleared = (long)debt * Math.Min(percent, 100) / 100;
            return (int)Math.Min(debt, Math.Max(1, cleared));
        }

        public static double CrowdFactor(int crowd)
        {
            return Math.Min(MaxCrowdFactor, 1.0 + CrowdFactorPerExtraWatcher * Math.Max(0, crowd - 1));
        }

        /// <summary>Skill XP a performer earns per minute; with no watchers it is practice at a quarter rate.</summary>
        public static int PerformerXP(int baseXP, PerformanceQuality quality, int crowd, PerformanceTempo tempo)
        {
            var audience = crowd > 0 ? CrowdFactor(crowd) : PracticeXPMultiplier;
            var xp = Math.Max(0, baseXP) * quality.GetBankingMultiplier() * audience * tempo.GetXPMultiplier();
            return (int)Math.Round(xp, MidpointRounding.AwayFromZero);
        }

        public static int ClampVenue(int venue)
        {
            return Math.Clamp(venue, 0, MaxVenueRating);
        }
    }
}
