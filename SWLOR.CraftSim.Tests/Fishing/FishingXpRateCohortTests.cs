using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Tests.Fishing
{
    [TestFixture]
    public class FishingXpRateCohortTests
    {
        private static GateContext Context() => new(null, null, 10, 1, AttributeSweep.Default);

        [Test]
        public void ThereIsACaseForEachBandOfFishLevels()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));

            var cases = cohort.Cases(Context());

            cases.Select(xpCase => xpCase.Group).Should().Equal("0-10", "11-20", "21-30", "31-40", "41-52");
            cases.Should().OnlyContain(xpCase => xpCase.Label == "best rod for the rank, rank = fish level (at most 50)");
            cohort.Name.Should().Be("Fishing");
            cohort.RunName.Should().Be("bite");
        }

        [Test]
        public void EveryBandsMix_SumsToOne_AndHoldsOnlyFishTheBestRodForTheirRankReaches()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));

            foreach (var xpCase in cohort.Cases(Context()))
            {
                var mix = cohort.Mix(xpCase.Group);

                mix.Sum(entry => entry.Weight).Should().BeApproximately(1.0, 1e-9, xpCase.Group);
                mix.Should().OnlyContain(entry => entry.Weight > 0);
                mix.Should().OnlyContain(entry => entry.Rank == Math.Min(entry.Level, 50));
                mix.Should().OnlyContain(entry => entry.RodTier == FishingParity.NominalRodTier(entry.Rank));
            }
        }

        [Test]
        public void TheBandsFishAreTheBandsLevels_AndTheTopBandHoldsTheSpecialFish()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));

            cohort.Mix("0-10").Should().OnlyContain(entry => entry.Level >= 0 && entry.Level <= 10);
            cohort.Mix("21-30").Should().OnlyContain(entry => entry.Level >= 21 && entry.Level <= 30);
            var top = cohort.Mix("41-52");
            top.Should().OnlyContain(entry => entry.Level >= 41 && entry.Level <= 52);
            top.Where(entry => entry.Rarity == FishRarity.Rare).Should().NotBeEmpty();
            top.Where(entry => entry.Rarity == FishRarity.Rare).Should().OnlyContain(entry => entry.Rank == 50 && entry.RodTier == 4);
        }

        [Test]
        public void AnUnknownBandIsRefused()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));

            var act = () => cohort.PlayLegacy(new XpRateCase("90-99", "x"), Context());

            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void TodaysLoop_PaysXpPerBiteWithNoActions_AndSpendsOnlyTheCast()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));
            var xpCase = cohort.Cases(Context()).Single(c => c.Group == "21-30");

            var sample = cohort.PlayLegacy(xpCase, Context());

            var expected = cohort.Mix("21-30").Sum(entry => FishingCatchModel.XpPerBite(entry.Rank, entry.Level) * entry.Weight);
            sample.Runs.Should().Be(FishingXpRateCohort.BitesPerCase);
            sample.TotalActions.Should().Be(0);
            sample.XpPerRun.Should().BeApproximately(expected, 1e-6);
            cohort.LegacyTiming.Should().Be(new XpLoopTiming(0, 7));
        }

        [Test]
        public void TheFight_PaysTheSameXpPerBite_AndTakesTurns()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));
            var xpCase = cohort.Cases(Context()).Single(c => c.Group == "41-52");

            var legacy = cohort.PlayLegacy(xpCase, Context());
            var fight = cohort.PlayReplacement(xpCase, Context());

            fight.Runs.Should().Be(legacy.Runs);
            fight.TotalXp.Should().BeApproximately(legacy.TotalXp, 1e-6, "XP is paid per bite, caught or not");
            fight.ActionsPerRun.Should().BeInRange(2, 10);
            cohort.ReplacementTiming.Should().Be(new XpLoopTiming(8, 7));
        }

        [Test]
        public void TheXpRateGate_ShowsWhatTheFightsTimeCostsAndTheMultiplierThatWouldRestoreTodaysRate()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));
            cohort.Prepare();

            var result = new XpRateGate(cohort).Evaluate(Context());

            result.Gate.Should().Be(GateNames.XpRate);
            result.Verdict.Should().Be(GateVerdict.Failed, "a fight adds turns to a bite that pays the same XP");
            result.Tables.Single().Rows.Should().HaveCount(5);
            foreach (var row in result.Tables.Single().Rows)
            {
                double.Parse(row[8]).Should().BeInRange(0.05, 0.35, "seven seconds of cast against seven plus several eight-second turns");
                row[9].Should().EndWith("x");
            }
        }

        [Test]
        public void ALongerTurn_LowersTheRatioFurther()
        {
            var parity = new FishingParity(10, 1);
            var quick = new FishingXpRateCohort(parity, FishingParityOptions.Default with { SecondsPerTurn = 4 });
            var slow = new FishingXpRateCohort(parity, FishingParityOptions.Default with { SecondsPerTurn = 12 });
            var xpCase = quick.Cases(Context()).First();

            var quickRate = quick.PlayReplacement(xpCase, Context()).XpPerHour(quick.ReplacementTiming);
            var slowRate = slow.PlayReplacement(xpCase, Context()).XpPerHour(slow.ReplacementTiming);

            quickRate.Should().BeGreaterThan(slowRate);
        }
    }
}
