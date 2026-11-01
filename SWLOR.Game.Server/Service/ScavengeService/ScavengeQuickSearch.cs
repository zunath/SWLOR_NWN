using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.Game.Server.Service.ScavengeService
{
    /// <summary>
    /// Quick Search: the choice the board offers besides playing it. It resolves the
    /// site by the rules scavenging had before the board, with no Dig Deeper, paying today's XP and taking as long as today's attempts.
    /// One attempt, plus a second at <c>Hard Look level x 10</c> percent (the level Gathering rank grants,
    /// <see cref="CraftRankMilestones.HardLookLevelForRank"/>); each attempt succeeds on a d20 against a DC of 6, rising by a d3 after each
    /// attempt. It pays the XP for the site against the Gathering rank for a success and a quarter of it for a failure.
    /// <para>
    /// The old search added the character's Perception modifier to the d20 and to the second attempt. Scavenging no longer reads
    /// attributes, so Quick Search rolls at the parity reference of a character with no investment: a modifier of 0
    /// (<see cref="ReferencePerceptionModifier"/>).
    /// </para>
    /// </summary>
    public static class ScavengeQuickSearch
    {
        /// <summary>The Perception modifier Quick Search rolls with, the same for every scavenger. 0 is a character with no investment.</summary>
        public const int ReferencePerceptionModifier = 0;

        /// <summary>The DC of the first attempt.</summary>
        public const int BaseDc = 6;

        /// <summary>The chance of a second attempt, in percent, for a Hard Look level: 10 per level.</summary>
        public static int SecondAttemptChance(int hardLookLevel) =>
            Math.Clamp(hardLookLevel * 10 + ReferencePerceptionModifier * 5, 0, 100);

        /// <summary>The chance that a d20 reaches the DC, as a fraction.</summary>
        public static double SuccessChance(int dc) =>
            Math.Clamp(21 - (dc - ReferencePerceptionModifier), 0, 20) / 20.0;

        /// <summary>The attempts a Quick Search makes on average: one, plus the chance of a second.</summary>
        public static double ExpectedAttempts(int hardLookLevel) => 1 + SecondAttemptChance(hardLookLevel) / 100.0;

        /// <summary>
        /// The successful attempts a Quick Search makes on average, which is today's expected rolls of the site's loot table at the
        /// reference: the first attempt's chance, plus the second attempt's chance of happening and succeeding against the DC raised
        /// by a d3.
        /// </summary>
        public static double ExpectedRolls(int hardLookLevel)
        {
            var second = 0.0;
            for (var rise = 1; rise <= 3; rise++)
                second += SuccessChance(BaseDc + rise);

            return SuccessChance(BaseDc) + SecondAttemptChance(hardLookLevel) / 100.0 * (second / 3);
        }

        /// <summary>
        /// The Gathering XP a Quick Search pays on average when a success pays <paramref name="deltaXp"/> and a failure a quarter of it
        /// (rounded down), which is today's XP for the site.
        /// </summary>
        public static double ExpectedXp(int hardLookLevel, int deltaXp)
        {
            var failureXp = deltaXp / 4;
            var first = SuccessChance(BaseDc);
            var second = 0.0;
            for (var rise = 1; rise <= 3; rise++)
                second += SuccessChance(BaseDc + rise);

            second /= 3;
            var secondChance = SecondAttemptChance(hardLookLevel) / 100.0;
            return first * deltaXp + (1 - first) * failureXp +
                   secondChance * (second * deltaXp + (1 - second) * failureXp);
        }

        /// <summary>
        /// Resolves a site: whether a second attempt is made, each roll against its DC, and the XP of each attempt.
        /// </summary>
        /// <param name="random">The source of chance.</param>
        /// <param name="hardLookLevel">The Hard Look level the Gathering rank grants.</param>
        /// <param name="deltaXp">The XP a success pays: <c>Skill.GetDeltaXP(10 x site level - Gathering rank)</c>. A failure pays a quarter.</param>
        public static QuickSearchResult Resolve(IMinigameRandom random, int hardLookLevel, int deltaXp)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var attempts = 1;
            if (random.D100() <= SecondAttemptChance(hardLookLevel))
                attempts++;

            var dc = BaseDc;
            var results = new List<QuickSearchAttempt>(attempts);
            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                var roll = random.Next(1, 21);
                var success = roll + ReferencePerceptionModifier >= dc;
                results.Add(new QuickSearchAttempt(roll, dc, success, success ? deltaXp : deltaXp / 4));
                dc += random.Next(1, 4);
            }

            return new QuickSearchResult(results);
        }
    }
}
