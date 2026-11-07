using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Neglect and recall: when ignored situations open a recall, and what an empty recall does, step by step.</summary>
public class RecallRulesTests
{
    private static DateTime Nov(int day, int hour = 8) => new(2026, 11, day, hour, 0, 0, DateTimeKind.Utc);

    #region Neglect

    [Test]
    public void ARecallOpens_AtFiveIgnoredSituationsInARow()
    {
        RecallRules.ShouldOpenRecall(4, false, false).Should().BeFalse();
        RecallRules.ShouldOpenRecall(5, false, false).Should().BeTrue();
        RecallRules.ShouldOpenRecall(7, false, false).Should().BeTrue();
    }

    [Test]
    public void ARecall_DoesNotOpenBesideAnotherElection()
    {
        RecallRules.ShouldOpenRecall(5, true, false).Should().BeFalse("a recall is already open, or an election is");
    }

    [Test]
    public void ARecall_DoesNotOpenInACityAlreadyPausedForNeglect()
    {
        RecallRules.ShouldOpenRecall(5, false, true).Should().BeFalse();
    }

    [Test]
    public void ExpiredSituations_AddToTheCount()
    {
        RecallRules.AddExpired(0, 1).Should().Be(1);
        RecallRules.AddExpired(4, 1).Should().Be(5);
        RecallRules.AddExpired(2, 3).Should().Be(5);
        RecallRules.AddExpired(-3, 1).Should().Be(1);
    }

    #endregion

    #region An empty recall

    [Test]
    public void AnEmptyFirstRecall_OpensTheSecond()
    {
        RecallRules.Decide(ElectionKind.Recall, true, 0, 0).Should().Be(RecallStep.OpenSecondRecall);
        RecallRules.Decide(ElectionKind.Recall, true, 0, 40).Should().Be(RecallStep.OpenSecondRecall, "the count only matters for the second");
    }

    [TestCase(0, RecallStep.RemoveCity)]
    [TestCase(4, RecallStep.RemoveCity)]
    [TestCase(5, RecallStep.KeepWithNeglect)]
    [TestCase(6, RecallStep.KeepWithNeglect)]
    public void AnEmptySecondRecall_RemovesACityNobodyUses_AndKeepsOneCitizensUse(int activeCitizens, RecallStep expected)
    {
        RecallRules.Decide(ElectionKind.Recall, true, RecallRules.SecondRecallStage, activeCitizens).Should().Be(expected);
    }

    [Test]
    public void ARecallWithACandidateAndAVote_DoesNothing()
    {
        RecallRules.Decide(ElectionKind.Recall, false, 0, 0).Should().Be(RecallStep.None);
        RecallRules.Decide(ElectionKind.Recall, false, 1, 2).Should().Be(RecallStep.None);
    }

    [TestCase(ElectionKind.Regular)]
    [TestCase(ElectionKind.SnapConfidence)]
    [TestCase(ElectionKind.SnapStepDown)]
    public void OnlyARecallFollowsTheRecallPath(ElectionKind kind)
    {
        RecallRules.Decide(kind, true, 0, 0).Should().Be(RecallStep.None);
        RecallRules.Decide(kind, true, 1, 0).Should().Be(RecallStep.None);
    }

    [Test]
    public void TheCitizensWhoLoggedIn_AreThoseOnOrAfterFourteenDaysBeforeTheRecallEnded()
    {
        var ended = Nov(16);
        var logins = new DateTime?[]
        {
            Nov(2, 8),      // exactly 14 days before: counts
            Nov(2, 7),      // an hour too early
            Nov(10),
            Nov(16),
            null,
            Nov(1)
        };

        RecallRules.GetActivityCutoffUtc(ended).Should().Be(Nov(2, 8));
        RecallRules.CountActiveCitizens(logins, ended).Should().Be(3);
        RecallRules.CountActiveCitizens(null, ended).Should().Be(0);
    }

    #endregion

    #region The Plan's dated example

    // The fifth ignored situation expires on November 2, and a recall opens. Registration closes November 9 with no
    // candidates: the city is marked Abandoned and the second recall opens that day. Registration closes November 16 with
    // no candidates again: citizens whose last login falls between November 2 and November 16 are counted.
    private static (RecallStep First, RecallStep Second, DateTime SecondEnded) Walk(int activeCitizensBetweenNov2AndNov16)
    {
        var neglect = RecallRules.AddExpired(4, 1);
        RecallRules.ShouldOpenRecall(neglect, false, false).Should().BeTrue();

        var firstOpened = Nov(2);
        var firstRegistrationCloses = ElectionRules.GetRegistrationCloseUtc(firstOpened);
        firstRegistrationCloses.Should().Be(Nov(9));
        var firstEnded = ElectionRules.GetEndUtc(firstRegistrationCloses, ElectionRules.GetVotingCloseUtc(firstRegistrationCloses), false);
        firstEnded.Should().Be(Nov(9));

        var firstStep = RecallRules.Decide(ElectionKind.Recall, true, 0, 0);
        var stage = RecallRules.GetNextRecallStage(ElectionKind.Recall, firstStep, 0);
        RecallRules.IsMarkedAbandoned(stage).Should().BeTrue();

        var secondOpened = firstEnded;
        var secondRegistrationCloses = ElectionRules.GetRegistrationCloseUtc(secondOpened);
        secondRegistrationCloses.Should().Be(Nov(16));
        var secondEnded = ElectionRules.GetEndUtc(secondRegistrationCloses, ElectionRules.GetVotingCloseUtc(secondRegistrationCloses), false);

        RecallRules.GetActivityCutoffUtc(secondEnded).Should().Be(Nov(2));
        var secondStep = RecallRules.Decide(ElectionKind.Recall, true, stage, activeCitizensBetweenNov2AndNov16);
        return (firstStep, secondStep, secondEnded);
    }

