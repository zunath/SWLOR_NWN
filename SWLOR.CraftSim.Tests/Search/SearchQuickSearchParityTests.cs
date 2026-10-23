using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Search;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.ScavengeService;

namespace SWLOR.CraftSim.Tests.Search
{
    /// <summary>
    /// Quick Search against today's scavenging: the same expected rolls, attempts and XP at the parity reference (a Perception modifier of
    /// 0), by the legacy model that ports ScavengePoint's rules. Quick Search is the server's own pure copy of those rules, so the two are
    /// independent code that must agree.
    /// </summary>
    [TestFixture]
    public class SearchQuickSearchParityTests
    {
        [Test]
        public void TheParityReferenceIsAPerceptionModifierOfZero()
        {
            ScavengeQuickSearch.ReferencePerceptionModifier.Should().Be(0);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void QuickSearchEqualsTodaysExpectedRollsAndAttempts_AtEveryHardLookLevel(int hardLook)
        {
            ScavengeQuickSearch.ExpectedRolls(hardLook).Should().BeApproximately(ScavengeModel.ExpectedSuccessesPerSite(hardLook, 0), 1e-12);
            ScavengeQuickSearch.ExpectedAttempts(hardLook).Should().BeApproximately(ScavengeModel.ExpectedAttemptsPerSite(hardLook, 0), 1e-12);
            ScavengeQuickSearch.SecondAttemptChance(hardLook).Should().Be((int)(ScavengeModel.SecondAttemptChance(hardLook, 0) * 100 + 0.5));
            ScavengeQuickSearch.SuccessChance(ScavengeQuickSearch.BaseDc).Should().BeApproximately(ScavengeModel.SuccessChance(ScavengeModel.BaseDc, 0), 1e-12);
        }

        [Test]
        public void TheExpectedRollsAtTheReferenceAreTheNumbersTheBaselineAndThePlanQuote()
        {
            // legacy-gathering-baseline.md, "Perception modifier 0": Hard Look 1 to 5 give 0.815, 0.88, 0.945, 1.01 and 1.075 successful
            // rolls per site (a 75% first attempt, plus a 10% to 50% second attempt that succeeds 65% of the time).
            var expected = new[] { 0.815, 0.88, 0.945, 1.01, 1.075 };

            Enumerable.Range(1, 5).Select(ScavengeQuickSearch.ExpectedRolls).Zip(expected, (actual, wanted) => Math.Abs(actual - wanted)).Should()
                .OnlyContain(difference => difference < 1e-9);
        }

        [Test]
        public void QuickSearchPaysTodaysExpectedXp_AtEverySiteLevelAndRank()
        {
            ServerXp.ForDelta(0);
            foreach (var level in Enumerable.Range(1, 5))
            {
                foreach (var rank in new[] { 0, 5, 10, 20, 30, 40, 45, 50 })
                {
                    var tier = CraftRankMilestones.HardLookLevelForRank(rank);
                    var delta = ServerXp.ForDelta(10 * level - rank);

                    ScavengeQuickSearch.ExpectedXp(tier, delta).Should().BeApproximately(
                        ScavengeModel.ExpectedXpPerSite(level, rank, tier, 0), 1e-9, $"site level {level}, rank {rank}");
                }
            }
        }

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        public void APlayedQuickSearchAgreesWithTheExpectedRollsAttemptsAndXp(int hardLook)
        {
            const int runs = 60_000;
            var random = SimMinigameRandom.ForRun(11, SimStreams.Legacy, hardLook);
            double rolls = 0, attempts = 0, xp = 0;
            for (var run = 0; run < runs; run++)
            {
                var result = ScavengeQuickSearch.Resolve(random, hardLook, 600);
                rolls += result.Successes;
                attempts += result.Attempts.Count;
                xp += result.TotalXp;
            }

            (rolls / runs).Should().BeApproximately(ScavengeQuickSearch.ExpectedRolls(hardLook), 0.01);
            (attempts / runs).Should().BeApproximately(ScavengeQuickSearch.ExpectedAttempts(hardLook), 0.01);
            (xp / runs).Should().BeApproximately(ScavengeQuickSearch.ExpectedXp(hardLook, 600), 3.0);
        }

        [Test]
        public void APlayedQuickSearchAgreesWithThePlayedLegacySearch()
        {
            const int runs = 60_000;
            double quick = 0, legacy = 0;
            var quickRandom = SimMinigameRandom.ForRun(21, SimStreams.Legacy, 1);
            var legacyRandom = new SimRandom(22);
            for (var run = 0; run < runs; run++)
            {
                quick += ScavengeQuickSearch.Resolve(quickRandom, 4, 600).Successes;
                legacy += ScavengeModel.SearchSite(legacyRandom, 3, 30, 4, 0).Successes;
            }

            (quick / runs).Should().BeApproximately(legacy / runs, 0.02);
        }

        [Test]
        public void QuickSearchTakesAsLongAsTodaysSearch_OneTwoSecondAnimationWhateverTheAttempts()
        {
            // The old code played one two-second animation before its whole one-or-two-attempt loop (ScavengePoint.OnOpened on master).
            // A second attempt added no time, so neither does it here; the legacy XP-rate loop is one two-second action per site.
            var random = SimMinigameRandom.ForRun(5, SimStreams.Legacy, 9);
            var sawSecondAttempt = false;
            for (var run = 0; run < 400; run++)
            {
                var result = ScavengeQuickSearch.Resolve(random, 5, 600);

                result.Seconds.Should().Be(2.0);
                sawSecondAttempt |= result.Attempts.Count == 2;
            }

            sawSecondAttempt.Should().BeTrue("at Hard Look level 5 half the sites make a second attempt");
            new SearchParityOptions().LegacySeconds.Should().Be(2.0);
        }

        [Test]
        public void ASuccessPaysTheFullXp_AFailureAQuarterRoundedDown_ExactlyAsScavengingAlwaysDid()
        {
            // d20 rolls of 5 (a failure against DC 6) and 20 (a success), no second attempt (a d100 of 100).
            var failure = ScavengeQuickSearch.Resolve(new ScriptedRolls(100, 5, 1), 1, 1050);
            var success = ScavengeQuickSearch.Resolve(new ScriptedRolls(100, 20, 1), 1, 1050);

            failure.Attempts.Single().Xp.Should().Be(1050 / 4);
            failure.Successes.Should().Be(0);
            success.Attempts.Single().Xp.Should().Be(1050);
            success.Successes.Should().Be(1);
        }

        [Test]
        public void TheSecondAttemptFacesAHigherDc()
        {
            // A d100 of 1 gives a second attempt; the first d20 is 6 (success against 6), the d3 is 3 (DC 9), the second d20 is 8 (fails).
            var result = ScavengeQuickSearch.Resolve(new ScriptedRolls(1, 6, 3, 8, 1), 5, 600);

            result.Attempts.Select(attempt => (attempt.Roll, attempt.Dc, attempt.Success)).Should().Equal((6, 6, true), (8, 9, false));
        }

        /// <summary>A source of chance that returns the rolls it was given in order, then fails.</summary>
        private sealed class ScriptedRolls : Game.Server.Service.CraftService.Minigame.IMinigameRandom
        {
            private readonly Queue<int> _rolls;

            public ScriptedRolls(params int[] rolls)
            {
                _rolls = new Queue<int>(rolls);
            }

            public int D100() => _rolls.Dequeue();

            public int Next(int minInclusive, int maxExclusive) => Math.Clamp(_rolls.Dequeue(), minInclusive, maxExclusive - 1);
        }
    }
}
