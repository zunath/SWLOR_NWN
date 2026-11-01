using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Research;

namespace SWLOR.Game.Server.Tests.Service.Research;

/// <summary>Covers how a research job is settled at login against the character file sequence the player loaded.</summary>
public class ResearchJobFileRulesTests
{
    [Test]
    public void AJobFromBeforeTheStampsWereKept_IsLeftAlone()
    {
        ResearchJobFileRules.AtLogin(new ResearchJob(), 0).Should().Be(ResearchJobFileState.Current);
    }

    [Test]
    public void AStartedJobTheFileHolds_IsLeftAlone()
    {
        var job = new ResearchJob { StartSequence = 4 };

        ResearchJobFileRules.AtLogin(job, 4).Should().Be(ResearchJobFileState.Current);
        ResearchJobFileRules.AtLogin(job, 9).Should().Be(ResearchJobFileState.Current);
    }

    [Test]
    public void AStartTheFilePredates_IsDiscarded_BecauseTheFileStillHoldsTheBlueprintAndCredits()
    {
        var job = new ResearchJob { StartSequence = 5 };

        ResearchJobFileRules.AtLogin(job, 4).Should().Be(ResearchJobFileState.StartNotInFile);
    }

    [Test]
    public void AStartTheFilePredates_IsDiscarded_EvenWhenAHandBackWasStamped()
    {
        var job = new ResearchJob { StartSequence = 5, DeliverySequence = 6 };

        ResearchJobFileRules.AtLogin(job, 4).Should().Be(ResearchJobFileState.StartNotInFile);
    }

    [Test]
    public void AHandBackTheFileHolds_EndsTheJob()
    {
        var job = new ResearchJob { StartSequence = 5, DeliverySequence = 7 };

        ResearchJobFileRules.AtLogin(job, 7).Should().Be(ResearchJobFileState.DeliveredInFile);
        ResearchJobFileRules.AtLogin(job, 8).Should().Be(ResearchJobFileState.DeliveredInFile);
    }

    [Test]
    public void AHandBackTheFilePredates_KeepsTheJobToCompleteAgain()
    {
        var job = new ResearchJob { StartSequence = 5, DeliverySequence = 7 };

        ResearchJobFileRules.AtLogin(job, 6).Should().Be(ResearchJobFileState.DeliveryNotInFile);
    }

    [Test]
    public void AHandBackOnAJobFromBeforeTheStartStamp_IsJudgedByItsOwnSequence()
    {
        var job = new ResearchJob { DeliverySequence = 3 };

        ResearchJobFileRules.AtLogin(job, 2).Should().Be(ResearchJobFileState.DeliveryNotInFile);
        ResearchJobFileRules.AtLogin(job, 3).Should().Be(ResearchJobFileState.DeliveredInFile);
    }
}
