using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using static SWLOR.Game.Server.Tests.Service.Search.SearchTestData;

namespace SWLOR.Game.Server.Tests.Service.Search;

/// <summary>Covers the words the Search window shows: the chances a button promises, and what the board never gives away.</summary>
public class SearchActionTextTests
{
    [Test]
    public void ASearchOfUnknownGround_NamesBothChances_AndNeverWhichOneApplies()
    {
        var engine = Engine();

        // The top is promising and the center is ordinary; the scavenger has learned about neither.
        var promising = SearchActionText.Describe(engine, Top, SearchActionIds.Search);
        var ordinary = SearchActionText.Describe(engine, Center, SearchActionIds.Search);

        promising.Summary.Should().Be(ordinary.Summary, "the window must not give the hidden layout away");
        promising.Summary.Should().Be("Finds 10% on ordinary ground, 45% on promising ground. Uses 1 search.");
        promising.Tooltip.Should().Be(ordinary.Tooltip);
        promising.Kind.Should().Be(SearchActionKind.Normal);
    }

    [Test]
    public void ASearchOfKnownGround_ShowsTheExactChance()
    {
        var engine = Engine();
        engine.Perform(Sift(Center));

        SearchActionText.Describe(engine, Top, SearchActionIds.Search).Summary.Should().Be("Finds 45% of the time on promising ground. Uses 1 search.");
        SearchActionText.Describe(engine, Left, SearchActionIds.Search).Summary.Should().Be("Finds 10% of the time on ordinary ground. Uses 1 search.");
    }

    [Test]
    public void ASearchOfKnownGroundWithAZeroChance_SaysSo_InsteadOfFailingOnTheMissingFindOutcome()
    {
        // A zero chance leaves the preview with no Find outcome. The words come from the setup, not from the outcome list.
        var setup = Setup(ordinary: 0, promisingChance: 0);
        var engine = Engine(setup);
        engine.Perform(Sift(Center));

        var ordinary = SearchActionText.Describe(engine, Left, SearchActionIds.Search);
        var promising = SearchActionText.Describe(engine, Top, SearchActionIds.Search);

        engine.Preview(SearchAt(Left)).Outcomes.Should().NotContain(outcome => outcome.Label == "Find");
        ordinary.Summary.Should().Be("Finds 0% of the time on ordinary ground. Uses 1 search.");
        promising.Summary.Should().Be("Finds 0% of the time on promising ground. Uses 1 search.");
        SearchActionText.Describe(engine, TopLeft, SearchActionIds.Search).Summary.Should().Be("Finds 0% on ordinary ground, 0% on promising ground. Uses 1 search.");
    }

    [Test]
    public void ADigShowsEveryResultAndItsChance_AndIsColouredAsRisky()
    {
        var engine = Engine(100);
        engine.Perform(SearchAt(Center));

        var view = SearchActionText.Describe(engine, Center, SearchActionIds.DigDeeper);

        view.Summary.Should().Be("find 38%, nothing 37%, spoils the spot 25%. Uses 1 search.");
        view.Kind.Should().Be(SearchActionKind.Risky);
    }

    [Test]
    public void ASiftSaysHowManySpotsItShows()
    {
        var engine = Engine();

        SearchActionText.Describe(engine, Center, SearchActionIds.Sift).Summary.Should().Be("Shows the ground at 4 spots next to it. Uses 1 search.");
        SearchActionText.Describe(engine, TopLeft, SearchActionIds.Sift).Summary.Should().Be("Shows the ground at 2 spots next to it. Uses 1 search.");
    }

    [Test]
    public void TheLastSearchIsMarkedOnTheActionThatSpendsIt()
    {
        var engine = Engine(Setup(searches: 2), 100);
        engine.Perform(SearchAt(Center));

        var view = SearchActionText.Describe(engine, TopLeft, SearchActionIds.Search);

        view.Summary.Should().EndWith(SearchActionText.LastSearch);
        view.Kind.Should().Be(SearchActionKind.LastSearch);
    }