    [Test]
    public void Example_FourActiveCitizensRemoveTheCity()
    {
        var walk = Walk(4);

        walk.First.Should().Be(RecallStep.OpenSecondRecall);
        walk.Second.Should().Be(RecallStep.RemoveCity);
    }

    [Test]
    public void Example_SixActiveCitizensKeepTheCityWithItsEdictsPausedForNeglect()
    {
        var walk = Walk(6);

        walk.First.Should().Be(RecallStep.OpenSecondRecall);
        walk.Second.Should().Be(RecallStep.KeepWithNeglect);
    }

    [Test]
    public void Example_TheCountComesFromLoginsOnTheDays()
    {
        var logins = new DateTime?[] { Nov(2, 9), Nov(5), Nov(9), Nov(15), Nov(1), Nov(1, 23), null };

        RecallRules.CountActiveCitizens(logins, Nov(16)).Should().Be(4);
    }

    #endregion

    #region What the end of an election does to the counts

    [Test]
    public void TheFirstEmptyRecall_MovesToTheSecondStage_AndASecondEndsTheRecallPath()
    {
        RecallRules.GetNextRecallStage(ElectionKind.Recall, RecallStep.OpenSecondRecall, 0).Should().Be(RecallRules.SecondRecallStage);
        RecallRules.GetNextRecallStage(ElectionKind.Recall, RecallStep.RemoveCity, 1).Should().Be(0);
        RecallRules.GetNextRecallStage(ElectionKind.Recall, RecallStep.KeepWithNeglect, 1).Should().Be(0);
        RecallRules.GetNextRecallStage(ElectionKind.Recall, RecallStep.None, 1).Should().Be(0, "a recall with a candidate and a vote ends the path");
    }

    [Test]
    public void ARegularOrSnapElection_LeavesTheRecallStageAlone()
    {
        RecallRules.GetNextRecallStage(ElectionKind.Regular, RecallStep.None, 0).Should().Be(0);
        RecallRules.GetNextRecallStage(ElectionKind.SnapConfidence, RecallStep.None, 1).Should().Be(1);
    }

    [Test]
    public void TheNeglectCount_StartsOverWhenCitizensTookPart()
    {
        RecallRules.GetNextNeglectCount(5, true, ElectionKind.Recall, RecallStep.None).Should().Be(0);
        RecallRules.GetNextNeglectCount(5, true, ElectionKind.Regular, RecallStep.None).Should().Be(0);
        RecallRules.GetNextNeglectCount(3, true, ElectionKind.SnapConfidence, RecallStep.None).Should().Be(0);
    }

    [TestCase(RecallStep.OpenSecondRecall)]
    [TestCase(RecallStep.RemoveCity)]
    [TestCase(RecallStep.KeepWithNeglect)]
    public void TheNeglectCount_StartsOverWhenARecallEndsEmpty(RecallStep step)
    {
        RecallRules.GetNextNeglectCount(5, false, ElectionKind.Recall, step).Should().Be(0);
    }

    [Test]
    public void AnEmptyOrdinaryElection_LeavesTheNeglectCountAlone()
    {
        RecallRules.GetNextNeglectCount(5, false, ElectionKind.Regular, RecallStep.None).Should().Be(5);
        RecallRules.GetNextNeglectCount(2, false, ElectionKind.SnapConfidence, RecallStep.None).Should().Be(2);
    }

    [Test]
    public void ACityIsMarkedAbandoned_FromTheFirstEmptyRecallUntilTheSecondEnds()
    {
        RecallRules.IsMarkedAbandoned(0).Should().BeFalse();
        RecallRules.IsMarkedAbandoned(RecallRules.SecondRecallStage).Should().BeTrue();
    }

    [Test]
    public void TheRecallOutcomes_MatchTheElectionRulesEmptyResults()
    {
        // ElectionRules reports an empty recall for no candidates, no votes, or every candidate gone; the recall path reads that.
        var noCandidates = ElectionRules.DecideNoCandidates(ElectionKind.Recall, "mayor");
        var noVotes = ElectionRules.DecideVoting(ElectionKind.Recall, false, "mayor", new[] { "a", "b" }, Enumerable.Empty<SWLOR.Game.Server.Entity.ElectionVoter>());
        var everyoneLeft = ElectionRules.DecideVoting(ElectionKind.Recall, false, "mayor", Array.Empty<string>(), Enumerable.Empty<SWLOR.Game.Server.Entity.ElectionVoter>());

        foreach (var outcome in new[] { noCandidates, noVotes, everyoneLeft })
        {
            outcome.IsEmptyRecall.Should().BeTrue();
            RecallRules.Decide(outcome.Kind, outcome.IsEmpty, 0, 0).Should().Be(RecallStep.OpenSecondRecall);
        }
    }

    #endregion
}
