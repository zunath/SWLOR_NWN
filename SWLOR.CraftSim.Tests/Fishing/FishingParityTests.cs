using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Fishing;
using SWLOR.Game.Server.Service.CraftService.Minigame.Line;
using SWLOR.Game.Server.Service.FishingService;

namespace SWLOR.CraftSim.Tests.Fishing
{
    [TestFixture]
    public class FishingParityTests
    {
        [TestCase(0, 0)]
        [TestCase(9, 0)]
        [TestCase(10, 1)]
        [TestCase(29, 2)]
        [TestCase(40, 4)]
        [TestCase(50, 4)]
        public void TheNominalRod_IsTheBestTierTheRankCanUse(int rank, int tier)
        {
            FishingParity.NominalRodTier(rank).Should().Be(tier);
        }

        [Test]
        public void ACase_IsPlayedOnce_AndTheSameSeedGivesTheSameSample()
        {
            var first = new FishingParity(25, 4);
            var second = new FishingParity(25, 4);

            var a = first.Catch(25, FishRarity.Common, 25, 2);

            first.Catch(25, FishRarity.Common, 25, 2).Should().Be(a);
            second.Catch(25, FishRarity.Common, 25, 2).Should().Be(a);
            a.Runs.Should().Be(25);
        }

        [Test]
        public void TheOrderCasesArePlayedIn_NeverChangesTheirDice()
        {
            var keys = new[]
            {
                new CatchKey(20, FishRarity.Common, 20, 2, LinePlayer.Adaptive, LineAnglerEffects.None),
                new CatchKey(23, FishRarity.Common, 20, 2, LinePlayer.Adaptive, LineAnglerEffects.None),
                new CatchKey(52, FishRarity.Rare, 50, 4, LinePlayer.RuleOfThumb, LineAnglerEffects.None)
            };
            var forward = new FishingParity(20, 9);
            var backward = new FishingParity(20, 9);
            var parallel = new FishingParity(20, 9);

            foreach (var key in keys)
                forward.Catch(key);
            foreach (var key in keys.Reverse())
                backward.Catch(key);
            parallel.Prepare(keys);

            foreach (var key in keys)
            {
                backward.Catch(key).Should().Be(forward.Catch(key));
                parallel.Catch(key).Should().Be(forward.Catch(key));
            }
        }

        [Test]
        public void EveryPartOfACase_GivesItsOwnStreamOfDice()
        {
            var baseKey = new CatchKey(30, FishRarity.Common, 30, 3, LinePlayer.Adaptive, LineAnglerEffects.None);
            var streams = new[]
            {
                baseKey,
                baseKey with { FishLevel = 31 },
                baseKey with { Rank = 31 },
                baseKey with { RodTier = 4 },
                baseKey with { Rarity = FishRarity.Rare },
                baseKey with { Player = LinePlayer.RuleOfThumb },
                baseKey with { Effects = new LineAnglerEffects(TensionCapBonus: 1) },
                baseKey with { Effects = new LineAnglerEffects(HoldDrainBonus: 1) },
                baseKey with { Effects = new LineAnglerEffects(ForecastDepth: 2) }
            }.Select(FishingParity.Stream).ToList();

            streams.Should().OnlyHaveUniqueItems();
            FishingParity.Stream(baseKey).Should().Be(FishingParity.Stream(baseKey with { }));
        }

        [Test]
        public void AStudyNeedsAtLeastOneFightPerCase()
        {
            ((Action)(() => new FishingParity(0, 1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void AFishAndAPerk_ChangeWhatTheFightIsMadeOf()
        {
            var parity = new FishingParity(60, 1);

            var plain = parity.Catch(33, FishRarity.Common, 30, 3);
            var perked = parity.Catch(33, FishRarity.Common, 30, 3, effects: new LineAnglerEffects(TensionCapBonus: 4, ForecastDepth: 2));

            perked.LandedPercent.Should().BeGreaterThan(plain.LandedPercent);
        }
    }
}
