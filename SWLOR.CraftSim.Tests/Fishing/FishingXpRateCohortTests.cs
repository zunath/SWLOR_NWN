using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
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
        public void TheFight_PaysTodaysXpAtTheBite_AndTheRestOfTheBandsMultipliedXpInProportionToHowLongItLasts()
        {
            var parity = new FishingParity(10, 1);
            var cohort = new FishingXpRateCohort(parity);
            var xpCase = cohort.Cases(Context()).Single(c => c.Group == "41-52");

            var legacy = cohort.PlayLegacy(xpCase, Context());
            var fight = cohort.PlayReplacement(xpCase, Context());

            fight.Runs.Should().Be(legacy.Runs);
            var expected = cohort.Mix("41-52").Sum(entry =>
            {
                var baseXp = FishingCatchModel.XpPerBite(entry.Rank, entry.Level);
                var fightXp = CraftMinigameXp.ScaleBaseXp(baseXp, LineRules.XpMultiplierLevels41Up);
                var share = parity.Catch(entry.Level, entry.Rarity, entry.Rank, entry.RodTier).Mean(turns => LineRules.XpShare(turns * 8.0));
                return (baseXp + (fightXp - baseXp) * share) * entry.Weight;
            });
            fight.XpPerRun.Should().BeApproximately(expected, 1e-6, "XP is paid per bite, caught or not, at the band's multiplier");
            fight.XpPerRun.Should().BeGreaterThan(legacy.XpPerRun);
            fight.XpPerRun.Should().BeLessThan(legacy.XpPerRun * LineRules.XpMultiplierLevels41Up, "a fight shorter than the full turns pays only a share of the rest");
            fight.ActionsPerRun.Should().BeInRange(2, 10);
            cohort.ReplacementTiming.Should().Be(new XpLoopTiming(8, 7));
        }

        [Test]
        public void ASampleMean_WeighsEachFightByItsOwnTurns()
        {
            var sample = new LineCatchSample(4, 2, 1, 1, 3 + 3 + 5 + 9, new[] { 0, 0, 0, 2, 0, 1, 0, 0, 0, 1 });

            sample.Mean(turns => turns).Should().BeApproximately(5.0, 1e-9);
            sample.Mean(turns => Math.Min(turns, 5)).Should().BeApproximately((3 + 3 + 5 + 5) / 4.0, 1e-9);
            sample.Equals(sample with { TurnCounts = new[] { 0, 0, 0, 2, 0, 1, 0, 0, 0, 1 } }).Should().BeTrue("counts compare by value");
        }

        [Test]
        public void WithNoMultiplier_TheFightPaysTodaysXpPerBite()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1), null, _ => 1.0);
            var xpCase = cohort.Cases(Context()).Single(c => c.Group == "21-30");

            cohort.PlayReplacement(xpCase, Context()).TotalXp.Should().BeApproximately(cohort.PlayLegacy(xpCase, Context()).TotalXp, 1e-6);
        }

        // The multipliers are the least step that passes the report's runs and seed, so these tests play the report's 1000 fights per case.
        // The two tests share one played set, which takes about a minute.
        private const int MultiplierRuns = 1000;

        private static readonly Lazy<FishingParity> ReportParity = new(() =>
        {
            var parity = new FishingParity(MultiplierRuns, 1);
            new FishingXpRateCohort(parity).Prepare();
            return parity;
        });

        private static bool InBand(string band, int level) => band switch
        {
            "0-10" => level <= 10,
            "11-20" => level is >= 11 and <= 20,
            "21-30" => level is >= 21 and <= 30,
            "31-40" => level is >= 31 and <= 40,
            _ => level >= 41
        };

        [Test]
        public void TheXpRateGate_PassesInEveryBand_WithTheMultipliersTheGameServerApplies()
        {
            var cohort = new FishingXpRateCohort(ReportParity.Value);

            var result = new XpRateGate(cohort).Evaluate(Context());

            result.Verdict.Should().Be(GateVerdict.Passed, result.Summary);
            result.Tables.Single().Rows.Should().OnlyContain(row => row[10] == "Passed");
        }

        [Test]
        public void EveryMultiplier_IsTheLeastStepOfOneTwentiethThatPassesItsBand()
        {
            var parity = ReportParity.Value;

            foreach (var (band, firstLevel) in new[] { ("0-10", 0), ("11-20", 11), ("21-30", 21), ("31-40", 31), ("41-52", 41) })
            {
                var shipped = LineRules.XpMultiplierForLevel(firstLevel);
                var lower = new FishingXpRateCohort(parity, null, level => LineRules.XpMultiplierForLevel(level) - (InBand(band, level) ? 0.05 : 0));

                var result = new XpRateGate(lower).Evaluate(Context());

                var row = result.Tables.Single().Rows.Single(r => r[0] == band);
                row[10].Should().Be("Failed", $"{shipped - 0.05}x is below what band {band} needs, so {shipped}x is the least step that passes");
            }
        }

        [Test]
        public void ATableWithOnlyADayPoolOrOnlyANightPool_KeepsItsHalfOfTheWeight()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1));
            var levels = FishingCatchModel.Fish().ToDictionary(fish => fish.Type, fish => fish.Level);
            var tables = FishingCatchModel.Pools().GroupBy(pool => (pool.Location, pool.Rod, pool.Bait)).ToList();
            Assume.That(tables.Any(table => table.Count() == 1), "the data has a table with one pool; otherwise there is nothing to keep");

            var expected = new Dictionary<(string Band, FishType Fish), double>();
            foreach (var pool in tables.SelectMany(table => table))
            {
                var total = pool.Entries.Sum(entry => (double)entry.Frequency);
                foreach (var entry in pool.Entries)
                {
                    var level = levels[entry.Fish];
                    if (FishingRodTier.Tier(pool.Rod) != FishingParity.NominalRodTier(Math.Min(level, 50)))
                        continue;

                    var band = level <= 10 ? "0-10" : level <= 20 ? "11-20" : level <= 30 ? "21-30" : level <= 40 ? "31-40" : "41-52";
                    expected[(band, entry.Fish)] = expected.GetValueOrDefault((band, entry.Fish)) + 0.5 * entry.Frequency / total;
                }
            }

            foreach (var xpCase in cohort.Cases(Context()))
            {
                var sum = expected.Where(pair => pair.Key.Band == xpCase.Group).Sum(pair => pair.Value);
                foreach (var entry in cohort.Mix(xpCase.Group))
                    entry.Weight.Should().BeApproximately(expected[(xpCase.Group, entry.Fish)] / sum, 1e-9, $"{xpCase.Group} {entry.Fish}");
            }
        }

        [Test]
        public void TheXpRateGate_WithNoMultiplier_ShowsWhatTheFightsTimeCostsAndTheMultiplierThatWouldRestoreTodaysRate()
        {
            var cohort = new FishingXpRateCohort(new FishingParity(10, 1), null, _ => 1.0);
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
