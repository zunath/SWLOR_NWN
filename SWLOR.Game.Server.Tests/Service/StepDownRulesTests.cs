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
/// A mayor stepping down: who may be named interim holder, the 7 days to accept, who counts as the incumbent in the snap
/// election, and who may run in it.
/// </summary>
public class StepDownRulesTests
{
    private const string Mayor = "mayor";
    private const string Acting = "acting";
    private const string Officer = "officer";
    private const string Other = "other";

    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static CityStepDown Pending(string holder = Officer) => new()
    {
        InterimHolderPlayerId = holder,
        Stage = CityStepDownStage.Pending,
        NamedUtc = Now,
        AcceptDeadlineUtc = StepDownRules.GetAcceptDeadlineUtc(Now)
    };

    private static CityStepDown Accepted(string holder = Officer)
    {
        var stepDown = Pending(holder);
        stepDown.Stage = CityStepDownStage.Accepted;
        return stepDown;
    }

    #region Who can be named

    [Test]
    public void TheMayorMayNameTheirActingMayorOrAnOfficer()
    {
        var holders = StepDownRules.GetEligibleHolders(Mayor, Acting, Now.AddDays(5), new[] { Officer, "second" }, Now);

        holders.Should().BeEquivalentTo(new[] { Acting, Officer, "second" });
    }

    [Test]
    public void AnActingMayorWhoseTermRanOutIsNotEligible()
    {
        StepDownRules.GetEligibleHolders(Mayor, Acting, Now.AddDays(-1), new[] { Officer }, Now)
            .Should().BeEquivalentTo(new[] { Officer });
    }

    [Test]
    public void SomeoneWhoIsBothAppearsOnce_AndTheMayorNeverAppears()
    {
        var holders = StepDownRules.GetEligibleHolders(Mayor, Acting, Now.AddDays(5), new[] { Acting, Mayor }, Now);

        holders.Should().BeEquivalentTo(new[] { Acting });
    }

    [Test]
    public void AMayorWithNeitherHasNoOneToName()
    {
        StepDownRules.GetEligibleHolders(Mayor, string.Empty, null, Array.Empty<string>(), Now).Should().BeEmpty();
    }

    [Test]
    public void AMayorWithNeitherMustNameAnActingMayorFirst()
    {
        var error = StepDownRules.CheckNaming(CityRole.Mayor, Other, new List<string>(), true, false, null);

        error.Should().Contain("Name an Acting Mayor first");
    }

    [Test]
    public void TheMayorCanStepDownNamingAnEligibleCitizen()
    {
        StepDownRules.CheckNaming(CityRole.Mayor, Officer, new[] { Acting, Officer }, true, false, null).Should().BeEmpty();
        StepDownRules.CheckNaming(CityRole.Mayor, Acting, new[] { Acting, Officer }, true, false, null).Should().BeEmpty();
    }

    [Test]
    public void SomeoneWhoIsNeitherTheActingMayorNorAnOfficerCannotBeNamed()
    {
        StepDownRules.CheckNaming(CityRole.Mayor, Other, new[] { Acting, Officer }, true, false, null)
            .Should().Contain("Acting Mayor or one of your officers");
        StepDownRules.CheckNaming(CityRole.Mayor, string.Empty, new[] { Acting }, true, false, null).Should().NotBeEmpty();
    }

    [TestCase(CityRole.ActingMayor)]
    [TestCase(CityRole.Officer)]
    [TestCase(CityRole.None)]
    [TestCase(CityRole.DepartedMayor)]
    public void OnlyTheSittingMayorCanStepDown(CityRole role)
    {
        StepDownRules.CheckNaming(role, Officer, new[] { Officer }, true, false, null).Should().Contain("Only the mayor");
    }

    [Test]
    public void TheNamedPlayerMustStillBeACitizen()
    {
        StepDownRules.CheckNaming(CityRole.Mayor, Officer, new[] { Officer }, false, false, null).Should().Contain("no longer a citizen");
    }

