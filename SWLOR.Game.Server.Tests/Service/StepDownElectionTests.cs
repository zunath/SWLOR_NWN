using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The snap election a step-down opens, read through the election rules: the interim holder is the incumbent, so every
/// result that leaves things as they were seats them, and the mayor who stepped down is the mayor of record who is left out.
/// </summary>
public class StepDownElectionTests
{
    private const string DepartedMayor = "departed";
    private const string Interim = "interim";
    private const string Challenger = "challenger";
    private const string Other = "other";

    private static ElectionVoter Vote(string candidate, bool isNo = false)
    {
        return new ElectionVoter { VoterPlayerId = Guid.NewGuid().ToString(), CandidatePlayerId = candidate, IsNo = isNo };
    }

    private static List<ElectionVoter> Votes(string candidate, int count, bool isNo = false)
    {
        return Enumerable.Range(0, count).Select(_ => Vote(candidate, isNo)).ToList();
    }

    /// <summary>Whether the result seats someone other than the mayor of record, the way the service decides it.</summary>
    private static bool SeatChangedHands(ElectionOutcome outcome)
    {
        return !string.IsNullOrWhiteSpace(outcome.WinnerPlayerId) && outcome.WinnerPlayerId != DepartedMayor;
    }

    #region The status quo seats the interim holder

    [Test]
    public void NobodyRunning_SeatsTheInterimHolder()
    {
        var outcome = ElectionRules.DecideNoCandidates(ElectionKind.SnapStepDown, Interim);

        outcome.Type.Should().Be(ElectionOutcomeType.NoCandidates);
        outcome.WinnerPlayerId.Should().Be(Interim);
        SeatChangedHands(outcome).Should().BeTrue();
        outcome.HasMandate.Should().BeFalse("nobody won outright, so no platform is enacted");
    }

    [Test]
    public void NoVotes_SeatsTheInterimHolder()
    {
        var outcome = ElectionRules.DecideVoting(ElectionKind.SnapStepDown, false, Interim, new[] { Challenger, Other }, Array.Empty<ElectionVoter>());

        outcome.Type.Should().Be(ElectionOutcomeType.NoVotes);
        outcome.WinnerPlayerId.Should().Be(Interim);
        SeatChangedHands(outcome).Should().BeTrue();
    }

    [Test]
    public void EveryCandidateLeaving_SeatsTheInterimHolder()
    {
        var outcome = ElectionRules.DecideVoting(ElectionKind.SnapStepDown, false, Interim, Array.Empty<string>(), Votes(Challenger, 3));

        outcome.Type.Should().Be(ElectionOutcomeType.NoVotes);
        outcome.WinnerPlayerId.Should().Be(Interim);
    }

    [Test]
    public void ALoneChallengerWhoGetsNo_SeatsTheInterimHolder()
    {
        var votes = Votes(Challenger, 2).Concat(Votes(Challenger, 5, true));
        var outcome = ElectionRules.DecideVoting(ElectionKind.SnapStepDown, true, Interim, new[] { Challenger }, votes);

        outcome.Type.Should().Be(ElectionOutcomeType.ChallengerRejected);
        outcome.WinnerPlayerId.Should().Be(Interim);
        outcome.HasMandate.Should().BeFalse();
    }

    [Test]
    public void AYesAndNoTie_SeatsTheInterimHolder()
    {
        var votes = Votes(Challenger, 3).Concat(Votes(Challenger, 3, true));
        var outcome = ElectionRules.DecideVoting(ElectionKind.SnapStepDown, true, Interim, new[] { Challenger }, votes);

        outcome.Type.Should().Be(ElectionOutcomeType.ConfidenceTie);
        outcome.WinnerPlayerId.Should().Be(Interim);
    }

    [Test]
    public void ATieForTheMostVotes_SeatsTheInterimHolder_EvenWhenTheyWereNotInIt()
    {
        var votes = Votes(Challenger, 4).Concat(Votes(Other, 4));
        var outcome = ElectionRules.DecideVoting(ElectionKind.SnapStepDown, false, Interim, new[] { Challenger, Other }, votes);

        outcome.Type.Should().Be(ElectionOutcomeType.Tie);
        outcome.WinnerPlayerId.Should().Be(Interim);
    }

