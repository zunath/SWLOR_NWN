using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Search;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.CraftSim.Tests.Search
{
    [TestFixture]
    public class SearchParityTests
    {
        [Test]
        public void ACase_IsPlayedOnce_AndTheSameSeedGivesTheSameSample()
        {
            var first = new SearchParity(40, 4);
            var second = new SearchParity(40, 4);

            var a = first.Sample(30, 3, SearchPlayerKind.Skilled);

            first.Sample(30, 3, SearchPlayerKind.Skilled).Should().Be(a);
            second.Sample(30, 3, SearchPlayerKind.Skilled).Should().Be(a);
            a.Runs.Should().Be(40);
            a.ActionsPerBoard.Should().Be(SearchSetup.ForRank(30, 3).Searches);
        }

        [Test]
        public void TheOrderCasesArePlayedIn_NeverChangesTheirDice()
        {
            var keys = new[]
            {
                new SearchKey(0, 1, SearchPlayerKind.Careless, SearchEffects.None),
                new SearchKey(20, 2, SearchPlayerKind.Skilled, SearchEffects.None),
                new SearchKey(50, 5, SearchPlayerKind.Oracle, new SearchEffects(ExtraPromisingSpots: 1))
            };
            var forward = new SearchParity(30, 9);
            var backward = new SearchParity(30, 9);

            var a = keys.Select(key => forward.Sample(key)).ToList();
            var b = keys.Reverse().Select(key => backward.Sample(key)).Reverse().ToList();
            var prepared = new SearchParity(30, 9);
            prepared.Prepare(keys);

            b.Should().Equal(a);
            keys.Select(key => prepared.Sample(key)).Should().Equal(a);
        }

        [Test]
        public void ADifferentSeedOrADifferentCaseGivesDifferentDice()
        {
            var key = new SearchKey(20, 1, SearchPlayerKind.Careless, SearchEffects.None);

            SearchParity.Stream(key).Should().NotBe(SearchParity.Stream(key with { Rank = 21 }));
            SearchParity.Stream(key).Should().NotBe(SearchParity.Stream(key with { Player = SearchPlayerKind.Skilled }));
            SearchParity.Stream(key).Should().NotBe(SearchParity.Stream(key with { Effects = new SearchEffects(DigRiskPercent: 50) }));
            new SearchParity(40, 1).Sample(key).Should().NotBe(new SearchParity(40, 2).Sample(key));
        }

        [Test]
        public void ASampleCountsEveryFindSiftAndDig()
        {
            var sample = new SearchParity(200, 6).Sample(35, 1, SearchPlayerKind.Skilled);

            sample.Finds.Should().BeApproximately(sample.Ordinary + sample.Deeper, 1e-9);
            sample.SuccessesPerBoard.Should().BeApproximately(sample.Finds, 1e-9);
            sample.FailuresPerBoard.Should().BeApproximately(sample.ActionsPerBoard - sample.SuccessesPerBoard, 1e-9);
            sample.SiftsPerBoard.Should().BeGreaterThan(0, "the skilled player sifts");
            sample.AttemptsPerBoard.Should().BeApproximately(sample.ActionsPerBoard - sample.SiftsPerBoard, 1e-9);
        }

        [Test]
        public void ARunsCountBelowOneIsRefused()
        {
            var parity = () => new SearchParity(0, 1);

            parity.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