    [Test]
    public void ACityCannotStepDownWhileAnElectionIsOpen()
    {
        StepDownRules.CheckNaming(CityRole.Mayor, Officer, new[] { Officer }, true, true, null).Should().Contain("election is open");
    }

    [Test]
    public void ACityCannotStartASecondStepDown()
    {
        StepDownRules.CheckNaming(CityRole.Mayor, Officer, new[] { Officer }, true, false, Pending()).Should().Contain("already under way");
        StepDownRules.CheckNaming(CityRole.Mayor, Officer, new[] { Officer }, true, false, Accepted()).Should().Contain("already under way");
    }

    #endregion

    #region The seven days

    [Test]
    public void TheNamedPlayerHasSevenDaysToAccept()
    {
        StepDownRules.AcceptDays.Should().Be(7);
        StepDownRules.GetAcceptDeadlineUtc(Now).Should().Be(Now.AddDays(7));
    }

    [Test]
    public void ThePendingStepDownWaitsForTheNamedPlayerUntilTheDeadline()
    {
        var stepDown = Pending();

        StepDownRules.IsWaitingForAnswerFrom(stepDown, Officer, Now.AddDays(6)).Should().BeTrue();
        StepDownRules.IsWaitingForAnswerFrom(stepDown, Officer, Now.AddDays(7)).Should().BeFalse();
        StepDownRules.IsWaitingForAnswerFrom(stepDown, Other, Now).Should().BeFalse();
        StepDownRules.IsWaitingForAnswerFrom(null, Officer, Now).Should().BeFalse();
        StepDownRules.IsWaitingForAnswerFrom(Accepted(), Officer, Now).Should().BeFalse();
    }

    [Test]
    public void AStepDownNobodyAcceptedInSevenDaysIsExpired()
    {
        StepDownRules.IsExpired(Pending(), Now.AddDays(7).AddSeconds(-1)).Should().BeFalse();
        StepDownRules.IsExpired(Pending(), Now.AddDays(7)).Should().BeTrue();
        StepDownRules.IsExpired(Accepted(), Now.AddDays(30)).Should().BeFalse("an accepted step-down cannot expire");
        StepDownRules.IsExpired(null, Now).Should().BeFalse();
    }

    [Test]
    public void TheNamedPlayerCanAcceptInTime()
    {
        StepDownRules.CheckAcceptance(Pending(), Officer, true, false, Now.AddDays(3)).Should().BeEmpty();
    }

    [Test]
    public void NoOneElseCanAccept_AndNothingIsAcceptedTwice()
    {
        StepDownRules.CheckAcceptance(Pending(), Other, true, false, Now).Should().Contain("No step-down is waiting for you");
        StepDownRules.CheckAcceptance(null, Officer, true, false, Now).Should().Contain("No step-down is waiting for you");
        StepDownRules.CheckAcceptance(Accepted(), Officer, true, false, Now).Should().Contain("No step-down is waiting for you");
    }

    [Test]
    public void AcceptingAfterTheSevenDaysIsRefused()
    {
        StepDownRules.CheckAcceptance(Pending(), Officer, true, false, Now.AddDays(7)).Should().Contain("ran out");
    }

    [Test]
    public void AcceptingNeedsACitizen_AndNoOpenElection()
    {
        StepDownRules.CheckAcceptance(Pending(), Officer, false, false, Now).Should().Contain("no longer a citizen");
        StepDownRules.CheckAcceptance(Pending(), Officer, true, true, Now).Should().Contain("election is open");
    }

    [Test]
    public void TheMayorOrTheNamedPlayerCancelsAPendingStepDown()
    {
        StepDownRules.CheckCancel(Pending(), Mayor, Mayor).Should().BeEmpty();
        StepDownRules.CheckCancel(Pending(), Officer, Mayor).Should().BeEmpty();
        StepDownRules.CheckCancel(Pending(), Other, Mayor).Should().Contain("Only the mayor or the named player");
    }

