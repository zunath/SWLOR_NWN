using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using static SWLOR.Game.Server.Tests.Service.Search.SearchTestData;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>Covers every Search action, the dig risk and the spoiling of a spot, Sift, the end of the board and the shared engine contract.</summary>
public class SearchEngineTests
{
    [Test]
    public void ANewBoard_HasAllItsSearchesAndNoFinds_AndOffersThreeActionsAtEverySpot()
    {
        var engine = Engine();

        engine.Type.Should().Be(CraftMinigameType.Search);
        engine.Status.Should().Be(MinigameStatus.InProgress);
        engine.SearchesLeft.Should().Be(5);
        engine.SearchesSpent.Should().Be(0);
        engine.Finds.Should().Be(0);
        engine.Attempts.Should().Be(0);
        engine.Last.Should().Be(SearchEvent.None);
        engine.NextOpportunity.Should().BeNull("the board telegraphs nothing; the ground the scavenger has learned is the information");
        engine.Actions.Should().HaveCount(27);
        engine.Actions.Select(action => action.Id).Should().OnlyHaveUniqueItems();
        for (var spot = 0; spot < SearchBoard.Spots; spot++)
        {
            engine.Knowledge(spot).Should().Be(SearchSpotKnowledge.Unknown);
            engine.IsSearched(spot).Should().BeFalse();
            engine.IsSpoiled(spot).Should().BeFalse();
        }
    }

    [Test]
    public void TheSharedMetersCountTheSearches_AndAreDescribedInTheBoardsOwnWords()
    {
        var engine = Engine();

        engine.Meters.Progress.Should().Be(new MinigameMeter(0, 5), "searches spent is the progress meter");
        engine.Meters.Cp.Should().Be(new MinigameMeter(0, 0), "the board has no CP");
        engine.DescribeMeters().Select(meter => (meter.Id, meter.Label)).Should().Equal(
            (MinigameMeterIds.Progress, "Searches used"),
            (SearchEngine.FindsMeterId, "Finds"));
    }

    [Test]
    public void ThePromisingSpotsAreLaidOutAtRandomWhenNoneIsFixed_AndAreNeverShownDirectly()
    {
        // Picks spot 0, then the seventh spot still in the shuffle (spot 7).
        var random = new ScriptedMinigameRandom(0, 7);

        var engine = new SearchEngine(random, Setup());

        random.Calls.Should().Be(2, "one pick for each promising spot");
        engine.Board.Promising.Should().Be(SearchBoard.Bit(0) | SearchBoard.Bit(7));
        for (var spot = 0; spot < SearchBoard.Spots; spot++)
            engine.Knowledge(spot).Should().Be(SearchSpotKnowledge.Unknown, "the layout is hidden");
    }

    [Test]
    public void AFixedLayoutMustHaveTheSetupsPromisingSpots()
    {
        var wrongCount = () => EngineWithLayout(Setup(promising: 2), SearchBoard.Bit(1));
        var offTheBoard = () => EngineWithLayout(Setup(promising: 1), 1 << 9);

        wrongCount.Should().Throw<ArgumentException>();
        offTheBoard.Should().Throw<ArgumentException>();
    }

    [TestCase(Top, 45, "promising")]
    [TestCase(TopLeft, 10, "ordinary")]
    [TestCase(BottomRight, 45, "promising")]
    [TestCase(Center, 10, "ordinary")]
    public void ASearchFindsWithTheChanceOfTheGroundAtTheSpot(int spot, int chance, string ground)
    {
        var engine = Engine();

        var preview = engine.Preview(SearchAt(spot));

        preview.Enabled.Should().BeTrue(ground);
        preview.CpCost.Should().Be(0);
        preview.IntegrityCost.Should().Be(0);
        preview.Outcomes.Select(outcome => (outcome.Label, outcome.ChancePercent)).Should().Equal(("Find", chance), ("Nothing", 100 - chance));
    }

    [Test]
    public void ASearchThatFinds_CostsASearch_AndCountsAnOrdinaryFind()
    {
        var engine = Engine(FirstOutcome);

        var result = engine.Perform(SearchAt(Top));

        result.Performed.Should().BeTrue();
        result.Outcome.Label.Should().Be("Find");
        engine.SearchesSpent.Should().Be(1);
        engine.OrdinaryFinds.Should().Be(1);
        engine.DeeperFinds.Should().Be(0);
        engine.Attempts.Should().Be(1);
        engine.SuccessfulAttempts.Should().Be(1);
        engine.IsSearched(Top).Should().BeTrue();
        engine.HasFound(Top).Should().BeTrue();
        engine.Last.Should().Be(new SearchEvent(SearchEventKind.Search, Top, SearchEventResult.Found));
        engine.Last.IsAttempt.Should().BeTrue();
        engine.Last.IsFind.Should().BeTrue();
    }