    #endregion

    #region An outright win

    [Test]
    public void AChallengerWhoWinsOutright_TakesTheSeatAndEnactsTheirPlatform()
    {
        var outcome = ElectionRules.DecideVoting(ElectionKind.SnapStepDown, false, Interim, new[] { Challenger, Other }, Votes(Challenger, 4).Concat(Votes(Other, 2)));

        outcome.Type.Should().Be(ElectionOutcomeType.NewMayor);
        outcome.WinnerPlayerId.Should().Be(Challenger);
        outcome.HasMandate.Should().BeTrue();
        SeatChangedHands(outcome).Should().BeTrue();
    }

    [Test]
    public void TheInterimHolderWhoWinsOutright_IsConfirmedAsMayor()
    {
        var outcome = ElectionRules.DecideVoting(ElectionKind.SnapStepDown, false, Interim, new[] { Interim, Other }, Votes(Interim, 4).Concat(Votes(Other, 2)));

        outcome.Type.Should().Be(ElectionOutcomeType.IncumbentConfirmed);
        outcome.WinnerPlayerId.Should().Be(Interim);
        outcome.HasMandate.Should().BeTrue();
    }

    [Test]
    public void TheInterimHolderWhoLosesAConfidenceVote_StillTakesTheSeat_AndNoSecondSnapElectionOpens()
    {
        var votes = Votes(Interim, 1).Concat(Votes(Interim, 4, true));
        var outcome = ElectionRules.DecideVoting(ElectionKind.SnapStepDown, true, Interim, new[] { Interim }, votes);

        outcome.Type.Should().Be(ElectionOutcomeType.NoConfidence);
        outcome.WinnerPlayerId.Should().Be(Interim);
        outcome.SetsNoConfidence.Should().BeTrue("Edicts stay paused if confidence was lost");
        outcome.OpensSnapElection.Should().BeFalse("a snap election never opens another");
    }

    #endregion

    #region What the end of the election changes

    [TestCase(ElectionOutcomeType.NoCandidates, true)]
    [TestCase(ElectionOutcomeType.NoVotes, true)]
    [TestCase(ElectionOutcomeType.ChallengerRejected, true)]
    [TestCase(ElectionOutcomeType.ConfidenceTie, true)]
    [TestCase(ElectionOutcomeType.Tie, true)]
    [TestCase(ElectionOutcomeType.NewMayor, false)]
    [TestCase(ElectionOutcomeType.IncumbentConfirmed, false)]
    [TestCase(ElectionOutcomeType.NoConfidence, false)]
    public void TheStatusQuoResultsAreTheOnesThatLeaveTheIncumbentInTheSeat(ElectionOutcomeType type, bool expected)
    {
        ElectionRules.IsStatusQuo(type).Should().Be(expected);
    }

    [Test]
    public void EveryResultOfTheSnapElectionEndsTheActingMayorsTermWhenItSeatsSomeone()
    {
        // The interim holder or a winner takes a seat the departed mayor held, so the term ends in every result that seats a mayor.
        var results = new[]
        {
            ElectionRules.DecideNoCandidates(ElectionKind.SnapStepDown, Interim),
            ElectionRules.DecideVoting(ElectionKind.SnapStepDown, false, Interim, new[] { Challenger }, Votes(Challenger, 1)),
            ElectionRules.DecideVoting(ElectionKind.SnapStepDown, false, Interim, new[] { Challenger, Other }, Array.Empty<ElectionVoter>())
        };

        foreach (var outcome in results)
        {
            ActingMayorRules.ElectionEndsTerm(outcome.HasMandate, SeatChangedHands(outcome)).Should().BeTrue(outcome.Type.ToString());
        }
    }

