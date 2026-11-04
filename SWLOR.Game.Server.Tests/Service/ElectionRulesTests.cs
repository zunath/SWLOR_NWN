using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Every row of the Player City Revamp's election outcome tables, in both the regular or recall column and the snap
/// column, plus the calendar, who may vote, and the candidate statement.
/// </summary>
public class ElectionRulesTests
{
    private const string Incumbent = "incumbent";
    private const string Challenger = "challenger";
    private const string Other = "other";
    private const string Interim = "interim";

    private static readonly ElectionKind[] AllKinds =
    {
        ElectionKind.Regular, ElectionKind.Recall, ElectionKind.SnapConfidence, ElectionKind.SnapStepDown
    };

    private static ElectionVoter Vote(string candidate, bool isNo = false)
    {
        return new ElectionVoter { VoterPlayerId = Guid.NewGuid().ToString(), CandidatePlayerId = candidate, IsNo = isNo };
    }

    private static List<ElectionVoter> Votes(string candidate, int count, bool isNo = false)
    {
        return Enumerable.Range(0, count).Select(_ => Vote(candidate, isNo)).ToList();
    }

    private static ElectionOutcome Confidence(ElectionKind kind, string candidate, int yes, int no, string incumbent = Incumbent)
    {
        var votes = Votes(candidate, yes).Concat(Votes(candidate, no, true));
        return ElectionRules.DecideVoting(kind, true, incumbent, new[] { candidate }, votes);
    }

    private static ElectionOutcome Contested(ElectionKind kind, string[] candidates, IEnumerable<ElectionVoter> votes, string incumbent = Incumbent)
    {
        return ElectionRules.DecideVoting(kind, false, incumbent, candidates, votes);
    }

    #region Calendar

    [Test]
    public void Calendar_IsSevenDaysOfRegistrationSevenOfVotingAndFourteenQuiet()
    {
        ElectionRules.RegistrationDays.Should().Be(7);
        ElectionRules.VotingDays.Should().Be(7);
        ElectionRules.QuietDays.Should().Be(14);
        SWLOR.Game.Server.Service.Property.ElectionRegistrationDays.Should().Be(ElectionRules.RegistrationDays);
        SWLOR.Game.Server.Service.Property.ElectionVotingDays.Should().Be(ElectionRules.VotingDays);
    }

    [Test]
    public void Calendar_AContestedCycleIsTwentyEightDays()
    {
        var opened = new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);
        var registrationCloses = ElectionRules.GetRegistrationCloseUtc(opened);
        var votingCloses = ElectionRules.GetVotingCloseUtc(registrationCloses);
        var ended = ElectionRules.GetEndUtc(registrationCloses, votingCloses, voteWasHeld: true);
        var next = ElectionRules.GetNextRegularElectionUtc(ended);