    [Test]
    public void AnAcceptedStepDownCannotBeCancelled()
    {
        StepDownRules.CheckCancel(Accepted(), Mayor, Mayor).Should().Contain("no step-down to cancel");
        StepDownRules.CheckCancel(null, Mayor, Mayor).Should().Contain("no step-down to cancel");
    }

    #endregion

    #region The stage

    [Test]
    public void OnlyAnAcceptedStepDownMeansTheMayorHasSteppedDown()
    {
        StepDownRules.HasSteppedDown(Accepted()).Should().BeTrue();
        StepDownRules.HasSteppedDown(Pending()).Should().BeFalse("until the holder accepts, the mayor keeps every power");
        StepDownRules.HasSteppedDown(null).Should().BeFalse();
        StepDownRules.IsPending(Pending()).Should().BeTrue();
        StepDownRules.IsPending(Accepted()).Should().BeFalse();
    }

    #endregion

    #region The snap election

    [Test]
    public void TheIncumbentOfAStepDownElectionIsTheInterimHolder()
    {
        StepDownRules.GetIncumbentId(Mayor, Accepted(), true).Should().Be(Officer);
    }

    [Test]
    public void IfTheHolderLeftTheCity_TheMayorOfRecordStaysTheIncumbent()
    {
        StepDownRules.GetIncumbentId(Mayor, Accepted(), false).Should().Be(Mayor);
        StepDownRules.GetIncumbentId(Mayor, null, true).Should().Be(Mayor);
    }

    [Test]
    public void TheMayorWhoSteppedDownCannotRunInTheSnapElection()
    {
        StepDownRules.CheckCandidate(ElectionKind.SnapStepDown, Mayor, Mayor).Should().Contain("stepped down");
    }

    [Test]
    public void EveryoneElseCanRunInTheSnapElection_IncludingTheInterimHolder()
    {
        StepDownRules.CheckCandidate(ElectionKind.SnapStepDown, Officer, Mayor).Should().BeEmpty();
        StepDownRules.CheckCandidate(ElectionKind.SnapStepDown, Other, Mayor).Should().BeEmpty();
    }

    [TestCase(ElectionKind.Regular)]
    [TestCase(ElectionKind.Recall)]
    [TestCase(ElectionKind.SnapConfidence)]
    public void TheMayorMayRunInEveryOtherKindOfElection(ElectionKind kind)
    {
        StepDownRules.CheckCandidate(kind, Mayor, Mayor).Should().BeEmpty();
    }

    [Test]
    public void EachStepDownHasItsOwnElectionId()
    {
        var first = Pending();
        var second = Pending();
        second.NamedUtc = Now.AddDays(30);

        StepDownRules.GetElectionId("city", first).Should().NotBe(StepDownRules.GetElectionId("city", second));
        StepDownRules.GetElectionId("city", first).Should().Be(StepDownRules.GetElectionId("city", first), "opening it twice replaces one record");
        StepDownRules.GetElectionId("city", first).Should().NotBe(StepDownRules.GetElectionId("other", first));
    }

    #endregion

    #region Text

    [Test]
    public void TheTextsUseAsciiPunctuation()
    {
        StepDownRules.BuildSteppedDownText().Should().MatchRegex("^[\\x20-\\x7E]+$");
        StepDownRules.BuildCancelledText("the mayor withdrew it").Should().MatchRegex("^[\\x20-\\x7E]+$").And.Contain("withdrew");
        StepDownRules.BuildStatusText(Pending(), "Bria", Now.AddDays(1)).Should().Contain("Bria").And.Contain("6 days left");
        StepDownRules.BuildStatusText(Accepted(), "Bria", Now).Should().Contain("stepped down").And.Contain("Bria");
        StepDownRules.BuildStatusText(null, "Bria", Now).Should().BeEmpty();
    }

    #endregion
}