    [Test]
    public void ARegularElectionThatEndsWithNobodyChosenLeavesAnActingMayorInPlace()
    {
        var tie = ElectionRules.DecideVoting(ElectionKind.Regular, false, "mayor", new[] { Challenger, Other }, Votes(Challenger, 2).Concat(Votes(Other, 2)));

        tie.WinnerPlayerId.Should().Be("mayor");
        ActingMayorRules.ElectionEndsTerm(tie.HasMandate, tie.WinnerPlayerId != "mayor").Should().BeFalse();
    }

    [Test]
    public void ARegularElectionThatConfirmsTheMayorEndsTheTerm()
    {
        var confirmed = ElectionRules.DecideVoting(ElectionKind.Regular, true, "mayor", new[] { "mayor" }, Votes("mayor", 3));

        confirmed.Type.Should().Be(ElectionOutcomeType.IncumbentConfirmed);
        ActingMayorRules.ElectionEndsTerm(confirmed.HasMandate, false).Should().BeTrue("an election seated the mayor");
    }

    #endregion

    #region Notices

    [TestCase(ElectionOutcomeType.NoCandidates)]
    [TestCase(ElectionOutcomeType.NoVotes)]
    [TestCase(ElectionOutcomeType.ChallengerRejected)]
    [TestCase(ElectionOutcomeType.ConfidenceTie)]
    [TestCase(ElectionOutcomeType.Tie)]
    public void TheBroadcastSaysTheInterimHolderIsNowMayor_WhenNobodyWonOutright(ElectionOutcomeType type)
    {
        var summary = ElectionNoticeText.BuildStepDownSummary(type);

        summary.Should().Contain("interim holder is now mayor").And.Contain("without a winner");
        summary.Should().MatchRegex("^[\x20-\x7E]+$");
    }

    [Test]
    public void TheBroadcastForAnInterimHolderWhoWinsOrLosesAConfidenceVoteSaysSo()
    {
        ElectionNoticeText.BuildStepDownSummary(ElectionOutcomeType.IncumbentConfirmed).Should().Contain("interim holder won");
        ElectionNoticeText.BuildStepDownSummary(ElectionOutcomeType.NoConfidence).Should().Contain("No on the interim holder").And.Contain("Edicts paused");
    }

    [Test]
    public void ANewMayorFromTheSnapElectionReadsLikeAnyOtherNewMayor()
    {
        ElectionNoticeText.BuildStepDownSummary(ElectionOutcomeType.NewMayor).Should().BeEmpty();
    }

    [Test]
    public void TheResultNoticeSaysTheInterimHolderBecomesMayor_NotThatTheyStay()
    {
        var result = new ElectionResultRecord
        {
            Id = "e",
            Kind = ElectionKind.SnapStepDown,
            Outcome = ElectionOutcomeType.NoCandidates,
            WinnerPlayerId = Interim,
            PostedUtc = DateTime.UtcNow
        };

        var notice = ElectionNoticeText.BuildResultNotice(result, "Aria");

        notice.Should().Contain("Aria becomes mayor").And.NotContain("stays mayor");
    }

    [Test]
    public void TheResultNoticeForAnyOtherElectionIsUnchanged()
    {
        var result = new ElectionResultRecord
        {
            Id = "e",
            Kind = ElectionKind.SnapConfidence,
            Outcome = ElectionOutcomeType.NoCandidates,
            WinnerPlayerId = "mayor",
            PostedUtc = DateTime.UtcNow
        };

        ElectionNoticeText.BuildResultNotice(result, "Aria").Should().Contain("Aria stays mayor");
    }

    [Test]
    public void AnInterimHolderWhoWinsOutrightIsNowMayor()
    {
        var result = new ElectionResultRecord
        {
            Id = "e",
            Kind = ElectionKind.SnapStepDown,
            Outcome = ElectionOutcomeType.IncumbentConfirmed,
            WinnerPlayerId = Interim,
            PostedUtc = DateTime.UtcNow
        };

        ElectionNoticeText.BuildResultNotice(result, "Aria").Should().Contain("Aria won and is now mayor");
    }

    #endregion
}
