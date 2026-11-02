using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DuelService;
using SWLOR.Game.Server.Service.TournamentService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// What a tournament says in the Gazette and at login: plain ASCII, and no one named except by how strangers see them.
/// </summary>
public class TournamentTextTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static CityTournament Tournament(int entrants)
    {
        var tournament = new CityTournament { Id = "t1", Prize = 50_000, EntryFee = 1_000, StartsUtc = Now.AddHours(5) };
        for (var i = 1; i <= entrants; i++)
            tournament.Entrants.Add(new TournamentEntrant { PlayerId = $"p{i}", Label = $"a stranger {i}" });

        tournament.Matches = TournamentRules.BuildBracket(tournament.Entrants.Select(x => x.PlayerId).ToList());
        return tournament;
    }

    private static string AllTexts()
    {
        var tournament = Tournament(4);
        var match = tournament.Matches[0];
        TournamentRules.ApplyDuelOutcome(match, new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Challenger), Now);
        var victory = TournamentText.BuildMatchText(tournament, match);

        match.Result = TournamentMatchResult.Forfeit;
        var forfeit = TournamentText.BuildMatchText(tournament, match);

        match.Result = TournamentMatchResult.Official;
        var official = TournamentText.BuildMatchText(tournament, match);

        match.WinnerId = string.Empty;
        match.Result = TournamentMatchResult.BothForfeit;
        var both = TournamentText.BuildMatchText(tournament, match);

        return string.Join("\n",
            TournamentText.BuildPostedText(50_000, 1_000, Now),
            TournamentText.BuildPostedText(0, 0, Now),
            TournamentText.BuildBeganText(4),
            TournamentText.BuildChampionText("a tall human", 50_000),
            TournamentText.BuildChampionText("a tall human", 0),
            TournamentText.BuildCancelledText("The Dueling Ring was taken down."),
            TournamentText.BuildCancelledText(string.Empty),
            TournamentText.BuildNoChampionText(),
            victory, forfeit, official, both,
            TournamentText.BuildSignUpNotice("Rivertown", "Dantooine", 50_000, 1_000, Now.AddHours(5), Now),
            TournamentText.BuildSignUpNotice("Rivertown", string.Empty, 0, 0, Now.AddHours(5), Now));
    }

    [Test]
    public void EveryText_IsPlainAscii()
    {
        AllTexts().Should().MatchRegex("^[\\x0A\\x20-\\x7E]+$");
    }

    [Test]
    public void ThePostedLine_SaysThePrizeTheFeeAndWhenSignUpsClose()
    {
        var text = TournamentText.BuildPostedText(50_000, 1_000, Now);

        text.Should().Contain("50,000 credits").And.Contain("1,000 credits").And.Contain("2026-11-02 12:00 UTC");
        TournamentText.BuildPostedText(0, 0, Now).Should().Contain("free entry");
    }

    [Test]
    public void TheMatchLines_NameDuelistsByTheirLabelAndTheRound()
    {
        var tournament = Tournament(4);
        var match = tournament.Matches.Single(x => x.Round == 1 && x.Slot == 0);
        TournamentRules.ApplyDuelOutcome(match, new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Challenger), Now);

        TournamentText.BuildMatchText(tournament, match)
            .Should().Be("A stranger 1 defeated a stranger 3 in the semifinal of the tournament.");
    }

    [Test]
    public void AForfeitLine_NamesTheOneWhoForfeited()
    {
        var tournament = Tournament(2);
        var match = tournament.Matches.Single();
        TournamentRules.ApplyNoShow(match, NoShowDecision.PlayerBForfeits);

        TournamentText.BuildMatchText(tournament, match)
            .Should().Be("A stranger 2 forfeited to a stranger 1 in the final of the tournament.");
    }

    [Test]
    public void AnOfficialsDecision_IsSaidToBeAnOfficialsDecisionAfterThreeInterruptions()
    {
        var tournament = Tournament(2);
        var match = tournament.Matches.Single();
        for (var i = 0; i < 3; i++)
        {
            TournamentRules.MarkActive(match);
            TournamentRules.RecordInterruption(match, Now);
        }

        TournamentRules.Decide(match, "p2", "official");

        var text = TournamentText.BuildMatchText(tournament, match);
        text.Should().Contain("interrupted 3 times").And.Contain("an official decided it").And.Contain("in favor of a stranger 2");
    }

    [Test]
    public void ABothForfeitLine_SaysNeitherAdvances()
    {
        var tournament = Tournament(2);
        var match = tournament.Matches.Single();
        TournamentRules.ApplyNoShow(match, NoShowDecision.BothForfeit);

        TournamentText.BuildMatchText(tournament, match).Should().Contain("neither advances");
    }

    [Test]
    public void AByeAndAnEmptyMatch_LeaveNoNews()
    {
        var tournament = Tournament(3);
        var bye = tournament.Matches.Single(x => x.Result == TournamentMatchResult.Bye);

        TournamentText.BuildMatchText(tournament, bye).Should().BeEmpty();
    }

    [Test]
    public void ALabelThatIsMissing_FallsBackToSomeone()
    {
        TournamentText.GetLabel(Tournament(2), "unknown").Should().Be("Someone");
        TournamentText.GetLabel(Tournament(2), string.Empty).Should().Be("no one");
    }

    [Test]
    public void TheLoginNotice_NamesTheCityThePrizeTheFeeAndTheTimeLeft()
    {
        var text = TournamentText.BuildSignUpNotice("Rivertown", "Dantooine", 50_000, 1_000, Now.AddHours(5), Now);

        text.Should().Contain("Rivertown").And.Contain("Dantooine").And.Contain("50,000 credits").And.Contain("1,000 credits");
        text.Should().Contain("Sign-ups close in").And.Contain("Sign up at the ring.");
        TournamentText.BuildSignUpNotice("Rivertown", "", 50_000, 0, Now.AddHours(5), Now).Should().Contain("Entry is free.");
    }
}