    [Test]
    public void ASearchThatFindsNothing_StillCostsASearchAndCountsAnAttempt()
    {
        var engine = Engine(LastOutcome);

        engine.Perform(SearchAt(Top)).Outcome.Label.Should().Be("Nothing");

        engine.SearchesSpent.Should().Be(1);
        engine.Finds.Should().Be(0);
        engine.Attempts.Should().Be(1);
        engine.SuccessfulAttempts.Should().Be(0);
        engine.IsSearched(Top).Should().BeTrue();
        engine.HasFound(Top).Should().BeFalse();
        engine.Last.Result.Should().Be(SearchEventResult.Nothing);
        engine.Last.IsFind.Should().BeFalse();
    }

    [Test]
    public void SearchingASpotShowsItsGround_WhetherOrNotItFound()
    {
        var engine = Engine(LastOutcome, LastOutcome);

        engine.Perform(SearchAt(Top));
        engine.Perform(SearchAt(Center));

        engine.Knowledge(Top).Should().Be(SearchSpotKnowledge.Promising);
        engine.Knowledge(Center).Should().Be(SearchSpotKnowledge.Ordinary);
        engine.Knowledge(Left).Should().Be(SearchSpotKnowledge.Unknown, "a Search shows only its own spot");
    }

    [Test]
    public void ASpotCanBeSearchedOnce_ThenOnlyDugDeeper()
    {
        var engine = Engine(LastOutcome);
        engine.Perform(SearchAt(Top));

        var preview = engine.Preview(SearchAt(Top));
        var result = engine.Perform(SearchAt(Top));

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Already searched. Dig Deeper searches it again.");
        result.Performed.Should().BeFalse();
        engine.SearchesSpent.Should().Be(1, "a refused action spends nothing");
    }

    [Test]
    public void DigDeeperNeedsASearchedSpot()
    {
        var engine = Engine();

        var preview = engine.Preview(Dig(Top));

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("Search the spot first.");
        engine.Perform(Dig(Top)).Performed.Should().BeFalse();
        engine.SearchesSpent.Should().Be(0);
    }

    [Test]
    public void TheFirstDigRisksTheBaseRisk_AndEachDigAddsAStep_ShownBeforeTheClick()
    {
        // Risk 25, 45, 65 and a 50% find chance on the rest: 38/37/25, 28/27/45, 18/17/65.
        var engine = Engine(Setup(searches: 8), LastOutcome, LastOutcome, LastOutcome, LastOutcome);
        engine.Perform(SearchAt(Center));

        engine.Preview(Dig(Center)).Outcomes.Select(o => (o.Label, o.ChancePercent))
            .Should().Equal(("Find", 38), ("Nothing", 37), ("Spoils the spot", 25));

        engine.Perform(Dig(Center)).Outcome.Label.Should().Be("Spoils the spot"); // roll 100 is the last outcome, which is the spoil.
    }

    [Test]
    public void TheDigRiskGrowsWithEveryDigAtTheSameSpot()
    {
        // The rolls land on "Nothing" at each dig (50 of 38/37/25, 40 of 28/27/45, 25 of 18/17/65), so the spot survives all three.
        var engine = Engine(Setup(searches: 8), LastOutcome, 50, 40, 25);
        engine.Perform(SearchAt(Center));

        var chances = new List<string>();
        for (var dig = 0; dig < 3; dig++)
        {
            chances.Add(string.Join("/", engine.Preview(Dig(Center)).Outcomes.Select(o => o.ChancePercent)));
            engine.Perform(Dig(Center)).Outcome.Label.Should().Be("Nothing");
            engine.DigsAt(Center).Should().Be(dig + 1);
        }

        chances.Should().Equal("38/37/25", "28/27/45", "18/17/65");
        var fourth = engine.Preview(Dig(Center));
        fourth.Enabled.Should().BeFalse();
        fourth.DisabledReason.Should().Be("Dug as deep as this spot goes.");
    }

