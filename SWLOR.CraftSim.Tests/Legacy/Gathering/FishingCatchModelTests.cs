using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy.Gathering;

namespace SWLOR.CraftSim.Tests.Legacy.Gathering
{
    [TestFixture]
    public class FishingCatchModelTests
    {
        [TestCase(0, 0, 10, 60)]
        [TestCase(1, 0, 10, 70)]
        [TestCase(0, 0, 0, 40)]
        [TestCase(5, 0, 10, 95)]
        [TestCase(50, 0, 10, 95)]
        [TestCase(0, 10, 2, 0)]
        [TestCase(5, 10, 8, 6)]
        public void CatchChance_IsFortyPlusTenPerRankAboveTheFishPlusTwoPerWillpowerClampedToZeroAndNinetyFive(
            int rank, int fishLevel, int willpower, int expected)
        {
            FishingCatchModel.CatchChancePercent(rank, fishLevel, willpower).Should().Be(expected);
        }

        [Test]
        public void CatchChance_NeverLeavesZeroToNinetyFive()
        {
            for (var rank = 0; rank <= 50; rank++)
            {
                for (var level = 0; level <= 52; level += 4)
                    FishingCatchModel.CatchChancePercent(rank, level, 14).Should().BeInRange(0, 95);
            }
        }

        [Test]
        public void EveryBitePaysXpWhetherOrNotTheFishLands()
        {
            FishingCatchModel.XpPerBite(0, 5).Should().Be(1050);
            FishingCatchModel.XpPerBite(10, 10).Should().Be(600);
            FishingCatchModel.XpPerBite(30, 10).Should().Be(0);
        }

        [Test]
        public void TheFishAreTheGamesFishByLevel()
        {
            var fish = FishingCatchModel.Fish();

            fish.Should().HaveCountGreaterThan(50);
            fish.Select(f => f.Level).Should().BeInAscendingOrder();
            fish.Min(f => f.Level).Should().Be(0);
            fish.Single(f => f.Name == "Moat Carp").Level.Should().Be(0);
        }

        [Test]
        public void ThePoolsListEachLocationRodAndBaitWithWeightedFish()
        {
            var pools = FishingCatchModel.Pools();
            var levels = FishingCatchModel.Fish().ToDictionary(f => f.Type, f => f.Level);

            pools.Should().NotBeEmpty();
            pools.Should().OnlyContain(p => p.Entries.Count > 0 && p.Entries.All(e => e.Frequency > 0));
            pools.SelectMany(p => p.Entries).Should().OnlyContain(e => levels[e.Fish] == e.Level);
        }

        [Test]
        public void TheSixSpecialFishAreNotPlacedAtRandom_SoTwoBuildsOfThePoolsAgree()
        {
            static List<string> Describe(IReadOnlyList<FishingPool> pools) => pools
                .Select(p => $"{p.Location}/{p.Rod}/{p.Bait}/{p.TimeOfDay}:{string.Join(",", p.Entries.Select(e => $"{e.Fish}x{e.Frequency}"))}")
                .ToList();

            var first = Describe(FishingCatchModel.BuildPools());
            var second = Describe(FishingCatchModel.BuildPools());

            second.Should().Equal(first, "the game places the special fish at random on every start, and the model fixes one placement");
            Describe(FishingCatchModel.Pools()).Should().Equal(first);
        }

        [Test]
        public void APoolsCatchChanceIsItsFishCatchChancesWeightedByFrequency()
        {
            var pool = new FishingPool(default, default, default, default, new[]
            {
                new FishingPoolEntry(default, 0, 3),
                new FishingPoolEntry(default, 10, 1)
            });

            FishingCatchModel.PoolCatchChance(pool, 5, 10).Should().BeApproximately(0.75 * 95 + 0.25 * 10, 1e-9);
        }
    }
}
