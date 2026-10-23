using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Rng;

namespace SWLOR.CraftSim.Legacy.Gathering
{
    /// <summary>Searching a scavenge site as it always played, and as Quick Search plays it (ScavengeQuickSearch.cs, at Perception modifier 0): one or two attempts at d20 + Perception against a rising DC. Loot is not modelled.</summary>
    public static class ScavengeModel
    {
        public const int BaseDc = 6;
        public const int FirstSiteLevel = 1;
        public const int LastSiteLevel = 5;
        public const int MaximumHardLook = 5;

        /// <summary>ScavengeQuickSearch.cs:44-45: d100 &lt;= Hard Look x 10 + Perception modifier x 5.</summary>
        public static double SecondAttemptChance(int hardLook, int perceptionModifier) =>
            Math.Clamp(hardLook * 10 + perceptionModifier * 5, 0, 100) / 100.0;

        /// <summary>ScavengeQuickSearch.cs:49: the chance that d20 + Perception modifier reaches the DC.</summary>
        public static double SuccessChance(int dc, int perceptionModifier) =>
            Math.Clamp(21 - (dc - perceptionModifier), 0, 20) / 20.0;

        /// <summary>The second attempt's success chance, averaged over the d3 the DC rose by (ScavengeQuickSearch.cs:108).</summary>
        public static double SecondAttemptSuccessChance(int perceptionModifier)
        {
            var total = 0.0;
            for (var rise = 1; rise <= 3; rise++)
                total += SuccessChance(BaseDc + rise, perceptionModifier);

            return total / 3;
        }

        public static double ExpectedSuccessesPerSite(int hardLook, int perceptionModifier) =>
            SuccessChance(BaseDc, perceptionModifier) +
            SecondAttemptChance(hardLook, perceptionModifier) * SecondAttemptSuccessChance(perceptionModifier);

        public static double ExpectedAttemptsPerSite(int hardLook, int perceptionModifier) =>
            1 + SecondAttemptChance(hardLook, perceptionModifier);

        /// <summary>The level XP is measured at: 10 x the site's level (ScavengePoint.cs:93).</summary>
        public static int SiteXpLevel(int siteLevel) => 10 * siteLevel;

        /// <summary>XP for a successful attempt, from the server's own chart; a failed attempt pays a quarter (ScavengeQuickSearch.cs:107).</summary>
        public static int SuccessXp(int siteLevel, int gatheringRank) => ServerXp.ForDelta(SiteXpLevel(siteLevel) - gatheringRank);

        public static double ExpectedXpPerSite(int siteLevel, int gatheringRank, int hardLook, int perceptionModifier)
        {
            var successXp = SuccessXp(siteLevel, gatheringRank);
            var failureXp = successXp / 4;
            var first = SuccessChance(BaseDc, perceptionModifier);
            var second = SecondAttemptChance(hardLook, perceptionModifier);
            var secondSuccess = SecondAttemptSuccessChance(perceptionModifier);
            return first * successXp + (1 - first) * failureXp +
                   second * (secondSuccess * successXp + (1 - secondSuccess) * failureXp);
        }

        public static ScavengeSiteResult SearchSite(SimRandom rng, int siteLevel, int gatheringRank, int hardLook, int perceptionModifier)
        {
            var attempts = 1;
            if (rng.D100() <= hardLook * 10 + perceptionModifier * 5)
                attempts++;

            var successXp = SuccessXp(siteLevel, gatheringRank);
            var dc = BaseDc;
            var successes = 0;
            var xp = 0;
            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                if (rng.D20() + perceptionModifier >= dc)
                {
                    successes++;
                    xp += successXp;
                }
                else
                {
                    xp += successXp / 4;
                }

                dc += rng.D3();
            }

            return new ScavengeSiteResult(attempts, successes, xp);
        }
    }
}