    [Test]
    public void ADigThatSpoilsTheSpot_RuinsIt_AndCountsAnAttemptThatFoundNothing()
    {
        var engine = Engine(LastOutcome, LastOutcome);
        engine.Perform(SearchAt(Center));

        var result = engine.Perform(Dig(Center));

        result.Outcome.Label.Should().Be("Spoils the spot");
        engine.IsSpoiled(Center).Should().BeTrue();
        engine.Last.Should().Be(new SearchEvent(SearchEventKind.DigDeeper, Center, SearchEventResult.Spoiled));
        engine.Attempts.Should().Be(2);
        engine.SuccessfulAttempts.Should().Be(0);
        engine.DeeperFinds.Should().Be(0);
        engine.SearchesSpent.Should().Be(2, "a dig costs a search");
    }

    [Test]
    public void ASpoiledSpot_CanBeNeitherSearchedNorDugAgain()
    {
        var engine = Engine(LastOutcome, LastOutcome);
        engine.Perform(SearchAt(Center));
        engine.Perform(Dig(Center));

        engine.Preview(Dig(Center)).DisabledReason.Should().Be("The ground here is spoiled.");
        engine.Preview(SearchAt(Center)).DisabledReason.Should().Be("The ground here is spoiled.");
        engine.Preview(Dig(Center)).Enabled.Should().BeFalse();
        engine.Preview(Sift(Center)).Enabled.Should().BeTrue("sifting a spoiled spot still shows the ground around it");
    }

    [Test]
    public void ADigThatFinds_CountsADeeperFind_AndLeavesTheSpotToBeDugAgain()
    {
        var engine = Engine(LastOutcome, FirstOutcome);
        engine.Perform(SearchAt(Center));

        var result = engine.Perform(Dig(Center));

        result.Outcome.Label.Should().Be("Find");
        engine.DeeperFinds.Should().Be(1);
        engine.OrdinaryFinds.Should().Be(0);
        engine.Finds.Should().Be(1);
        engine.SuccessfulAttempts.Should().Be(1);
        engine.HasFound(Center).Should().BeTrue();
        engine.IsSpoiled(Center).Should().BeFalse();
        engine.Last.Should().Be(new SearchEvent(SearchEventKind.DigDeeper, Center, SearchEventResult.Found));
        engine.Last.IsFind.Should().BeTrue();
        engine.Preview(Dig(Center)).Enabled.Should().BeTrue();
    }

    [Test]
    public void SteadyDig_HalvesTheRiskAndTheLaterDigsStillGrow()
    {
        var effects = new SearchEffects(DigRiskPercent: 50);
        var engine = Engine(Setup(effects: effects), LastOutcome, 50);
        engine.Perform(SearchAt(Center));

        engine.Preview(Dig(Center)).Outcomes.Select(o => o.ChancePercent).Should().Equal(44, 43, 13);
        engine.Perform(Dig(Center));
        engine.Preview(Dig(Center)).Outcomes.Select(o => o.ChancePercent).Should().Equal(39, 38, 23);
    }

    [Test]
    public void ADigFindBonus_RaisesTheChanceOfAFind_AndNeverPastTheCap()
    {
        var setup = SearchSetup.ForRank(30, 3, new SearchEffects(DigFindBonus: 100));
        var engine = new SearchEngine(new ScriptedMinigameRandom(LastOutcome), setup, Layout.AndSpots(setup.PromisingSpots));
        engine.Perform(SearchAt(Center));

        setup.DigFindChance.Should().Be(SearchRules.MaximumDigFindChance);
        engine.Preview(Dig(Center)).Outcomes.First().Label.Should().Be("Find");
    }

    [Test]
    public void ASiftShowsTheGroundNextToTheSpot_ForOneSearch_AndFindsNothing()
    {
        var engine = Engine(FirstOutcome);

        var result = engine.Perform(Sift(Center));

        result.Performed.Should().BeTrue();
        engine.SearchesSpent.Should().Be(1);
        engine.Attempts.Should().Be(0, "a Sift is not an attempt: it cannot find anything");
        engine.Finds.Should().Be(0);
        engine.Sifts.Should().Be(1);
        engine.Last.Kind.Should().Be(SearchEventKind.Sift);
        engine.Last.IsAttempt.Should().BeFalse();
        engine.Last.Revealed.Should().Be(SearchBoard.Bit(Top) | SearchBoard.Bit(Left) | SearchBoard.Bit(Right) | SearchBoard.Bit(7));
        engine.Knowledge(Top).Should().Be(SearchSpotKnowledge.Promising);
        engine.Knowledge(Left).Should().Be(SearchSpotKnowledge.Ordinary);
        engine.Knowledge(Right).Should().Be(SearchSpotKnowledge.Ordinary);
        engine.Knowledge(7).Should().Be(SearchSpotKnowledge.Ordinary);
        engine.Knowledge(BottomRight).Should().Be(SearchSpotKnowledge.Unknown, "a corner is not next to the center");
        engine.Knowledge(Center).Should().Be(SearchSpotKnowledge.Unknown, "a Sift does not show its own spot");
    }

