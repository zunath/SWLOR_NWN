using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Scenario;

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

            cases.Select(xpCase => xpCase.Group).Should().Equal("0-9", "10-19", "20-29", "30-39", "40-49", "50-52");
            cases.Should().OnlyContain(xpCase => xpCase.Label == "rank = fish level (at most 50)");
            cohort.Name.Should().Be("Fishing");
            cohort.RunName.Should().Be("bite");
        }

        [Test]
        public void TodaysLoop_PaysXpPerBiteWithNoActions_AndSpendsOnlyTheCast()
        {
            var parity = new FishingParity(10, 1);
            var cohort = new FishingXpRateCohort(parity);
            var xpCase = cohort.Cases(Context()).Single(c => c.Group == "20-29");

            var sample = cohort.PlayLegacy(xpCase, Context());

            var fish = FishingCatchModel.Fish().Where(f => f.Level is >= 20 and <= 29).ToList();
            sample.Runs.Should().Be(fish.Count * 10);
            sample.TotalActions.Should().Be(0);
            sample.TotalXp.Should().Be(fish.Sum(f => FishingCatchModel.XpPerBite(f.Level, f.Level) * 10.0));
            cohort.LegacyTiming.Should().Be(new XpLoopTiming(0, 7));
        }

        [Test]
        public void TheFight_PaysTheSameXpPerBite_AndTakesTurns()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));
            var xpCase = cohort.Cases(Context()).Single(c => c.Group == "50-52");

            var legacy = cohort.PlayLegacy(xpCase, Context());
            var fight = cohort.PlayReplacement(xpCase, Context());

            fight.Runs.Should().Be(legacy.Runs);
            fight.TotalXp.Should().Be(legacy.TotalXp, "XP is paid per bite, caught or not");
            fight.TotalActions.Should().BeGreaterThan(0);
            fight.ActionsPerRun.Should().BeInRange(2, 10);
            cohort.ReplacementTiming.Should().Be(new XpLoopTiming(8, 7));
        }

        [Test]
        public void TheXpRateGate_ShowsWhatTheFightsTimeCostsAndTheMultiplierThatWouldRestoreTodaysRate()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));

            var result = new XpRateGate(cohort).Evaluate(Context());

            result.Gate.Should().Be(GateNames.XpRate);
            result.Verdict.Should().Be(GateVerdict.Failed, "a fight adds turns to a bite that pays the same XP");
            result.Tables.Single().Rows.Should().HaveCount(6);
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

        [Test]
        public void AnUnknownCaseIsRefused()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));

            var act = () => cohort.PlayLegacy(new XpRateCase("90-99", "x"), Context());

            act.Should().Throw<ArgumentException>();
        }
    }
}