    [Test]
    public void ADisabledActionShowsWhyInPlaceOfTheResult()
    {
        var engine = Engine();

        var dig = SearchActionText.Describe(engine, Center, SearchActionIds.DigDeeper);

        dig.Kind.Should().Be(SearchActionKind.Unavailable);
        dig.Summary.Should().Be("Search the spot first.");
    }

    [Test]
    public void ASpotsButtonNamesItAndWhatTheScavengerKnowsOfIt()
    {
        var engine = Engine(100, 50);

        SearchActionText.SpotButtonText(engine, Top).Should().Be("Top\nUnknown");

        engine.Perform(Sift(Center));
        SearchActionText.SpotButtonText(engine, Top).Should().Be("Top\nPromising");
        SearchActionText.SpotButtonText(engine, Left).Should().Be("Left\nOrdinary");

        engine.Perform(SearchAt(Top));
        SearchActionText.SpotButtonText(engine, Top).Should().Be("Top\nEmpty");
        engine.Perform(Dig(Top));
        SearchActionText.SpotState(engine, Top).Should().Be("searched, found nothing, dug once");
    }

    [Test]
    public void AFoundSpotAndASpoiledSpotAreSaidSo()
    {
        var engine = Engine(1, 100);
        engine.Perform(SearchAt(Top));
        SearchActionText.SpotButtonText(engine, Top).Should().Be("Top\nFound");

        engine.Perform(Dig(Top));
        SearchActionText.SpotButtonText(engine, Top).Should().Be("Top\nSpoiled");
        SearchActionText.SpotTooltip(engine, Top).Should().Be("Top: promising ground, spoiled.");
    }

    [Test]
    public void TheStatusLineSaysWhatTheLastActionDid()
    {
        var engine = Engine(1, 1, 100);

        engine.Perform(SearchAt(Top));
        SearchActionText.EventText(engine, engine.Last).Should().Be("You search the top spot and find something.");

        engine.Perform(Dig(Top));
        SearchActionText.EventText(engine, engine.Last).Should().Be("You dig deeper at the top spot and turn up a find.");

        engine.Perform(Sift(Center));
        SearchActionText.EventText(engine, engine.Last).Should().Be("You sift the ground around the center spot. Ordinary: left, right, bottom.");
        SearchActionText.EventText(engine, SearchEvent.None).Should().BeEmpty();
    }

    [Test]
    public void ASiftNamesThePromisingAndTheOrdinarySpotsItShowed()
    {
        var engine = Engine();
        engine.Perform(Sift(Center));

        SearchActionText.SiftResultText(engine, engine.Last).Should().Be("Promising: top. Ordinary: left, right, bottom.");
    }

    [Test]
    public void EveryTextTheWindowShowsIsAsciiPunctuation()
    {
        var engine = Engine(Setup(searches: 9), 1, 1, 100, 100);
        engine.Perform(SearchAt(Top));
        engine.Perform(Dig(Top));
        engine.Perform(SearchAt(Center));
        engine.Perform(Sift(Left));
        var texts = new List<string> { SearchActionText.SearchesText(engine), SearchActionText.LastSearch };
        for (var spot = 0; spot < SearchBoard.Spots; spot++)
        {
            texts.Add(SearchActionText.SpotButtonText(engine, spot));
            texts.Add(SearchActionText.SpotTooltip(engine, spot));
            foreach (var action in new[] { SearchActionIds.Search, SearchActionIds.DigDeeper, SearchActionIds.Sift })
            {
                var view = SearchActionText.Describe(engine, spot, action);
                texts.Add(view.Summary);
                texts.Add(view.Tooltip);
            }
        }

        texts.Add(SearchActionText.EventText(engine, engine.Last));
        texts.Add(SearchActionText.SiftResultText(engine, engine.Last));
        texts.Should().OnlyContain(text => text.All(character => character < 128));
    }

    [Test]
    public void TheSearchesLeftAreShownOutOfTheTotal()
    {
        var engine = Engine();
        engine.Perform(Sift(Center));

        SearchActionText.SearchesText(engine).Should().Be("Searches left: 4 of 5");
    }
}