    [TestCase(Center, 4)]
    [TestCase(TopLeft, 2)]
    [TestCase(Top, 3)]
    public void ASiftReachesTheSpotsAboveBelowAndBesideIt(int spot, int reached)
    {
        var engine = Engine();

        SearchBoard.PopCount(engine.UnknownAround(spot)).Should().Be(reached);
        engine.Preview(Sift(spot)).Outcomes.Should().ContainSingle().Which.ChancePercent.Should().Be(100);
    }

    [Test]
    public void TheSpotsNextToASpot_AreTheOnesAboveBelowAndBesideIt_NeverTheCorners()
    {
        SearchBoard.Neighbors(Center).Should().BeEquivalentTo(new[] { Top, Left, Right, 7 });
        SearchBoard.Neighbors(TopLeft).Should().BeEquivalentTo(new[] { Top, Left });
        SearchBoard.Neighbors(BottomRight).Should().BeEquivalentTo(new[] { Right, 7 });
        SearchBoard.Neighbors(Top).Should().BeEquivalentTo(new[] { TopLeft, 2, Center });
    }

    [Test]
    public void ASiftWithNothingNewToShow_IsRefused_AndSpendsNothing()
    {
        var engine = Engine(FirstOutcome, FirstOutcome);
        engine.Perform(Sift(Center));

        var preview = engine.Preview(Sift(Center));
        var again = engine.Perform(Sift(Center));

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("You already know the ground around here.");
        again.Performed.Should().BeFalse();
        engine.SearchesSpent.Should().Be(1);
    }

    [Test]
    public void ASiftShowsOnlyTheSpotsStillUnknown()
    {
        var engine = Engine(LastOutcome, FirstOutcome);
        engine.Perform(SearchAt(Top));

        engine.Perform(Sift(Center));

        engine.Last.Revealed.Should().Be(SearchBoard.Bit(Left) | SearchBoard.Bit(Right) | SearchBoard.Bit(7), "the top was already known");
    }

    [Test]
    public void EveryActionCostsExactlyOneSearch()
    {
        foreach (var actionId in new[] { SearchAt(Top), Sift(Center) })
        {
            var engine = Engine(FirstOutcome);

            var result = engine.Perform(actionId);

            result.After.Progress.Value.Should().Be(result.Before.Progress.Value + 1, actionId);
        }

        var dig = Engine(LastOutcome, FirstOutcome);
        dig.Perform(SearchAt(Center));
        var digResult = dig.Perform(Dig(Center));
        digResult.After.Progress.Value.Should().Be(digResult.Before.Progress.Value + 1);
    }

    [Test]
    public void TheBoardEndsWhenTheLastSearchIsSpent_AndTheLastActionSaysSo()
    {
        var engine = Engine(Setup(searches: 2), LastOutcome, LastOutcome);
        engine.Perform(SearchAt(Center));

        var preview = engine.Preview(SearchAt(TopLeft));
        var result = engine.Perform(SearchAt(TopLeft));

        preview.FinishesItem.Should().BeTrue("the warning shared with every minigame: this action ends the attempt");
        preview.Outcomes.Should().OnlyContain(outcome => outcome.ResultingStatus == MinigameStatus.Succeeded);
        result.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Status.Should().Be(MinigameStatus.Succeeded);
        engine.SearchesLeft.Should().Be(0);
    }

    [Test]
    public void AnActionBeforeTheLastSearchDoesNotFinishTheBoard()
    {
        var engine = Engine(Setup(searches: 3));

        engine.PreviewAll().Where(preview => preview.Enabled).Should().OnlyContain(preview => !preview.FinishesItem);
    }

    [Test]
    public void OnceTheBoardIsOver_EveryActionIsRefused()
    {
        var engine = Engine(Setup(searches: 1), LastOutcome);
        engine.Perform(SearchAt(Center));

        var preview = engine.Preview(SearchAt(TopLeft));
        var result = engine.Perform(SearchAt(TopLeft));

        preview.Enabled.Should().BeFalse();
        preview.DisabledReason.Should().Be("The attempt is over.");
        result.Performed.Should().BeFalse();
        engine.SearchesSpent.Should().Be(1);
    }

