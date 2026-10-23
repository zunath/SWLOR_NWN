using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Search;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;

namespace SWLOR.CraftSim.Tests.Search
{
    [TestFixture]
    public class SearchPlayerTests
    {
        private static readonly SearchSetup Setup = new(6, 2, 8, 30, 30, 20, 20);

        private static SearchEngine Board(int promisingMask, int seed = 1) =>
            new(SimMinigameRandom.ForRun(seed, 55), Setup, promisingMask);

        [Test]
        public void TheCarelessPlayerSearchesTheSpotsInReadingOrder_AndNeverSiftsOrDigs()
        {
            var engine = Board(SearchBoard.Bit(4) | SearchBoard.Bit(8));
            var player = new SearchPlayer(SearchPlayerKind.Careless);

            var chosen = new List<string>();
            while (engine.Status == MinigameStatus.InProgress)
            {
                var action = player.Choose(engine);
                chosen.Add(action);
                engine.Perform(action);
            }

            chosen.Should().Equal(Enumerable.Range(0, 6).Select(spot => SearchActionIds.For(SearchActionIds.Search, spot)));
            engine.Sifts.Should().Be(0);
            Enumerable.Range(0, SearchBoard.Spots).Sum(spot => engine.DigsAt(spot)).Should().Be(0);
        }

        [Test]
        public void ThePlayersGiveUpWhenTheBoardIsOver()
        {
            var engine = Board(SearchBoard.Bit(0) | SearchBoard.Bit(1));
            var player = new SearchPlayer(SearchPlayerKind.Careless);
            while (engine.Status == MinigameStatus.InProgress)
                engine.Perform(player.Choose(engine));

            foreach (var kind in Enum.GetValues<SearchPlayerKind>())
                new SearchPlayer(kind).Choose(engine).Should().BeNull();
        }

        [Test]
        public void TheSkilledPlayerReadsOnlyWhatTheWindowShows_SoTwoLayoutsGiveTheSameFirstChoice()
        {
            var a = Board(SearchBoard.Bit(0) | SearchBoard.Bit(1));
            var b = Board(SearchBoard.Bit(7) | SearchBoard.Bit(8));
            var player = new SearchPlayer(SearchPlayerKind.Skilled);

            player.Choose(a).Should().Be(player.Choose(b), "nothing is known yet about either board");
        }

        [Test]
        public void TheSkilledPlayerOpensWithASiftOfTheCenter_WhichShowsTheMostGround()
        {
            var player = new SearchPlayer(SearchPlayerKind.Skilled);

            player.Choose(Board(SearchBoard.Bit(2) | SearchBoard.Bit(6))).Should().Be(SearchActionIds.For(SearchActionIds.Sift, 4));
        }

        [Test]
        public void TheSkilledPlayerSearchesTheGroundASiftShowedToBePromising()
        {
            // The top and the left are promising; after sifting the center the skilled player should search one of them.
            var engine = Board(SearchBoard.Bit(1) | SearchBoard.Bit(3));
            engine.Perform(SearchActionIds.For(SearchActionIds.Sift, 4));

            var action = new SearchPlayer(SearchPlayerKind.Skilled).Choose(engine);

            action.Should().BeOneOf(SearchActionIds.For(SearchActionIds.Search, 1), SearchActionIds.For(SearchActionIds.Search, 3));
        }

        [Test]
        public void WhenFreshGroundIsWorthMoreThanADig_TheInformedPlayersSearchItInsteadOfDiggingTheSpotTheyJustSearched()
        {
            // Eight fresh spots are each worth more than a dig (a search finds about 16%, a first dig about 14%) and there are more of
            // them than searches left, so the best split does not need any one of them. The player once took "a chain the split cannot
            // do without", found none, and fell back to the first spot it could act on: a dig of the spot it had just searched.
            var setup = new SearchSetup(5, 2, 12, 30, 30, 20, 20);
            foreach (var kind in new[] { SearchPlayerKind.Skilled, SearchPlayerKind.Oracle })
            {
                var engine = new SearchEngine(SimMinigameRandom.ForRun(1, 56, (int)kind), setup, SearchBoard.Bit(7) | SearchBoard.Bit(8));
                engine.Perform(SearchActionIds.For(SearchActionIds.Search, 0));

                var action = new SearchPlayer(kind).Choose(engine);

                action.Should().NotStartWith(SearchActionIds.DigDeeper, $"{kind} has fresh ground worth more than a dig");
            }
        }

        [Test]
        public void TheOraclePlayerSearchesAPromisingSpotFirst_BecauseItWasToldWhereTheyAre()
        {
            var layout = SearchBoard.Bit(2) | SearchBoard.Bit(6);

            var action = new SearchPlayer(SearchPlayerKind.Oracle).Choose(Board(layout));

            action.Should().BeOneOf(SearchActionIds.For(SearchActionIds.Search, 2), SearchActionIds.For(SearchActionIds.Search, 6));
        }

        [Test]
        public void TheOracleNeverSifts_ItAlreadyKnows()
        {
            var engine = Board(SearchBoard.Bit(0) | SearchBoard.Bit(8));
            var player = new SearchPlayer(SearchPlayerKind.Oracle);
            while (engine.Status == MinigameStatus.InProgress)
                engine.Perform(player.Choose(engine));

            engine.Sifts.Should().Be(0);
        }

        [Test]
        public void EveryActionAPlayerChoosesIsOneTheBoardOffers()
        {
            foreach (var kind in Enum.GetValues<SearchPlayerKind>())
            {
                for (var seed = 1; seed <= 30; seed++)
                {
                    var engine = new SearchEngine(SimMinigameRandom.ForRun(seed, 77, (int)kind), Setup);
                    var player = new SearchPlayer(kind);
                    while (engine.Status == MinigameStatus.InProgress)
                    {
                        var action = player.Choose(engine);

                        action.Should().NotBeNull();
                        engine.Preview(action).Enabled.Should().BeTrue($"{kind} chose {action}");
                        engine.Perform(action);
                    }
                }
            }
        }

        [Test]
        public void ABoardAlwaysTakesAsManyActionsAsTheScavengerHasSearches()
        {
            foreach (var kind in Enum.GetValues<SearchPlayerKind>())
            {
                var engine = SearchParity.PlayBoard(Setup, kind, SimMinigameRandom.ForRun(3, 88, (int)kind));

                engine.Turn.Should().Be(Setup.Searches, kind.ToString());
                engine.Status.Should().Be(MinigameStatus.Succeeded);
            }
        }

        [Test]
        public void TheInformedPlayersFindNoLessThanTheCarelessOne_OnAverage()
        {
            const int runs = 1500;
            double Mean(SearchPlayerKind kind) =>
                Enumerable.Range(0, runs).Average(run =>
                    SearchParity.PlayBoard(Setup, kind, SimMinigameRandom.ForRun(4, 99, (int)kind, run)).Finds);

            var careless = Mean(SearchPlayerKind.Careless);
            var skilled = Mean(SearchPlayerKind.Skilled);
            var oracle = Mean(SearchPlayerKind.Oracle);

            skilled.Should().BeGreaterThan(careless);
            oracle.Should().BeGreaterThan(skilled, "knowing the layout for free beats paying a search to learn part of it");
        }
    }
}
