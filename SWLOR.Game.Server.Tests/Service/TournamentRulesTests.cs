using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.DuelService;
using SWLOR.Game.Server.Service.TournamentService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A city tournament's rules: who can post, sign up and begin, how the bracket is built and advances, no-shows, draws,
/// interruptions, official decisions, restarts and barring.
/// </summary>
public class TournamentRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Starts = Now.AddHours(24);

    private static CityTournament Tournament(int entrants, long prize = 50_000, long fee = 1_000)
    {
        var tournament = new CityTournament
        {
            Id = "tournament-1",
            CityId = "city-1",
            RingPropertyId = "ring-1",
            State = TournamentState.SignUp,
            Prize = prize,
            EntryFee = fee,
            StartsUtc = Starts
        };

        for (var i = 1; i <= entrants; i++)
        {
            tournament.Entrants.Add(new TournamentEntrant
            {
                PlayerId = $"p{i}",
                Label = $"Entrant {i}",
                FeePaid = fee,
                PaymentId = $"pay{i}"
            });
        }

        return tournament;
    }

    private static CityTournament RunningTournament(int entrants)
    {
        var tournament = Tournament(entrants);
        tournament.Matches = TournamentRules.BuildBracket(tournament.Entrants.Select(x => x.PlayerId).ToList());
        tournament.State = TournamentState.Running;
        return tournament;
    }

    // The numbers the plan sets

    [Test]
    public void ANoShow_ForfeitsAfterFiveMinutes_AndAThirdInterruptionGoesToAnOfficial()
    {
        TournamentRules.NoShowMinutes.Should().Be(5.0);
        TournamentRules.InterruptionsBeforeOfficial.Should().Be(3);
        TournamentRules.MinimumEntrants.Should().Be(2);
    }

    [Test]
    public void ThePresets_AreAscendingAndFitInACharactersCredits()
    {
        TournamentRules.PrizeChoices.Should().BeInAscendingOrder().And.OnlyContain(x => x > 0 && x <= TournamentRules.MaxCredits);
        TournamentRules.FeeChoices.Should().BeInAscendingOrder().And.OnlyContain(x => x >= 0 && x <= TournamentRules.MaxCredits);
        TournamentRules.FeeChoices[0].Should().Be(0, "a tournament can be free to enter");
        TournamentRules.LeadHourChoices.Should().BeInAscendingOrder().And.OnlyContain(x => x > 0 && x <= TournamentRules.MaxLeadDays * 24);
    }

    // Posting

    [Test]
    public void CheckPost_AllowsAnOfficialWithAFundedPrize()
    {
        TournamentRules.CheckPost(true, false, 50_000, 1_000, Starts, Now, 50_000).Should().BeEmpty();
    }

    [Test]
    public void CheckPost_AllowsAFreeTournamentWithNoPrize()
    {
        TournamentRules.CheckPost(true, false, 0, 0, Starts, Now, 0).Should().BeEmpty();
    }

    [Test]
    public void CheckPost_RefusesAnyoneWhoIsNotTheMayorOrAnOfficer()
    {
        TournamentRules.CheckPost(false, false, 50_000, 0, Starts, Now, 1_000_000).Should().Contain("mayor or an officer");
    }

    [Test]
    public void CheckPost_RefusesASecondTournamentAtTheSameRing()
    {
        TournamentRules.CheckPost(true, true, 50_000, 0, Starts, Now, 1_000_000).Should().Contain("already has a tournament");
    }

    [Test]
    public void CheckPost_RefusesAPrizeTheOrdinaryTreasuryCannotCover()
    {
        // The caller passes only the ordinary balance: restricted funds can never pay a prize.
        TournamentRules.CheckPost(true, false, 50_001, 0, Starts, Now, 50_000).Should().Contain("Restricted funds cannot pay");
    }

    [TestCase(-1L, 0L)]
    [TestCase(0L, -1L)]
    [TestCase((long)int.MaxValue + 1, 0L)]
    [TestCase(0L, (long)int.MaxValue + 1)]
    public void CheckPost_RefusesAmountsACharacterCannotHold(long prize, long fee)
    {
        TournamentRules.CheckPost(true, false, prize, fee, Starts, Now, long.MaxValue).Should().NotBeEmpty();
    }

    [Test]
    public void CheckPost_RefusesAStartThatIsNotInTheFuture()
    {
        TournamentRules.CheckPost(true, false, 0, 0, Now, Now, 0).Should().Contain("future");
        TournamentRules.CheckPost(true, false, 0, 0, Now.AddMinutes(-1), Now, 0).Should().Contain("future");
    }

    [Test]
    public void CheckPost_RefusesAStartMoreThanTwoWeeksAhead()
    {
        TournamentRules.CheckPost(true, false, 0, 0, Now.AddDays(TournamentRules.MaxLeadDays), Now, 0).Should().BeEmpty();
        TournamentRules.CheckPost(true, false, 0, 0, Now.AddDays(TournamentRules.MaxLeadDays).AddMinutes(1), Now, 0).Should().Contain("days ahead");
    }

    // Signing up

    [Test]
    public void CheckSignUp_AllowsAPlayerWhoCanPayTheFee()
    {
        TournamentRules.CheckSignUp(Tournament(0), "p1", 1_000, Now).Should().BeEmpty();
    }

    [Test]
    public void CheckSignUp_NeedsTheFee_ButAFreeTournamentNeedsNoCredits()
    {
        TournamentRules.CheckSignUp(Tournament(0), "p1", 999, Now).Should().Contain("entry fee is 1,000");
        TournamentRules.CheckSignUp(Tournament(0, fee: 0), "p1", 0, Now).Should().BeEmpty();
    }

    [Test]
    public void CheckSignUp_RefusesAPlayerWhoAlreadySignedUp()
    {
        TournamentRules.CheckSignUp(Tournament(2), "p2", 5_000, Now).Should().Contain("already signed up");
    }

    [Test]
    public void CheckSignUp_ClosesAtTheStartTime()
    {
        TournamentRules.CheckSignUp(Tournament(0), "p1", 5_000, Starts.AddSeconds(-1)).Should().BeEmpty();
        TournamentRules.CheckSignUp(Tournament(0), "p1", 5_000, Starts).Should().Contain("closed");
    }

    [Test]
    public void CheckSignUp_ClosesOnceTheTournamentBegins()
    {
        TournamentRules.CheckSignUp(RunningTournament(2), "p9", 5_000, Now).Should().Contain("not taking sign-ups");
        TournamentRules.CheckSignUp(null, "p9", 5_000, Now).Should().Contain("not taking sign-ups");
    }

    [Test]
    public void CheckSignUp_RefusesABarredPlayer()
    {
        var tournament = Tournament(0);
        TournamentRules.Bar(tournament, "p1");

        TournamentRules.CheckSignUp(tournament, "p1", 5_000, Now).Should().Contain("barred");
    }

    // Beginning and calling

    [Test]
    public void CheckBegin_WaitsForTheStartTime()
    {
        var tournament = Tournament(2);

        TournamentRules.CheckBegin(tournament, Starts.AddMinutes(-1)).Should().Contain("due to start in");
        TournamentRules.CheckBegin(tournament, Starts).Should().BeEmpty();
    }

    [Test]
    public void CheckBegin_NeedsTwoEntrants()
    {
        TournamentRules.CheckBegin(Tournament(1), Starts).Should().Contain("At least 2 entrants");
        TournamentRules.CheckBegin(Tournament(0), Starts).Should().Contain("At least 2 entrants");
    }

    [Test]
    public void CheckBegin_RefusesATournamentThatAlreadyBegan()
    {
        TournamentRules.CheckBegin(RunningTournament(4), Starts).Should().Contain("already begun");
    }

    [Test]
    public void CheckCall_NeedsAReadyMatchAndNoMatchInProgress()
    {
        var tournament = RunningTournament(4);
        TournamentRules.CheckCall(tournament).Should().BeEmpty();

        TournamentRules.Call(TournamentRules.GetNextReady(tournament.Matches), Now);
        TournamentRules.CheckCall(tournament).Should().Contain("already in progress");

        TournamentRules.CheckCall(Tournament(4)).Should().Contain("not running");
    }

    // The bracket

    [TestCase(2, 1, 1, 0)]
    [TestCase(3, 2, 2, 1)]
    [TestCase(4, 2, 2, 0)]
    [TestCase(5, 3, 4, 3)]
    [TestCase(8, 3, 4, 0)]
    [TestCase(9, 4, 8, 7)]
    [TestCase(16, 4, 8, 0)]
    [TestCase(17, 5, 16, 15)]
    public void BuildBracket_HasTheRightShape(int entrants, int rounds, int firstRoundMatches, int byes)
    {
        var ids = Enumerable.Range(1, entrants).Select(x => $"p{x}").ToList();

        var matches = TournamentRules.BuildBracket(ids);

        TournamentRules.GetRoundCount(matches).Should().Be(rounds);
        matches.Count(x => x.Round == 1).Should().Be(firstRoundMatches);
        matches.Count.Should().Be(firstRoundMatches * 2 - 1, "a single-elimination bracket has one match fewer than its first round holds places for");
        matches.Count(x => x.Round == 1 && x.Result == TournamentMatchResult.Bye).Should().Be(byes);
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(5)]
    [TestCase(6)]
    [TestCase(7)]
    [TestCase(9)]
    [TestCase(13)]
    public void BuildBracket_PutsEveryEntrantInTheFirstRoundOnce_AndNeverPairsTwoByes(int entrants)
    {
        var ids = Enumerable.Range(1, entrants).Select(x => $"p{x}").ToList();

        var matches = TournamentRules.BuildBracket(ids);

        var placed = matches.Where(x => x.Round == 1)
            .SelectMany(x => new[] { x.PlayerAId, x.PlayerBId })
            .Where(x => x.Length > 0)
            .ToList();
        placed.Should().BeEquivalentTo(ids);
        matches.Where(x => x.Round == 1).Should().OnlyContain(x => x.PlayerAId.Length > 0, "a bye always sits opposite a duelist");
    }

    [Test]
    public void BuildBracket_WithFewerThanTwoEntrants_BuildsNothing()
    {
        TournamentRules.BuildBracket(new List<string>()).Should().BeEmpty();
        TournamentRules.BuildBracket(new List<string> { "p1" }).Should().BeEmpty();
    }

    [Test]
    public void BuildBracket_MovesAByeWinnerStraightIntoTheNextRound()
    {
        var matches = TournamentRules.BuildBracket(new List<string> { "p1", "p2", "p3" });

        var firstRound = matches.Where(x => x.Round == 1).OrderBy(x => x.Slot).ToList();
        firstRound[0].PlayerAId.Should().Be("p1");
        firstRound[0].PlayerBId.Should().Be("p3");
        firstRound[0].State.Should().Be(TournamentMatchState.Pending);
        firstRound[1].PlayerAId.Should().Be("p2");
        firstRound[1].State.Should().Be(TournamentMatchState.Done);
        firstRound[1].WinnerId.Should().Be("p2");

        var final = TournamentRules.GetFinal(matches);
        final.PlayerAId.Should().BeEmpty("the first match is still to be fought");
        final.PlayerBId.Should().Be("p2");
        TournamentRules.IsReady(final).Should().BeFalse();
    }

    [Test]
    public void BuildBracket_ByesCanMakeALaterRoundMatchReadyAtOnce()
    {
        var matches = TournamentRules.BuildBracket(Enumerable.Range(1, 5).Select(x => $"p{x}").ToList());

        var ready = TournamentRules.GetNextReady(matches);
        ready.Round.Should().Be(1, "the earliest round is called first");

        var secondRoundLowerMatch = TournamentRules.FindMatch(matches, 2, 1);
        TournamentRules.IsReady(secondRoundLowerMatch).Should().BeTrue("two byes feed it");
        secondRoundLowerMatch.PlayerAId.Should().Be("p3");
        secondRoundLowerMatch.PlayerBId.Should().Be("p4");
    }

    [Test]
    public void GetNextReady_TakesTheEarliestRoundThenTheLowestSlot()
    {
        var matches = TournamentRules.BuildBracket(Enumerable.Range(1, 8).Select(x => $"p{x}").ToList());

        TournamentRules.GetNextReady(matches).Slot.Should().Be(0);
        TournamentRules.Call(TournamentRules.GetNextReady(matches), Now);
        TournamentRules.GetNextReady(matches).Slot.Should().Be(1);
    }

    [TestCase(1, 3, "the quarterfinal")]
    [TestCase(2, 3, "the semifinal")]
    [TestCase(3, 3, "the final")]
    [TestCase(1, 2, "the semifinal")]
    [TestCase(2, 2, "the final")]
    [TestCase(1, 1, "the final")]
    public void GetRoundName_CallsTheLastRoundsByName(int round, int rounds, string expected)
    {
        TournamentRules.GetRoundName(round, rounds).Should().Be(expected);
    }

    [Test]
    public void GetRoundName_NumbersTheEarlyRoundsOfALargeBracket()
    {
        TournamentRules.GetRoundName(1, 5).Should().Be("round 1");
        TournamentRules.GetRoundName(2, 5).Should().Be("round 2");
        TournamentRules.GetRoundName(3, 5).Should().Be("the quarterfinal");
    }

    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(7)]
    [TestCase(8)]
    [TestCase(11)]
    [TestCase(16)]
    public void APlayedOutBracket_HasOneFewerMatchThanEntrants_AndAChampion(int entrants)
    {
        var tournament = RunningTournament(entrants);
        var played = 0;

        while (!TournamentRules.IsFinished(tournament))
        {
            var match = TournamentRules.GetNextReady(tournament.Matches);
            match.Should().NotBeNull("an unfinished bracket always has a match ready");
            TournamentRules.Call(match, Now);
            TournamentRules.ApplyDuelOutcome(match, new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Challenger), Now).Should().BeTrue();
            TournamentRules.Refresh(tournament.Matches);
            played++;
        }

        played.Should().Be(entrants - 1);
        TournamentRules.GetChampionId(tournament).Should().NotBeEmpty();
        tournament.Matches.Should().OnlyContain(x => x.State == TournamentMatchState.Done);
    }

    [Test]
    public void TheChampion_IsEmptyWhileTheFinalIsOpen()
    {
        var tournament = RunningTournament(4);

        TournamentRules.IsFinished(tournament).Should().BeFalse();
        TournamentRules.GetChampionId(tournament).Should().BeEmpty();
    }

    // Winners and losers

    [Test]
    public void AVictory_AdvancesTheDuelistWhoWon_AndTheChallengerIsTheFirstDuelist()
    {
        var tournament = RunningTournament(2);
        var match = tournament.Matches.Single();

        TournamentRules.ApplyDuelOutcome(match, new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Target), Now).Should().BeTrue();

        match.WinnerId.Should().Be(match.PlayerBId);
        match.Result.Should().Be(TournamentMatchResult.Victory);
        match.State.Should().Be(TournamentMatchState.Done);
    }

    [Test]
    public void AForfeitInTheDuel_AdvancesTheOtherDuelist_AsAForfeit()
    {
        var match = RunningTournament(2).Matches.Single();

        TournamentRules.ApplyDuelOutcome(match, new DuelOutcome(DuelOutcomeType.Forfeit, DuelSide.Challenger), Now);

        match.WinnerId.Should().Be(match.PlayerAId);
        match.Result.Should().Be(TournamentMatchResult.Forfeit);
    }

    [Test]
    public void ADraw_ReplaysTheMatchWithAFreshFiveMinutes()
    {
        var match = RunningTournament(2).Matches.Single();
        TournamentRules.Call(match, Now.AddMinutes(-30));
        TournamentRules.MarkActive(match);

        var finished = TournamentRules.ApplyDuelOutcome(match, new DuelOutcome(DuelOutcomeType.Draw, DuelSide.None), Now);

        finished.Should().BeFalse();
        match.State.Should().Be(TournamentMatchState.Called);
        match.CalledUtc.Should().Be(Now);
        match.WinnerId.Should().BeEmpty();
        match.Interruptions.Should().Be(0, "a draw is not an interruption");
    }

    // No-shows

    [Test]
    public void NoShow_NothingHappensBeforeFiveMinutes()
    {
        var match = RunningTournament(2).Matches.Single();
        TournamentRules.Call(match, Now);

        TournamentRules.EvaluateNoShow(match, true, false, Now.AddMinutes(4).AddSeconds(59)).Should().Be(NoShowDecision.None);
        TournamentRules.EvaluateNoShow(match, false, false, Now.AddMinutes(4).AddSeconds(59)).Should().Be(NoShowDecision.None);
    }

    [TestCase(true, false, NoShowDecision.PlayerBForfeits)]
    [TestCase(false, true, NoShowDecision.PlayerAForfeits)]
    [TestCase(false, false, NoShowDecision.BothForfeit)]
    [TestCase(true, true, NoShowDecision.None)]
    public void NoShow_ForfeitsTheDuelistWhoIsMissingAfterFiveMinutes(bool firstPresent, bool secondPresent, NoShowDecision expected)
    {
        var match = RunningTournament(2).Matches.Single();
        TournamentRules.Call(match, Now);

        TournamentRules.EvaluateNoShow(match, firstPresent, secondPresent, Now.AddMinutes(5)).Should().Be(expected);
    }

    [Test]
    public void NoShow_OnlyAppliesToACalledMatch()
    {
        var match = RunningTournament(2).Matches.Single();

        TournamentRules.EvaluateNoShow(match, false, false, Now.AddHours(1)).Should().Be(NoShowDecision.None, "an uncalled match has no clock");

        TournamentRules.Call(match, Now);
        TournamentRules.MarkActive(match);
        TournamentRules.EvaluateNoShow(match, false, false, Now.AddHours(1)).Should().Be(NoShowDecision.None, "a duel in progress has its own rules");
    }

    [Test]
    public void ANoShow_GivesTheMatchToThePlayerWhoCame()
    {
        var match = RunningTournament(2).Matches.Single();

        TournamentRules.ApplyNoShow(match, NoShowDecision.PlayerBForfeits);

        match.WinnerId.Should().Be(match.PlayerAId);
        match.Result.Should().Be(TournamentMatchResult.Forfeit);
        match.State.Should().Be(TournamentMatchState.Done);
    }

    [Test]
    public void WhenBothNoShow_NobodyAdvances_AndTheirOpponentInTheNextRoundAdvancesWithoutAFight()
    {
        var tournament = RunningTournament(4);
        var first = TournamentRules.FindMatch(tournament.Matches, 1, 0);
        TournamentRules.Call(first, Now);
        TournamentRules.ApplyNoShow(first, NoShowDecision.BothForfeit);
        TournamentRules.Refresh(tournament.Matches);

        first.WinnerId.Should().BeEmpty();
        first.Result.Should().Be(TournamentMatchResult.BothForfeit);

        var second = TournamentRules.FindMatch(tournament.Matches, 1, 1);
        TournamentRules.Call(second, Now);
        TournamentRules.ApplyDuelOutcome(second, new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Challenger), Now);
        TournamentRules.Refresh(tournament.Matches);

        var final = TournamentRules.GetFinal(tournament.Matches);
        final.State.Should().Be(TournamentMatchState.Done);
        final.Result.Should().Be(TournamentMatchResult.Bye);
        TournamentRules.GetChampionId(tournament).Should().Be(second.PlayerAId);
    }

    [Test]
    public void WhenBothFinalistsNoShow_TheTournamentEndsWithNoChampion()
    {
        var tournament = RunningTournament(2);
        var final = tournament.Matches.Single();
        TournamentRules.Call(final, Now);

        TournamentRules.ApplyNoShow(final, NoShowDecision.BothForfeit);
        TournamentRules.Refresh(tournament.Matches);

        TournamentRules.IsFinished(tournament).Should().BeTrue();
        TournamentRules.GetChampionId(tournament).Should().BeEmpty();
    }

    [Test]
    public void WhenEveryEarlierMatchHadNoWinner_TheNextMatchHasNoContest()
    {
        var tournament = RunningTournament(4);
        foreach (var slot in new[] { 0, 1 })
        {
            var match = TournamentRules.FindMatch(tournament.Matches, 1, slot);
            TournamentRules.Call(match, Now);
            TournamentRules.ApplyNoShow(match, NoShowDecision.BothForfeit);
        }

        TournamentRules.Refresh(tournament.Matches);

        TournamentRules.GetFinal(tournament.Matches).Result.Should().Be(TournamentMatchResult.NoContest);
        TournamentRules.IsFinished(tournament).Should().BeTrue();
        TournamentRules.GetChampionId(tournament).Should().BeEmpty();
    }

    // Interruptions

    [Test]
    public void AnInterruption_ReplaysTheMatchWithAFreshFiveMinutes()
    {
        var match = RunningTournament(2).Matches.Single();
        TournamentRules.Call(match, Now);
        TournamentRules.MarkActive(match);

        var state = TournamentRules.RecordInterruption(match, Now.AddMinutes(3));

        state.Should().Be(TournamentMatchState.Called);
        match.Interruptions.Should().Be(1);
        match.CalledUtc.Should().Be(Now.AddMinutes(3));
    }

    [Test]
    public void TheSecondInterruption_StillReplaysTheMatch()
    {
        var match = RunningTournament(2).Matches.Single();
        for (var i = 0; i < 2; i++)
        {
            TournamentRules.MarkActive(match);
            TournamentRules.RecordInterruption(match, Now);
        }

        match.State.Should().Be(TournamentMatchState.Called);
        match.Interruptions.Should().Be(2);
    }

    [Test]
    public void TheThirdInterruption_HandsTheMatchToAnOfficial()
    {
        var match = RunningTournament(2).Matches.Single();
        TournamentMatchState state = TournamentMatchState.Invalid;
        for (var i = 0; i < TournamentRules.InterruptionsBeforeOfficial; i++)
        {
            TournamentRules.MarkActive(match);
            state = TournamentRules.RecordInterruption(match, Now);
        }

        state.Should().Be(TournamentMatchState.AwaitingOfficial);
        match.Interruptions.Should().Be(3);
    }

    [Test]
    public void AMatchThatIsNotInProgress_IsNotInterrupted()
    {
        var match = RunningTournament(2).Matches.Single();
        TournamentRules.Call(match, Now);

        TournamentRules.RecordInterruption(match, Now.AddMinutes(1)).Should().Be(TournamentMatchState.Called);

        match.Interruptions.Should().Be(0);
        match.CalledUtc.Should().Be(Now);
    }

    [Test]
    public void AMatchWaitingForAnOfficial_IsNotReplayedOrCalledAgain()
    {
        var tournament = RunningTournament(2);
        var match = tournament.Matches.Single();
        for (var i = 0; i < 3; i++)
        {
            TournamentRules.MarkActive(match);
            TournamentRules.RecordInterruption(match, Now);
        }

        TournamentRules.GetCurrentMatch(tournament).Should().BeSameAs(match);
        TournamentRules.CheckCall(tournament).Should().Contain("already in progress");
        TournamentRules.EvaluateNoShow(match, false, false, Now.AddHours(1)).Should().Be(NoShowDecision.None);
    }

    // An official decides

    private static TournamentMatch InterruptedMatch(CityTournament tournament)
    {
        var match = TournamentRules.GetNextReady(tournament.Matches);
        for (var i = 0; i < TournamentRules.InterruptionsBeforeOfficial; i++)
        {
            TournamentRules.MarkActive(match);
            TournamentRules.RecordInterruption(match, Now);
        }

        return match;
    }

    [Test]
    public void AnOfficial_PicksTheWinnerOfAnInterruptedMatch()
    {
        var tournament = RunningTournament(2);
        var match = InterruptedMatch(tournament);

        TournamentRules.CanDecide(match, match.PlayerBId).Should().BeTrue();
        TournamentRules.Decide(match, match.PlayerBId, "official-1").Should().BeTrue();

        match.WinnerId.Should().Be(match.PlayerBId);
        match.Result.Should().Be(TournamentMatchResult.Official);
        match.DecidedByPlayerId.Should().Be("official-1");
        match.State.Should().Be(TournamentMatchState.Done);
    }

    [Test]
    public void AnOfficial_CannotPickSomeoneOutsideTheMatch_OrDecideAMatchThatIsNotWaiting()
    {
        var tournament = RunningTournament(4);
        var waiting = InterruptedMatch(tournament);

        TournamentRules.Decide(waiting, "someone-else", "official-1").Should().BeFalse();
        TournamentRules.Decide(waiting, string.Empty, "official-1").Should().BeFalse();
        waiting.State.Should().Be(TournamentMatchState.AwaitingOfficial);

        var other = TournamentRules.FindMatch(tournament.Matches, 1, 1);
        TournamentRules.Decide(other, other.PlayerAId, "official-1").Should().BeFalse("only an interrupted match goes to an official");
        other.State.Should().Be(TournamentMatchState.Pending);
    }

    [Test]
    public void AnOfficialsDecision_AdvancesTheWinner()
    {
        var tournament = RunningTournament(4);
        var match = InterruptedMatch(tournament);

        TournamentRules.Decide(match, match.PlayerAId, "official-1");
        TournamentRules.Refresh(tournament.Matches);

        TournamentRules.GetFinal(tournament.Matches).PlayerAId.Should().Be(match.PlayerAId);
    }

    // Barring

    [Test]
    public void Bar_AddsAPlayerOnce()
    {
        var tournament = Tournament(2);

        TournamentRules.Bar(tournament, "stranger").Should().BeTrue();
        TournamentRules.Bar(tournament, "stranger").Should().BeFalse();

        tournament.BarredPlayerIds.Should().Equal("stranger");
        TournamentRules.IsBarred(tournament, "stranger").Should().BeTrue();
        TournamentRules.IsBarred(tournament, "p1").Should().BeFalse();
    }

    [Test]
    public void Bar_IgnoresNoOne()
    {
        var tournament = Tournament(2);

        TournamentRules.Bar(tournament, string.Empty).Should().BeFalse();
        TournamentRules.IsBarred(tournament, string.Empty).Should().BeFalse();
        tournament.BarredPlayerIds.Should().BeEmpty();
    }

    [Test]
    public void OnlyAThirdPartyIsBarred_NotEitherDuelist_AndNotACreature()
    {
        var match = RunningTournament(2).Matches.Single();

        TournamentRules.ShouldBar(match, "stranger").Should().BeTrue();
        TournamentRules.ShouldBar(match, match.PlayerAId).Should().BeFalse();
        TournamentRules.ShouldBar(match, match.PlayerBId).Should().BeFalse();
        TournamentRules.ShouldBar(match, string.Empty).Should().BeFalse("a hazard or a non-player has no one to bar");
    }

    // Restart

    [Test]
    public void ARestart_ReplaysTheMatchInProgress_AndRestartsTheClockOnACalledMatch()
    {
        var tournament = RunningTournament(8);
        var active = TournamentRules.FindMatch(tournament.Matches, 1, 0);
        var called = TournamentRules.FindMatch(tournament.Matches, 1, 1);
        TournamentRules.Call(active, Now.AddMinutes(-20));
        TournamentRules.MarkActive(active);
        TournamentRules.Call(called, Now.AddMinutes(-20));

        TournamentRules.ReplayInProgress(tournament, Now).Should().Be(2);

        active.State.Should().Be(TournamentMatchState.Called);
        active.CalledUtc.Should().Be(Now);
        called.State.Should().Be(TournamentMatchState.Called);
        called.CalledUtc.Should().Be(Now);
    }

    [Test]
    public void ARestart_KeepsWhatIsDecided_AndWhatWaitsForAnOfficial()
    {
        var tournament = RunningTournament(4);
        var waiting = InterruptedMatch(tournament);
        var done = TournamentRules.FindMatch(tournament.Matches, 1, 1);
        TournamentRules.ApplyDuelOutcome(done, new DuelOutcome(DuelOutcomeType.Victory, DuelSide.Challenger), Now);

        TournamentRules.ReplayInProgress(tournament, Now.AddHours(1)).Should().Be(0);

        waiting.State.Should().Be(TournamentMatchState.AwaitingOfficial);
        waiting.Interruptions.Should().Be(3, "the interruption count survives a restart");
        done.State.Should().Be(TournamentMatchState.Done);
    }

    // Shuffling

    [Test]
    public void Shuffle_KeepsEveryEntrantExactlyOnce()
    {
        var ids = Enumerable.Range(1, 20).Select(x => $"p{x}").ToList();
        var random = new System.Random(7);

        var shuffled = TournamentRules.Shuffle(ids, random.Next);

        shuffled.Should().BeEquivalentTo(ids);
        shuffled.Should().NotEqual(ids, "a seeded shuffle of twenty moves someone");
    }

    [Test]
    public void Shuffle_UsesTheRandomSourceItIsGiven()
    {
        var ids = new List<string> { "a", "b", "c", "d" };

        TournamentRules.Shuffle(ids, _ => 0).Should().Equal("b", "c", "d", "a");
        TournamentRules.Shuffle(ids, limit => limit - 1).Should().Equal(ids, "swapping each place with itself changes nothing");
    }

    // Signing up and a restart

    [Test]
    public void ASignUp_CountsAsSavedOnlyWhenAnEntrantNamesThePayment()
    {
        var tournament = Tournament(2);

        TournamentRules.FindEntryPaymentId(tournament, "pay2").Should().Be("pay2");
        TournamentRules.FindEntryPaymentId(tournament, "pay9").Should().BeNull("a payment no entrant carries is returned to the player");
        TournamentRules.FindEntryPaymentId(null, "pay2").Should().BeNull("a tournament that is gone says nothing itself; its settlement left receipts for its paid entrants");
        TournamentRules.FindEntryPaymentId(tournament, string.Empty).Should().BeNull();
    }

    [Test]
    public void FormatCredits_GroupsThousands()
    {
        TournamentRules.FormatCredits(1_234_567).Should().Be("1,234,567");
        TournamentRules.FormatCredits(0).Should().Be("0");
    }
}