        registrationCloses.Should().Be(opened.AddDays(7));
        votingCloses.Should().Be(opened.AddDays(14));
        ended.Should().Be(votingCloses);
        next.Should().Be(opened.AddDays(28));
    }

    [Test]
    public void Calendar_AnElectionWithNoCandidatesEndsWhenRegistrationCloses()
    {
        var opened = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
        var registrationCloses = ElectionRules.GetRegistrationCloseUtc(opened);
        var votingCloses = ElectionRules.GetVotingCloseUtc(registrationCloses);

        var ended = ElectionRules.GetEndUtc(registrationCloses, votingCloses, voteWasHeld: false);

        ended.Should().Be(registrationCloses);
        ElectionRules.GetNextRegularElectionUtc(ended).Should().Be(opened.AddDays(21));
    }

    [Test]
    public void Calendar_ASnapElectionTakesTheRegularElectionsPlaceAndTheQuietDaysStartWhenItEnds()
    {
        var regularOpened = new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc);
        var regularEnded = ElectionRules.GetVotingCloseUtc(ElectionRules.GetRegistrationCloseUtc(regularOpened));

        // A failed confidence vote opens the snap election at once, so the regular schedule is not used.
        var snapOpened = regularEnded;
        var snapRegistrationCloses = ElectionRules.GetRegistrationCloseUtc(snapOpened);
        var snapEnded = ElectionRules.GetEndUtc(snapRegistrationCloses, ElectionRules.GetVotingCloseUtc(snapRegistrationCloses), true);

        ElectionRules.GetNextRegularElectionUtc(snapEnded).Should().Be(snapOpened.AddDays(28));
    }

    [Test]
    public void Stages_CloseOnTheDatesStoredOnTheElectionNotOnConstants()
    {
        // An election that opened under the old calendar: 14 days of registration, 7 of voting.
        var opened = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var registrationCloses = opened.AddDays(14);
        var votingCloses = opened.AddDays(21);

        ElectionRules.GetDueAction(ElectionStageType.Registration, opened.AddDays(8), registrationCloses, votingCloses)
            .Should().Be(ElectionStageAction.None, "the new 7-day registration must not close an election that opened for 14");
        ElectionRules.GetDueAction(ElectionStageType.Registration, registrationCloses, registrationCloses, votingCloses)
            .Should().Be(ElectionStageAction.CloseRegistration);
        ElectionRules.GetDueAction(ElectionStageType.Voting, opened.AddDays(20), registrationCloses, votingCloses)
            .Should().Be(ElectionStageAction.None);
        ElectionRules.GetDueAction(ElectionStageType.Voting, votingCloses, registrationCloses, votingCloses)
            .Should().Be(ElectionStageAction.CloseVoting);
    }

    [Test]
    public void Stages_AServerKeptDownPastBothDeadlinesClosesRegistrationThenVoting()
    {
        var opened = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var registrationCloses = ElectionRules.GetRegistrationCloseUtc(opened);
        var votingCloses = ElectionRules.GetVotingCloseUtc(registrationCloses);
        var now = votingCloses.AddDays(3);

        ElectionRules.GetDueAction(ElectionStageType.Registration, now, registrationCloses, votingCloses)
            .Should().Be(ElectionStageAction.CloseRegistration);
        ElectionRules.GetDueAction(ElectionStageType.Voting, now, registrationCloses, votingCloses)
            .Should().Be(ElectionStageAction.CloseVoting);
    }

    [TestCase(0, ElectionRegistrationResult.NoCandidates)]
    [TestCase(1, ElectionRegistrationResult.ConfidenceVote)]
    [TestCase(2, ElectionRegistrationResult.ContestedVote)]
    [TestCase(5, ElectionRegistrationResult.ContestedVote)]
    public void RegistrationClose_DependsOnTheNumberOfCandidates(int candidates, ElectionRegistrationResult expected)
    {
        ElectionRules.GetRegistrationResult(candidates).Should().Be(expected);
    }

    [Test]
    public void Changes_AreAllowedOnlyWhileTheStageIsOpen()
    {
        var opened = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var registrationCloses = opened.AddDays(7);
        var votingCloses = opened.AddDays(14);

        ElectionRules.IsRegistrationOpen(ElectionStageType.Registration, opened.AddDays(6), registrationCloses).Should().BeTrue();
        ElectionRules.IsRegistrationOpen(ElectionStageType.Registration, registrationCloses, registrationCloses).Should().BeFalse();
        ElectionRules.IsRegistrationOpen(ElectionStageType.Voting, opened, registrationCloses).Should().BeFalse();

        ElectionRules.IsVotingOpen(ElectionStageType.Voting, opened.AddDays(13), votingCloses).Should().BeTrue();
        ElectionRules.IsVotingOpen(ElectionStageType.Voting, votingCloses, votingCloses).Should().BeFalse();
        ElectionRules.IsVotingOpen(ElectionStageType.Registration, opened, votingCloses).Should().BeFalse();
    }

    [Test]
    public void DaysLeft_RoundUpAndNeverGoBelowZero()
    {
        var now = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

        ElectionRules.GetDaysLeft(now, now.AddDays(5)).Should().Be(5);
        ElectionRules.GetDaysLeft(now, now.AddDays(4).AddHours(1)).Should().Be(5);
        ElectionRules.GetDaysLeft(now, now.AddMinutes(5)).Should().Be(1);
        ElectionRules.GetDaysLeft(now, now).Should().Be(0);
        ElectionRules.GetDaysLeft(now, now.AddDays(-2)).Should().Be(0);
    }

    #endregion

    #region Registration closes: no candidates

    [Test]
    public void NoCandidates_NoVoteIsHeldAndTheIncumbentStays()
    {
        foreach (var kind in AllKinds)
        {
            var outcome = ElectionRules.DecideNoCandidates(kind, Incumbent);

            outcome.Type.Should().Be(ElectionOutcomeType.NoCandidates, kind.ToString());
            outcome.WinnerPlayerId.Should().Be(Incumbent, kind.ToString());
            outcome.HasMandate.Should().BeFalse(kind.ToString());
            outcome.SetsNoConfidence.Should().BeFalse(kind.ToString());
            outcome.OpensSnapElection.Should().BeFalse(kind.ToString());
            outcome.ClearsNeglect.Should().BeFalse(kind.ToString());
            outcome.VoteWasHeld.Should().BeFalse(kind.ToString());
            outcome.IsEmpty.Should().BeTrue(kind.ToString());
        }
    }

    [Test]
    public void NoCandidates_InAStepDownElectionTheInterimHolderBecomesMayor()
    {
        var outcome = ElectionRules.DecideNoCandidates(ElectionKind.SnapStepDown, Interim);

        outcome.WinnerPlayerId.Should().Be(Interim);
    }

    [Test]
    public void NoCandidates_OnlyARecallCountsAsEmpty()
    {
        ElectionRules.DecideNoCandidates(ElectionKind.Recall, Incumbent).IsEmptyRecall.Should().BeTrue();
        ElectionRules.DecideNoCandidates(ElectionKind.Regular, Incumbent).IsEmptyRecall.Should().BeFalse();
        ElectionRules.DecideNoCandidates(ElectionKind.SnapConfidence, Incumbent).IsEmptyRecall.Should().BeFalse();
        ElectionRules.DecideNoCandidates(ElectionKind.SnapStepDown, Incumbent).IsEmptyRecall.Should().BeFalse();
    }

    #endregion

    #region Voting closes: confidence vote on a challenger

    [Test]
    public void ConfidenceOnChallenger_YesWinsIsAnOutrightWin()
    {
        foreach (var kind in AllKinds)
        {
            var outcome = Confidence(kind, Challenger, yes: 3, no: 2);

            outcome.Type.Should().Be(ElectionOutcomeType.NewMayor, kind.ToString());
            outcome.WinnerPlayerId.Should().Be(Challenger, kind.ToString());
            outcome.HasMandate.Should().BeTrue(kind.ToString());
            outcome.SetsNoConfidence.Should().BeFalse(kind.ToString());
            outcome.OpensSnapElection.Should().BeFalse(kind.ToString());
            outcome.ClearsNeglect.Should().BeTrue(kind.ToString());
            outcome.VotesCast.Should().Be(5, kind.ToString());
        }
    }

    [Test]
    public void ConfidenceOnChallenger_NoWinsAndTheIncumbentStaysWithoutAMandate()
    {
        foreach (var kind in AllKinds)
        {
            var outcome = Confidence(kind, Challenger, yes: 1, no: 4);

            outcome.Type.Should().Be(ElectionOutcomeType.ChallengerRejected, kind.ToString());
            outcome.WinnerPlayerId.Should().Be(Incumbent, kind.ToString());
            outcome.HasMandate.Should().BeFalse(kind.ToString());
            outcome.SetsNoConfidence.Should().BeFalse(kind.ToString());
            outcome.OpensSnapElection.Should().BeFalse(kind.ToString());
            outcome.ClearsNeglect.Should().BeTrue(kind.ToString());
        }
    }

    [Test]
    public void ConfidenceOnChallenger_ATieLeavesTheIncumbentInPlace()
    {
        foreach (var kind in AllKinds)
        {
            var outcome = Confidence(kind, Challenger, yes: 2, no: 2);

            outcome.Type.Should().Be(ElectionOutcomeType.ConfidenceTie, kind.ToString());
            outcome.WinnerPlayerId.Should().Be(Incumbent, kind.ToString());
            outcome.HasMandate.Should().BeFalse(kind.ToString());
            outcome.OpensSnapElection.Should().BeFalse(kind.ToString());
            outcome.ClearsNeglect.Should().BeTrue(kind.ToString());
        }
    }

    [Test]
    public void ConfidenceOnChallenger_NoVotesLeavesTheIncumbentInPlace()
    {
        foreach (var kind in AllKinds)
        {
            var outcome = Confidence(kind, Challenger, yes: 0, no: 0);

            outcome.Type.Should().Be(ElectionOutcomeType.NoVotes, kind.ToString());
            outcome.WinnerPlayerId.Should().Be(Incumbent, kind.ToString());
            outcome.HasMandate.Should().BeFalse(kind.ToString());
            outcome.ClearsNeglect.Should().BeFalse(kind.ToString());
            outcome.IsEmpty.Should().BeTrue(kind.ToString());
            outcome.VoteWasHeld.Should().BeTrue(kind.ToString());
        }
    }

    #endregion

    #region Voting closes: confidence vote on the incumbent

    [Test]
    public void ConfidenceOnIncumbent_YesConfirmsThemWithAMandate()
    {
        foreach (var kind in AllKinds)
        {
            var outcome = Confidence(kind, Incumbent, yes: 4, no: 1);

            outcome.Type.Should().Be(ElectionOutcomeType.IncumbentConfirmed, kind.ToString());
            outcome.WinnerPlayerId.Should().Be(Incumbent, kind.ToString());
            outcome.HasMandate.Should().BeTrue(kind.ToString());
            outcome.SetsNoConfidence.Should().BeFalse(kind.ToString());
            outcome.OpensSnapElection.Should().BeFalse(kind.ToString());
            outcome.ClearsNeglect.Should().BeTrue(kind.ToString());
        }
    }

    [TestCase(ElectionKind.Regular, true)]
    [TestCase(ElectionKind.Recall, true)]
    [TestCase(ElectionKind.SnapConfidence, false)]
    [TestCase(ElectionKind.SnapStepDown, false)]
    public void ConfidenceOnIncumbent_NoMeansNoConfidenceAndOnlyARegularOrRecallElectionOpensASnap(ElectionKind kind, bool opensSnap)
    {
        var outcome = Confidence(kind, Incumbent, yes: 1, no: 3);

        outcome.Type.Should().Be(ElectionOutcomeType.NoConfidence);
        outcome.WinnerPlayerId.Should().Be(Incumbent);
        outcome.HasMandate.Should().BeFalse();
        outcome.SetsNoConfidence.Should().BeTrue();
        outcome.OpensSnapElection.Should().Be(opensSnap);
        outcome.ClearsNeglect.Should().BeTrue();
    }

    [Test]
    public void ConfidenceOnIncumbent_YesAndNoTiedKeepsThemWithoutNoConfidence()
    {
        foreach (var kind in AllKinds)
        {
            var outcome = Confidence(kind, Incumbent, yes: 2, no: 2);

            outcome.Type.Should().Be(ElectionOutcomeType.ConfidenceTie, kind.ToString());
            outcome.WinnerPlayerId.Should().Be(Incumbent, kind.ToString());
            outcome.HasMandate.Should().BeFalse(kind.ToString());
            outcome.SetsNoConfidence.Should().BeFalse(kind.ToString());
            outcome.OpensSnapElection.Should().BeFalse(kind.ToString());
        }
    }

    [Test]
    public void ConfidenceOnIncumbent_NoVotesKeepsThemWithoutNoConfidence()
    {
        foreach (var kind in AllKinds)
        {
            var outcome = Confidence(kind, Incumbent, yes: 0, no: 0);

            outcome.Type.Should().Be(ElectionOutcomeType.NoVotes, kind.ToString());
            outcome.SetsNoConfidence.Should().BeFalse(kind.ToString());
            outcome.OpensSnapElection.Should().BeFalse(kind.ToString());
            outcome.ClearsNeglect.Should().BeFalse(kind.ToString());
        }
    }

    [Test]
    public void ConfidenceVote_OnlyCountsVotesForTheLoneCandidate()
    {
        var votes = Votes(Challenger, 1).Concat(Votes(Other, 5, true));

        var outcome = ElectionRules.DecideVoting(ElectionKind.Regular, true, Incumbent, new[] { Challenger }, votes);

        outcome.Type.Should().Be(ElectionOutcomeType.NewMayor);
        outcome.VotesCast.Should().Be(1);
    }

    [Test]
    public void SnapStepDown_TheInterimHolderStandsInForTheIncumbent()
    {
        var yes = Confidence(ElectionKind.SnapStepDown, Interim, yes: 3, no: 1, incumbent: Interim);
        yes.Type.Should().Be(ElectionOutcomeType.IncumbentConfirmed);
        yes.WinnerPlayerId.Should().Be(Interim);

        var rejected = Confidence(ElectionKind.SnapStepDown, Challenger, yes: 0, no: 2, incumbent: Interim);
        rejected.Type.Should().Be(ElectionOutcomeType.ChallengerRejected);
        rejected.WinnerPlayerId.Should().Be(Interim);

        var tie = Contested(ElectionKind.SnapStepDown, new[] { Challenger, Other }, Votes(Challenger, 1).Concat(Votes(Other, 1)), Interim);
        tie.Type.Should().Be(ElectionOutcomeType.Tie);
        tie.WinnerPlayerId.Should().Be(Interim);
    }

    #endregion

    #region Voting closes: contested vote

    [Test]
    public void Contested_OneCandidateWithTheMostVotesWinsOutright()
    {
        foreach (var kind in AllKinds)
        {
            var votes = Votes(Challenger, 3).Concat(Votes(Other, 2)).Concat(Votes(Incumbent, 1));
            var outcome = Contested(kind, new[] { Incumbent, Challenger, Other }, votes);

            outcome.Type.Should().Be(ElectionOutcomeType.NewMayor, kind.ToString());
            outcome.WinnerPlayerId.Should().Be(Challenger, kind.ToString());
            outcome.HasMandate.Should().BeTrue(kind.ToString());
            outcome.OpensSnapElection.Should().BeFalse(kind.ToString());
            outcome.ClearsNeglect.Should().BeTrue(kind.ToString());
            outcome.CandidateCount.Should().Be(3, kind.ToString());
            outcome.VotesCast.Should().Be(6, kind.ToString());
        }
    }

    [Test]
    public void Contested_TheIncumbentWinningOutrightIsConfirmedWithAMandate()
    {
        var outcome = Contested(ElectionKind.Regular, new[] { Incumbent, Challenger }, Votes(Incumbent, 3).Concat(Votes(Challenger, 1)));

        outcome.Type.Should().Be(ElectionOutcomeType.IncumbentConfirmed);
        outcome.WinnerPlayerId.Should().Be(Incumbent);
        outcome.HasMandate.Should().BeTrue();
    }

    [Test]
    public void Contested_ATieForMostVotesLeavesTheIncumbentInPlace()
    {
        foreach (var kind in AllKinds)
        {
            var votes = Votes(Challenger, 2).Concat(Votes(Other, 2)).Concat(Votes(Incumbent, 1));
            var outcome = Contested(kind, new[] { Incumbent, Challenger, Other }, votes);

            outcome.Type.Should().Be(ElectionOutcomeType.Tie, kind.ToString());
            outcome.WinnerPlayerId.Should().Be(Incumbent, kind.ToString());
            outcome.HasMandate.Should().BeFalse(kind.ToString());
            outcome.SetsNoConfidence.Should().BeFalse(kind.ToString());
            outcome.OpensSnapElection.Should().BeFalse(kind.ToString());
            outcome.ClearsNeglect.Should().BeTrue(kind.ToString());
        }
    }

    [Test]
    public void Contested_ATieThatIncludesTheIncumbentLeavesThemInPlaceWithoutAMandate()
    {
        var outcome = Contested(ElectionKind.Regular, new[] { Incumbent, Challenger }, Votes(Incumbent, 2).Concat(Votes(Challenger, 2)));

        outcome.Type.Should().Be(ElectionOutcomeType.Tie);
        outcome.WinnerPlayerId.Should().Be(Incumbent);
        outcome.HasMandate.Should().BeFalse();
    }

    [Test]
    public void Contested_AThreeWayTieIsATie()
    {
        var votes = Votes(Incumbent, 1).Concat(Votes(Challenger, 1)).Concat(Votes(Other, 1));

        Contested(ElectionKind.Recall, new[] { Incumbent, Challenger, Other }, votes).Type.Should().Be(ElectionOutcomeType.Tie);
    }

    [Test]
    public void Contested_NoVotesLeavesTheIncumbentInPlace()
    {
        foreach (var kind in AllKinds)
        {
            var outcome = Contested(kind, new[] { Challenger, Other }, Array.Empty<ElectionVoter>());

            outcome.Type.Should().Be(ElectionOutcomeType.NoVotes, kind.ToString());
            outcome.WinnerPlayerId.Should().Be(Incumbent, kind.ToString());
            outcome.HasMandate.Should().BeFalse(kind.ToString());
            outcome.ClearsNeglect.Should().BeFalse(kind.ToString());
            outcome.IsEmpty.Should().BeTrue(kind.ToString());
            outcome.CandidateCount.Should().Be(2, kind.ToString());
        }
    }

    [Test]
    public void Contested_VotesForPeopleNotOnTheBallotAreIgnored()
    {
        var outcome = Contested(ElectionKind.Regular, new[] { Challenger, Other }, Votes("gone", 9).Concat(Votes(Other, 1)));

        outcome.Type.Should().Be(ElectionOutcomeType.NewMayor);
        outcome.WinnerPlayerId.Should().Be(Other);
        outcome.VotesCast.Should().Be(1);
    }

    [Test]
    public void Contested_NoFlagsAreIgnoredBecauseThereAreNoNoVotesInAContestedVote()
    {
        var outcome = Contested(ElectionKind.Regular, new[] { Challenger, Other }, Votes(Challenger, 2, isNo: true).Concat(Votes(Other, 1)));

        outcome.Type.Should().Be(ElectionOutcomeType.NewMayor);
        outcome.WinnerPlayerId.Should().Be(Challenger);
    }

    [Test]
    public void Contested_WhenOnlyOneCandidateIsLeftTheirVotesWinOutright()
    {
        var outcome = Contested(ElectionKind.Regular, new[] { Challenger }, Votes(Challenger, 2));

        outcome.Type.Should().Be(ElectionOutcomeType.NewMayor);
        outcome.WinnerPlayerId.Should().Be(Challenger);
    }

    #endregion

    #region Every candidate gone

    [Test]
    public void EveryCandidateGone_TheElectionEndsAsIfNobodyVoted()
    {
        foreach (var kind in AllKinds)
        {
            foreach (var isConfidence in new[] { true, false })
            {
                var outcome = ElectionRules.DecideVoting(kind, isConfidence, Incumbent, Array.Empty<string>(), Array.Empty<ElectionVoter>());

                var label = $"{kind} confidence={isConfidence}";
                outcome.Type.Should().Be(ElectionOutcomeType.NoVotes, label);
                outcome.WinnerPlayerId.Should().Be(Incumbent, label);
                outcome.HasMandate.Should().BeFalse(label);
                outcome.ClearsNeglect.Should().BeFalse(label);
                outcome.IsEmpty.Should().BeTrue(label);
                outcome.CandidateCount.Should().Be(0, label);
            }
        }
    }

    [Test]
    public void EveryCandidateGone_VotesLeftBehindDoNotCount()
    {
        var outcome = ElectionRules.DecideVoting(ElectionKind.Regular, true, Incumbent, Array.Empty<string>(), Votes(Challenger, 4));

        outcome.Type.Should().Be(ElectionOutcomeType.NoVotes);
        outcome.VotesCast.Should().Be(0);
    }

    [Test]
    public void Recall_IsEmptyWithNoCandidatesNoVotesOrEveryCandidateGone()
    {
        ElectionRules.DecideNoCandidates(ElectionKind.Recall, Incumbent).IsEmptyRecall.Should().BeTrue();
        Contested(ElectionKind.Recall, new[] { Challenger, Other }, Array.Empty<ElectionVoter>()).IsEmptyRecall.Should().BeTrue();
        Confidence(ElectionKind.Recall, Challenger, 0, 0).IsEmptyRecall.Should().BeTrue();
        ElectionRules.DecideVoting(ElectionKind.Recall, false, Incumbent, Array.Empty<string>(), Array.Empty<ElectionVoter>())
            .IsEmptyRecall.Should().BeTrue();
    }

    [Test]
    public void Recall_WithACandidateAndAVoteIsNeverEmpty()
    {
        Confidence(ElectionKind.Recall, Challenger, 1, 0).IsEmptyRecall.Should().BeFalse();
        Confidence(ElectionKind.Recall, Challenger, 0, 1).IsEmptyRecall.Should().BeFalse();
        Confidence(ElectionKind.Recall, Challenger, 1, 1).IsEmptyRecall.Should().BeFalse();
        Contested(ElectionKind.Recall, new[] { Challenger, Other }, Votes(Challenger, 1).Concat(Votes(Other, 1))).IsEmptyRecall.Should().BeFalse();
    }

    [Test]
    public void AnEmptyElectionOfAnotherKindIsNotAnEmptyRecall()
    {
        Contested(ElectionKind.Regular, new[] { Challenger, Other }, Array.Empty<ElectionVoter>()).IsEmptyRecall.Should().BeFalse();
        Contested(ElectionKind.SnapConfidence, new[] { Challenger, Other }, Array.Empty<ElectionVoter>()).IsEmptyRecall.Should().BeFalse();
    }

    #endregion

    #region Conditions

    [Test]
    public void OnlyAnOutrightWinHasAMandate()
    {
        var results = new[]
        {
            Confidence(ElectionKind.Regular, Challenger, 2, 1),
            Confidence(ElectionKind.Regular, Incumbent, 2, 1),
            Contested(ElectionKind.Regular, new[] { Challenger, Other }, Votes(Challenger, 2).Concat(Votes(Other, 1))),
        };
        var noMandate = new[]
        {
            ElectionRules.DecideNoCandidates(ElectionKind.Regular, Incumbent),
            Confidence(ElectionKind.Regular, Challenger, 1, 2),
            Confidence(ElectionKind.Regular, Incumbent, 1, 2),
            Confidence(ElectionKind.Regular, Challenger, 1, 1),
            Confidence(ElectionKind.Regular, Challenger, 0, 0),
            Contested(ElectionKind.Regular, new[] { Challenger, Other }, Votes(Challenger, 1).Concat(Votes(Other, 1))),
            Contested(ElectionKind.Regular, new[] { Challenger, Other }, Array.Empty<ElectionVoter>()),
        };

        results.Should().OnlyContain(x => x.HasMandate);
        noMandate.Should().OnlyContain(x => !x.HasMandate);
    }

    [Test]
    public void AnyElectionWithACandidateAndAVoteEndsNeglectWhateverItsResult()
    {
        var participated = new[]
        {
            Confidence(ElectionKind.Regular, Challenger, 1, 0),
            Confidence(ElectionKind.Regular, Challenger, 0, 1),
            Confidence(ElectionKind.Regular, Incumbent, 0, 1),
            Confidence(ElectionKind.Regular, Challenger, 1, 1),
            Contested(ElectionKind.Recall, new[] { Challenger, Other }, Votes(Challenger, 1).Concat(Votes(Other, 1))),
            Contested(ElectionKind.SnapConfidence, new[] { Challenger, Other }, Votes(Challenger, 4)),
        };
        var empty = new[]
        {
            ElectionRules.DecideNoCandidates(ElectionKind.Regular, Incumbent),
            Confidence(ElectionKind.Regular, Challenger, 0, 0),
            Contested(ElectionKind.Regular, new[] { Challenger, Other }, Array.Empty<ElectionVoter>()),
            ElectionRules.DecideVoting(ElectionKind.Regular, false, Incumbent, Array.Empty<string>(), Votes(Challenger, 3)),
        };

        participated.Should().OnlyContain(x => x.ClearsNeglect);
        empty.Should().OnlyContain(x => !x.ClearsNeglect);
    }

    [Test]
    public void ASnapElectionNeverOpensAnother_AndARegularOrRecallElectionOpensOneOnlyAfterNoConfidence()
    {
        foreach (var kind in AllKinds)
        {
            var opened = new[]
            {
                ElectionRules.DecideNoCandidates(kind, Incumbent),
                Confidence(kind, Challenger, 2, 1),
                Confidence(kind, Challenger, 1, 2),
                Confidence(kind, Incumbent, 2, 1),
                Confidence(kind, Incumbent, 1, 1),
                Contested(kind, new[] { Challenger, Other }, Votes(Challenger, 1)),
                Contested(kind, new[] { Challenger, Other }, Array.Empty<ElectionVoter>()),
            };
            opened.Should().OnlyContain(x => !x.OpensSnapElection, kind.ToString());
        }

        Confidence(ElectionKind.Regular, Incumbent, 0, 1).OpensSnapElection.Should().BeTrue();
        Confidence(ElectionKind.Recall, Incumbent, 0, 1).OpensSnapElection.Should().BeTrue();
        Confidence(ElectionKind.SnapConfidence, Incumbent, 0, 1).OpensSnapElection.Should().BeFalse();
        Confidence(ElectionKind.SnapStepDown, Incumbent, 0, 1).OpensSnapElection.Should().BeFalse();
    }

    #endregion

    #region Who may take part

    private static readonly DateTime RegistrationOpened = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void Voter_MustHaveBeenACitizenSinceRegistrationOpened()
    {
        ElectionRules.IsEligibleVoter("city", "city", false, RegistrationOpened.AddDays(-30), RegistrationOpened).Should().BeTrue();
        ElectionRules.IsEligibleVoter("city", "city", false, RegistrationOpened, RegistrationOpened).Should().BeTrue();
        ElectionRules.IsEligibleVoter("city", "city", false, RegistrationOpened.AddMinutes(1), RegistrationOpened).Should().BeFalse();
    }

    [Test]
    public void Voter_RejoiningResetsTheirCitizenSinceTimeSoTheyCannotVoteInThatElection()
    {
        var leftAndRejoined = RegistrationOpened.AddDays(2);

        ElectionRules.IsEligibleVoter("city", "city", false, leftAndRejoined, RegistrationOpened).Should().BeFalse();
    }

    [Test]
    public void Voter_MustBeACitizenOfThisCityAndNotDeleted()
    {
        var since = RegistrationOpened.AddDays(-30);

        ElectionRules.IsEligibleVoter("other-city", "city", false, since, RegistrationOpened).Should().BeFalse();
        ElectionRules.IsEligibleVoter(string.Empty, "city", false, since, RegistrationOpened).Should().BeFalse();
        ElectionRules.IsEligibleVoter(null, "city", false, since, RegistrationOpened).Should().BeFalse();
        ElectionRules.IsEligibleVoter("city", "city", true, since, RegistrationOpened).Should().BeFalse();
        ElectionRules.IsEligibleVoter("city", string.Empty, false, since, RegistrationOpened).Should().BeFalse();
    }

    [Test]
    public void Voter_WithoutACitizenSinceTimeCannotVote()
    {
        ElectionRules.IsEligibleVoter("city", "city", false, null, RegistrationOpened).Should().BeFalse();
    }

    [Test]
    public void Candidate_MustBeACitizenOfTheCityButNeedNotHaveBeenOneSinceRegistrationOpened()
    {
        ElectionRules.IsCitizenOf("city", "city", false).Should().BeTrue();
        ElectionRules.IsCitizenOf("other-city", "city", false).Should().BeFalse();
        ElectionRules.IsCitizenOf("city", "city", true).Should().BeFalse();
        ElectionRules.IsCitizenOf(string.Empty, string.Empty, false).Should().BeFalse();
    }

    #endregion

    #region Statement

    [Test]
    public void Statement_LimitIsTheHoloNetLimit()
    {
        ElectionRules.StatementMaxLength.Should().Be(600);
    }

    [Test]
    public void Statement_IsTrimmedAndCutToTheLimit()
    {
        ElectionRules.NormalizeStatement(null).Should().BeEmpty();
        ElectionRules.NormalizeStatement("   ").Should().BeEmpty();
        ElectionRules.NormalizeStatement("  Lower the taxes.  ").Should().Be("Lower the taxes.");
        ElectionRules.NormalizeStatement(new string('a', 600)).Should().HaveLength(600);
        ElectionRules.NormalizeStatement(new string('a', 601)).Should().HaveLength(600);
        ElectionRules.NormalizeStatement(new string('a', 5000)).Should().HaveLength(600);
    }

    #endregion

    #region Records

    [Test]
    public void NewElection_IsRegularWithAnEmptyBallotAndNoPlatforms()
    {
        var election = new Election();

        election.Kind.Should().Be(ElectionKind.Regular);
        election.IsConfidenceVote.Should().BeFalse();
        election.CandidatePlayerIds.Should().BeEmpty();
        election.VoterSelections.Should().BeEmpty();
        election.Platforms.Should().BeEmpty();
    }

    [Test]
    public void Election_RoundTripsItsStageDatesKindAndPlatforms()
    {
        var election = new Election
        {
            PropertyId = "city",
            Kind = ElectionKind.SnapConfidence,
            Stage = ElectionStageType.Voting,
            IsConfidenceVote = true,
            RegistrationOpenedUtc = RegistrationOpened,
            RegistrationClosesUtc = RegistrationOpened.AddDays(7),
            VotingClosesUtc = RegistrationOpened.AddDays(14)
        };
        election.CandidatePlayerIds.Add("a");
        election.Platforms["a"] = new ElectionPlatform { Statement = "Hello" };
        election.VoterSelections["cdkey"] = new ElectionVoter { VoterPlayerId = "v", CandidatePlayerId = "a", IsNo = true };

        var copy = Newtonsoft.Json.JsonConvert.DeserializeObject<Election>(Newtonsoft.Json.JsonConvert.SerializeObject(election));

        copy.Kind.Should().Be(ElectionKind.SnapConfidence);
        copy.IsConfidenceVote.Should().BeTrue();
        copy.RegistrationOpenedUtc.Should().Be(RegistrationOpened);
        copy.RegistrationClosesUtc.Should().Be(RegistrationOpened.AddDays(7));
        copy.VotingClosesUtc.Should().Be(RegistrationOpened.AddDays(14));
        copy.Platforms["a"].Statement.Should().Be("Hello");
        copy.VoterSelections["cdkey"].IsNo.Should().BeTrue();
    }

    [Test]
    public void ElectionSavedBeforeTheRevamp_LoadsAsARegularElectionWithNoPlatforms()
    {
        const string legacy = "{\"Id\":\"e1\",\"PropertyId\":\"city\",\"Stage\":2,\"CandidatePlayerIds\":[\"a\",\"b\"],\"VoterSelections\":{\"key\":{\"VoterPlayerId\":\"v\",\"CandidatePlayerId\":\"a\"}}}";

        var election = Newtonsoft.Json.JsonConvert.DeserializeObject<Election>(legacy);

        election.Kind.Should().Be(ElectionKind.Regular);
        election.Stage.Should().Be(ElectionStageType.Voting);
        election.IsConfidenceVote.Should().BeFalse();
        election.Platforms.Should().BeEmpty();
        election.VoterSelections["key"].IsNo.Should().BeFalse();
    }

    #endregion
}
