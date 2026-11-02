using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>The login notices about a city's election: the open stage with its days left, and each result once.</summary>
public class ElectionNoticeTextTests
{
    private static readonly DateTime Now = new(2026, 11, 5, 12, 0, 0, DateTimeKind.Utc);

    private static Election Open(ElectionStageType stage, bool confidence = false, ElectionKind kind = ElectionKind.Regular)
    {
        var opened = Now.AddDays(-2);
        return new Election
        {
            Kind = kind,
            Stage = stage,
            IsConfidenceVote = confidence,
            RegistrationOpenedUtc = opened,
            RegistrationClosesUtc = opened.AddDays(7),
            VotingClosesUtc = opened.AddDays(14)
        };
    }

    private static ElectionResultRecord Result(ElectionOutcomeType outcome, bool snap = false, DateTime? next = null)
    {
        return new ElectionResultRecord
        {
            Id = "result-1",
            Kind = ElectionKind.Regular,
            Outcome = outcome,
            WinnerPlayerId = "mayor",
            OpenedSnapElection = snap,
            NextElectionUtc = next,
            PostedUtc = Now
        };
    }

    [Test]
    public void Registration_ShowsTheDaysLeft()
    {
        var text = ElectionNoticeText.BuildStageNotice(Open(ElectionStageType.Registration), Now, true);

        text.Should().Contain("open for candidate registration");
        text.Should().Contain("5 days left");
    }

    [Test]
    public void Registration_OnTheLastDayShowsOneDay()
    {
        var election = Open(ElectionStageType.Registration);
        election.RegistrationClosesUtc = Now.AddHours(10);

        ElectionNoticeText.BuildStageNotice(election, Now, true).Should().Contain("1 day left");
    }

    [Test]
    public void Voting_ShowsTheDaysLeftAndTheReward()
    {
        var text = ElectionNoticeText.BuildStageNotice(Open(ElectionStageType.Voting), Now, true);

        text.Should().Contain("Voting in the city's mayoral election is open");
        text.Should().Contain("12 days left");
        text.Should().Contain("10 RP points");
        text.Should().Contain("every 28 days");
    }

    [Test]
    public void ConfidenceVote_SaysCitizensVoteYesOrNo()
    {
        var text = ElectionNoticeText.BuildStageNotice(Open(ElectionStageType.Voting, confidence: true), Now, true);

        text.Should().Contain("Yes or No");
    }

    [Test]
    public void Voting_ACitizenWhoJoinedAfterRegistrationOpenedIsToldTheyCannotVote()
    {
        var text = ElectionNoticeText.BuildStageNotice(Open(ElectionStageType.Voting), Now, false);

        text.Should().Contain("cannot vote");
        text.Should().NotContain("RP points");
    }

    [TestCase(ElectionKind.Recall, "recall election")]
    [TestCase(ElectionKind.SnapConfidence, "snap election")]
    [TestCase(ElectionKind.SnapStepDown, "snap election")]
    [TestCase(ElectionKind.Regular, "mayoral election")]
    public void StageNotice_NamesTheKindOfElection(ElectionKind kind, string expected)
    {
        ElectionNoticeText.BuildStageNotice(Open(ElectionStageType.Registration, kind: kind), Now, true).Should().Contain(expected);
    }

    [Test]
    public void ResultIsShownOnceAfterItPosts()
    {
        var result = Result(ElectionOutcomeType.NewMayor);
        var citizenSince = Now.AddDays(-90);

        ElectionNoticeText.IsResultUnread(result, null, citizenSince, Now).Should().BeTrue();
        ElectionNoticeText.IsResultUnread(result, string.Empty, citizenSince, Now).Should().BeTrue();
        ElectionNoticeText.IsResultUnread(result, "an older result", citizenSince, Now).Should().BeTrue();
        ElectionNoticeText.IsResultUnread(result, "result-1", citizenSince, Now).Should().BeFalse();
    }

    [Test]
    public void ResultIsNotShownToSomeoneWhoJoinedAfterItPostedOrWhenItIsOld()
    {
        var result = Result(ElectionOutcomeType.NewMayor);

        ElectionNoticeText.IsResultUnread(result, null, Now.AddDays(1), Now.AddDays(2)).Should().BeFalse();
        ElectionNoticeText.IsResultUnread(result, null, null, Now).Should().BeFalse();
        ElectionNoticeText.IsResultUnread(result, null, Now.AddDays(-90), Now.AddDays(27)).Should().BeTrue();
        ElectionNoticeText.IsResultUnread(result, null, Now.AddDays(-90), Now.AddDays(28)).Should().BeFalse();
        ElectionNoticeText.IsResultUnread(null, null, Now.AddDays(-90), Now).Should().BeFalse();
    }

    [Test]
    public void EachResultSaysWhatHappened()
    {
        ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.NoCandidates), "Kira").Should().Contain("Nobody ran, so Kira stays mayor");
        ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.NoVotes), "Kira").Should().Contain("No votes were cast, so Kira stays mayor");
        ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.NewMayor), "Kira").Should().Contain("Kira won and is now mayor");
        ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.IncumbentConfirmed), "Kira").Should().Contain("Kira won and stays mayor");
        ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.ChallengerRejected), "Kira").Should().Contain("voted No on the lone candidate");
        ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.ConfidenceTie), "Kira").Should().Contain("Yes and No tied");
        ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.Tie), "Kira").Should().Contain("tied for the most votes");
    }

    [Test]
    public void NoConfidenceResult_SaysWhetherASnapElectionOpened()
    {
        ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.NoConfidence, snap: true), "Kira")
            .Should().Contain("Citizens voted No on Kira").And.Contain("snap election opens now");
        ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.NoConfidence, snap: false), "Kira")
            .Should().Contain("until the next regular election");
    }

    [Test]
    public void Result_NamesTheNextRegularElectionWhenThereIsOne()
    {
        var text = ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.Tie, next: new DateTime(2026, 11, 28, 0, 0, 0, DateTimeKind.Utc)), "Kira");

        text.Should().Contain("2026-11-28");
        ElectionNoticeText.BuildResultNotice(Result(ElectionOutcomeType.Tie), "Kira").Should().NotContain("next regular election opens");
    }

    [Test]
    public void EveryNoticeIsPlainAscii()
    {
        var notices = Enum.GetValues<ElectionOutcomeType>()
            .Where(x => x != ElectionOutcomeType.Invalid)
            .SelectMany(x => new[]
            {
                ElectionNoticeText.BuildResultNotice(Result(x, snap: true, next: Now), "Kira"),
                ElectionNoticeText.BuildResultNotice(Result(x, snap: false, next: Now), "Kira")
            })
            .Concat(new[]
            {
                ElectionNoticeText.BuildStageNotice(Open(ElectionStageType.Registration), Now, true),
                ElectionNoticeText.BuildStageNotice(Open(ElectionStageType.Voting), Now, true),
                ElectionNoticeText.BuildStageNotice(Open(ElectionStageType.Voting, true), Now, false)
            });

        foreach (var notice in notices)
        {
            notice.Should().NotBeEmpty();
            notice.All(c => c < 128).Should().BeTrue($"'{notice}' must use ASCII punctuation");
        }
    }
}