    [Test]
    public void AnUnknownActionIsAnError()
    {
        var engine = Engine();

        foreach (var id in new[] { "search", "search:9", "search:-1", "hunt:3", "dig:", ":3", "sift:x" })
            ((Action)(() => engine.Preview(id))).Should().Throw<ArgumentException>(id);
    }

    [TestCase("search:4", true, "search", 4)]
    [TestCase("dig:0", true, "dig", 0)]
    [TestCase("sift:8", true, "sift", 8)]
    [TestCase("sift:9", false, null, -1)]
    [TestCase("rest:1", false, null, -1)]
    [TestCase("", false, null, -1)]
    public void ActionIdsNameTheActionAndTheSpot(string id, bool valid, string action, int spot)
    {
        SearchActionIds.TryParse(id, out var parsedAction, out var parsedSpot).Should().Be(valid);
        parsedAction.Should().Be(action);
        parsedSpot.Should().Be(spot);
        if (valid)
            SearchActionIds.For(parsedAction, parsedSpot).Should().Be(id);
    }

    [Test]
    public void EveryPreviewShowsTheResultThePerformedActionHas_ForEveryOutcome()
    {
        // A board part-way through: the top searched and dug once, the left sifted around.
        var template = Engine(Setup(searches: 9), FirstOutcome, 50, FirstOutcome);
        template.Perform(SearchAt(Top));
        template.Perform(Dig(Top));
        template.Perform(Sift(Left));

        foreach (var preview in template.PreviewAll().Where(preview => preview.Enabled))
        {
            var lowerRoll = 1;
            foreach (var outcome in preview.Outcomes)
            {
                var copy = template.Fork(new ScriptedMinigameRandom(lowerRoll));
                var result = copy.Perform(preview.Action.Id);

                result.Performed.Should().BeTrue(preview.Action.Id);
                result.Outcome.Label.Should().Be(outcome.Label, preview.Action.Id);
                result.Outcome.ChancePercent.Should().Be(outcome.ChancePercent, preview.Action.Id);
                result.MetersAfter.Select(meter => meter.Value).Should().Equal(
                    outcome.Changes.Select(change => change.After), $"{preview.Action.Id} {outcome.Label}");
                result.Status.Should().Be(outcome.ResultingStatus, preview.Action.Id);
                lowerRoll += outcome.ChancePercent;
            }
        }
    }

    [Test]
    public void EveryOutcomeChanceIsShownAndTheChancesAddUpToOneHundred()
    {
        var engine = Engine(Setup(searches: 9), LastOutcome);
        engine.Perform(SearchAt(Center));

        foreach (var preview in engine.PreviewAll().Where(preview => preview.Enabled))
            preview.Outcomes.Sum(outcome => outcome.ChancePercent).Should().Be(100, preview.Action.Id);
    }

    [Test]
    public void AForkContinuesFromTheSameBoard_AndNeitherChangesTheOther()
    {
        var engine = Engine(Setup(searches: 6), FirstOutcome);
        engine.Perform(SearchAt(Top));
        var fork = (SearchEngine)engine.Fork(new ScriptedMinigameRandom(LastOutcome));

        fork.Perform(SearchAt(Center));

        engine.SearchesSpent.Should().Be(1);
        engine.IsSearched(Center).Should().BeFalse();
        fork.SearchesSpent.Should().Be(2);
        fork.IsSearched(Top).Should().BeTrue("the fork continues from the original's board");
    }

    [Test]
    public void SearchingEverySpotOnceNeedsNineSearches_AndAnEngineWithMoreIsPossible()
    {
        var engine = Engine(Setup(searches: 9), Enumerable.Repeat(LastOutcome, 9).ToArray());

        for (var spot = 0; spot < SearchBoard.Spots; spot++)
            engine.Perform(SearchAt(spot)).Performed.Should().BeTrue();

        engine.Status.Should().Be(MinigameStatus.Succeeded);
        engine.Attempts.Should().Be(9);
    }
}

internal static class SearchLayoutExtensions
{
    /// <summary>A layout with exactly this many promising spots, starting from the test layout's.</summary>
    public static int AndSpots(this int layout, int count)
    {
        var mask = layout;
        for (var spot = 0; SearchBoard.PopCount(mask) < count && spot < SearchBoard.Spots; spot++)
            mask |= SearchBoard.Bit(spot);

        while (SearchBoard.PopCount(mask) > count)
            mask &= mask - 1;

        return mask;
    }
}
