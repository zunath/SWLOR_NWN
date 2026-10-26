using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.CraftSim.Tests.Gates
{
    [TestFixture]
    public class QualityDistributionTests
    {
        /// <summary>A distribution of <paramref name="attempts"/> attempts of which the given counts made the item at the given percentages.</summary>
        internal static QualityDistribution Dist(int attempts, params (int Percent, int Count)[] made)
        {
            var counts = new int[QualityDistribution.Levels];
            foreach (var (percent, count) in made)
                counts[percent] += count;

            return new QualityDistribution(counts, attempts);
        }

        [Test]
        public void EightyPercentQualityAveragesTwoPointFourOfThreeToday_AndExactlyOneUnderFiftyEightyFiveEightyFive()
        {
            // The plan's example: Today each enhancement transfers at the quality percentage; the thresholds transfer slot 1 only at 80%.
            var always80 = Dist(100, (80, 100));

            LegacyEnhancementRules.ExpectedTransfers(3, 80).Should().BeApproximately(2.4, 1e-12);
            always80.ExpectedTransfers(3).Should().Be(1.0);
            always80.ExpectedTransfers(1).Should().Be(1.0);
            always80.ExpectedTransfers(2).Should().Be(1.0);
        }

        [Test]
        public void TheThresholdsInForceAreTheOnesCraftOutcomeRulesHolds()
        {
            var atEighty = Dist(10, (80, 10));
            var atNinety = Dist(10, (90, 10));
            var atHundred = Dist(10, (100, 10));

            atEighty.ExpectedTransfers(3, new[] { CraftOutcomeRules.GetEnhancementThreshold(1), CraftOutcomeRules.GetEnhancementThreshold(2), CraftOutcomeRules.GetEnhancementThreshold(3) })
                .Should().Be(atEighty.ExpectedTransfers(3));
            var thresholds = new[] { CraftOutcomeRules.GetEnhancementThreshold(1), CraftOutcomeRules.GetEnhancementThreshold(2), CraftOutcomeRules.GetEnhancementThreshold(3) };

            atHundred.ExpectedTransfers(3).Should().Be(3.0, "100% meets every threshold in force");
            atNinety.ExpectedTransfers(3).Should().Be(thresholds.Count(t => t <= 90), "90% meets only the thresholds at or below 90");
            atNinety.ExpectedTransfers(3).Should().Be(2.0, "the third slot needs a full-quality craft");
        }

        [Test]
        public void EveryQualityMeetsTheSameThresholdsTheOutcomeRulesApply()
        {
            // The distribution counts transfers the way CraftOutcomeRules.CalculateSuccess decides them, for every quality and slot count.
            for (var percent = 0; percent <= 100; percent++)
            {
                for (var slots = 1; slots <= 4; slots++)
                {
                    var outcome = CraftOutcomeRules.CalculateSuccess(
                        new MinigameMeter(percent, 100), 40, 100, false, 0, false, Enumerable.Range(1, slots).ToList());

                    Dist(1, (percent, 1)).ExpectedTransfers(slots).Should().Be(
                        outcome.Enhancements.Count(e => e.Transfers), $"quality {percent}% with {slots} slot(s)");
                }
            }
        }

        [Test]
        public void ShareAtLeastCountsFailuresInTheDenominator()
        {
            var distribution = Dist(10, (40, 2), (60, 3), (100, 1));

            distribution.Attempts.Should().Be(10);
            distribution.Successes.Should().Be(6);
            distribution.SuccessShare.Should().Be(0.6);
            distribution.ShareAtLeast(0).Should().Be(0.6);
            distribution.ShareAtLeast(41).Should().Be(0.4);
            distribution.ShareAtLeast(60).Should().Be(0.4);
            distribution.ShareAtLeast(61).Should().Be(0.1);
            distribution.ShareAtLeast(100).Should().Be(0.1);
            distribution.ShareAtLeast(-5).Should().Be(0.6);
        }

        [Test]
        public void PercentilesAndTheMeanAreOverTheItemsMade()
        {
            var distribution = Dist(20, (40, 2), (60, 3), (100, 5));

            distribution.Percentile(0.10).Should().Be(40);
            distribution.Percentile(0.20).Should().Be(40);
            distribution.Percentile(0.50).Should().Be(60);
            distribution.Percentile(0.90).Should().Be(100);
            distribution.Percentile(1.0).Should().Be(100);
            distribution.MeanPercent.Should().BeApproximately((2 * 40 + 3 * 60 + 5 * 100) / 10.0, 1e-12);
            Dist(5).Percentile(0.5).Should().Be(0);
            Dist(5).MeanPercent.Should().Be(0);
            Dist(5).ExpectedTransfers(3).Should().Be(0);
        }

        [Test]
        public void TheStandardErrorIsZeroWhenEveryAttemptTransfersTheSameAndGrowsWithTheSpread()
        {
            Dist(100, (100, 100)).TransfersStandardError(3).Should().Be(0);
            Dist(100).TransfersStandardError(3).Should().Be(0);
            Dist(1, (100, 1)).TransfersStandardError(3).Should().Be(0, "one attempt has no spread");

            // Half of 100 attempts transfer 3 and half none: the standard deviation is 1.5, so the error is 0.15.
            Dist(100, (100, 50)).TransfersStandardError(3).Should().BeApproximately(0.15, 1e-12);
        }

        [Test]
        public void ADistributionMustHaveAHundredAndOneCountsAndNoMoreItemsThanAttempts()
        {
            ((Action)(() => new QualityDistribution(new[] { 1, 2 }, 3))).Should().Throw<ArgumentException>().WithMessage("*101 counts*");
            ((Action)(() => Dist(2, (50, 3)))).Should().Throw<ArgumentException>().WithMessage("*exceed the attempts*");
            ((Action)(() => new QualityDistribution(null, 1))).Should().Throw<ArgumentNullException>();
        }
    }

    [TestFixture]
    public class EnhancementThresholdSolverTests
    {
        private static QualityDistribution Dist(int attempts, params (int Percent, int Count)[] made) => QualityDistributionTests.Dist(attempts, made);

        private static ParityCase Case(int slots, double required, QualityDistribution distribution) =>
            new($"{slots} slot(s)", slots, distribution, required);

        [Test]
        public void ConstantEightyPercentQualityNeedsThresholdsAtEightyToMatchTodaysTwoPointFour()
        {
            var solution = EnhancementThresholdSolver.Solve(new[] { Case(3, 2.4, Dist(100, (80, 100))) });

            solution.Reachable.Should().BeTrue();
            solution.Thresholds.Should().Equal(80, 80, 80);
            solution.Describe().Should().Be("80% / 80% / 80%");
        }

        [Test]
        public void TheStrictestThresholdsThatStillMeetParityAreChosen()
        {
            // 20 of 100 attempts at 50, 80 at 80. Three slots need 2.4: (80, 80, 80) gives 0.8 x 3; lower thresholds would also pass but are less strict.
            var solution = EnhancementThresholdSolver.Solve(new[] { Case(3, 2.4, Dist(100, (50, 20), (80, 80))) });

            solution.Thresholds.Should().Equal(80, 80, 80);
        }

        [Test]
        public void SeveralCasesShareTheThresholds()
        {
            // One slot needs 0.9: 90 of 100 at 70 and 10 at 30. Three slots need 2.4 from attempts all at 80.
            var oneSlot = Case(1, 0.9, Dist(100, (70, 90), (30, 10)));
            var threeSlots = Case(3, 2.4, Dist(100, (80, 100)));

            var solution = EnhancementThresholdSolver.Solve(new[] { oneSlot, threeSlots });

            solution.Thresholds.Should().Equal(70, 80, 80);
        }

        [Test]
        public void NoThresholdsMeetParityWhenTheMinigameMakesTooFewItems()
        {
            // Only 40% of attempts make the item, and today's craft transfers 0.8 of 1: even free thresholds give 0.4.
            var solution = EnhancementThresholdSolver.Solve(new[] { Case(1, 0.8, Dist(100, (100, 40))) });

            solution.Reachable.Should().BeFalse();
            solution.Thresholds.Should().BeEmpty();
            solution.Describe().Should().Be("none");
        }

        [Test]
        public void ACaseThatNeedsNothingIsMetByTheStrictestPossibleThresholds()
        {
            var solution = EnhancementThresholdSolver.Solve(new[] { Case(2, 0, Dist(100, (80, 100))) });

            solution.Thresholds.Should().Equal(100, 100);
        }

        [Test]
        public void StepsOfFiveRoundTheThresholdsDownToRoundFigures()
        {
            var cases = new[] { Case(1, 1.0, Dist(100, (83, 100))) };

            EnhancementThresholdSolver.Solve(cases).Thresholds.Should().Equal(83);
            EnhancementThresholdSolver.Solve(cases, 5).Thresholds.Should().Equal(80);
            EnhancementThresholdSolver.Solve(cases, 100).Thresholds.Should().Equal(new[] { 0 }); // only 0 and 100 are allowed and 100 is out of reach
        }

        [Test]
        public void TheNumberOfThresholdsIsTheLargestSlotCount()
        {
            EnhancementThresholdSolver.Solve(new[] { Case(1, 1, Dist(10, (90, 10))) }).Thresholds.Should().Equal(90);
            EnhancementThresholdSolver.Solve(new[] { Case(2, 1, Dist(10, (90, 10))), Case(1, 1, Dist(10, (90, 10))) }).Thresholds.Should().HaveCount(2);
        }

        [Test]
        public void ThresholdsNeverFallFromOneSlotToTheNext()
        {
            // Half the attempts reach 95 and half 40; two slots need 1.0 in total. (95, 95) and (40, 95) both pass but (95, 40) would be a fall.
            var solution = EnhancementThresholdSolver.Solve(new[] { Case(2, 1.0, Dist(100, (95, 50), (40, 50))) });

            solution.Thresholds.Should().Equal(95, 95);
            solution.Thresholds[0].Should().BeLessThanOrEqualTo(solution.Thresholds[1]);
        }

        [Test]
        public void ItAgreesWithABruteForceOverEveryTripleOnRandomDistributions()
        {
            var random = new Random(11);
            for (var round = 0; round < 25; round++)
            {
                var cases = Enumerable.Range(1, 3).Select(slots =>
                {
                    var counts = new List<(int, int)>();
                    var remaining = 60;
                    for (var group = 0; group < 4 && remaining > 0; group++)
                    {
                        var count = random.Next(1, Math.Min(remaining, 40) + 1);
                        counts.Add((random.Next(20, 101), count));
                        remaining -= count;
                    }

                    var merged = counts.GroupBy(c => c.Item1).Select(g => (g.Key, g.Sum(c => c.Item2))).ToArray();
                    var distribution = Dist(100, merged);
                    // Ask for somewhere between 40% and 100% of what the best thresholds could give.
                    var required = distribution.ExpectedTransfers(slots, Enumerable.Repeat(0, slots).ToArray()) * (0.4 + 0.6 * random.NextDouble());
                    return Case(slots, required, distribution);
                }).ToList();

                foreach (var step in new[] { 1, 5 })
                {
                    var solved = EnhancementThresholdSolver.Solve(cases, step);
                    var best = BruteForce(cases, step);

                    solved.Reachable.Should().Be(best != null, $"round {round}, step {step}");
                    if (best == null)
                        continue;

                    solved.Thresholds.Should().Equal(best, $"round {round}, step {step}");
                    solved.Thresholds.Zip(solved.Thresholds.Skip(1), (a, b) => a <= b).Should().OnlyContain(ok => ok);
                    foreach (var parity in cases)
                        parity.Distribution.ExpectedTransfers(parity.Slots, solved.Thresholds).Should().BeGreaterThanOrEqualTo(parity.RequiredTransfers - 1e-9);
                }
            }
        }

        [Test]
        public void BadArgumentsAreRejected()
        {
            ((Action)(() => EnhancementThresholdSolver.Solve(null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => EnhancementThresholdSolver.Solve(new[] { Case(1, 1, Dist(1, (50, 1))) }, 0))).Should().Throw<ArgumentOutOfRangeException>();
            ((Action)(() => EnhancementThresholdSolver.Solve(new[] { Case(0, 1, Dist(1, (50, 1))) }))).Should().Throw<ArgumentException>();
            EnhancementThresholdSolver.Solve(Array.Empty<ParityCase>()).Reachable.Should().BeFalse();
        }

        /// <summary>The best triple by the solver's own rule, found by trying every non-decreasing triple of whole percentages in steps.</summary>
        private static int[] BruteForce(IReadOnlyList<ParityCase> cases, int step)
        {
            int[] best = null;
            var levels = Enumerable.Range(0, 100 / step + 1).Select(i => i * step).ToList();
            foreach (var a in levels)
            {
                foreach (var b in levels.Where(l => l >= a))
                {
                    foreach (var c in levels.Where(l => l >= b))
                    {
                        var thresholds = new[] { a, b, c };
                        if (!cases.All(parity => parity.Distribution.ExpectedTransfers(parity.Slots, thresholds) + 1e-9 >= parity.RequiredTransfers))
                            continue;

                        if (best == null || thresholds.Sum() > best.Sum() ||
                            (thresholds.Sum() == best.Sum() && string.CompareOrdinal(Key(thresholds), Key(best)) > 0))
                        {
                            best = thresholds;
                        }
                    }
                }
            }

            return best;
        }

        private static string Key(int[] thresholds) => string.Join(",", thresholds.Select(t => t.ToString("D3")));
    }
}
