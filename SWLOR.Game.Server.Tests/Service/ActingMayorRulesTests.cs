using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CityAffairsService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>The Acting Mayor's term: who the mayor may name, how long it lasts, and what ends it.</summary>
public class ActingMayorRulesTests
{
    private const string Mayor = "mayor";
    private const string Target = "target";

    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    #region Term

    [Test]
    public void ATermIsAtMostThirtyDays()
    {
        ActingMayorRules.MaxTermDays.Should().Be(30);
        ActingMayorRules.ClampDays(45).Should().Be(30);
        ActingMayorRules.ClampDays(30).Should().Be(30);
        ActingMayorRules.ClampDays(7).Should().Be(7);
        ActingMayorRules.ClampDays(0).Should().Be(1);
        ActingMayorRules.ClampDays(-3).Should().Be(1);
    }

    [Test]
    public void TheTermChoicesNeverGoPastThirtyDays()
    {
        ActingMayorRules.TermChoicesDays.Should().NotBeEmpty();
        ActingMayorRules.TermChoicesDays.Should().OnlyContain(days => days >= 1 && days <= 30);
        ActingMayorRules.TermChoicesDays.Should().BeInAscendingOrder();
        ActingMayorRules.TermChoicesDays[^1].Should().Be(30);
    }

    [Test]
    public void ATermEndsThatManyDaysAfterItBegan()
    {
        ActingMayorRules.GetEndsUtc(Now, 14).Should().Be(Now.AddDays(14));
        ActingMayorRules.GetEndsUtc(Now, 99).Should().Be(Now.AddDays(30));
    }

    [Test]
    public void ATermRunsUntilItsEndTime()
    {
        var ends = Now.AddDays(5);

        ActingMayorRules.IsActive(Target, ends, Now).Should().BeTrue();
        ActingMayorRules.IsActive(Target, ends, ends.AddSeconds(-1)).Should().BeTrue();
        ActingMayorRules.IsActive(Target, ends, ends).Should().BeFalse("30 days passing ends it");
        ActingMayorRules.IsElapsed(Target, ends, ends).Should().BeTrue();
        ActingMayorRules.IsElapsed(Target, ends, Now).Should().BeFalse();
    }

    [Test]
    public void NoOneNamed_MeansNoTermIsRunningAndNoneHasElapsed()
    {
        ActingMayorRules.IsActive(string.Empty, Now.AddDays(5), Now).Should().BeFalse();
        ActingMayorRules.IsActive(null, null, Now).Should().BeFalse();
        ActingMayorRules.IsElapsed(string.Empty, null, Now).Should().BeFalse();
    }

    [Test]
    public void ARecordWithAPlayerButNoEndTimeIsOver()
    {
        ActingMayorRules.IsActive(Target, null, Now).Should().BeFalse();
        ActingMayorRules.IsElapsed(Target, null, Now).Should().BeTrue();
    }

    #endregion

    #region Naming

    [Test]
    public void TheMayorCanNameACitizenInGoodStanding()
    {
        ActingMayorRules.CheckNaming(CityRole.Mayor, Mayor, Target, true, false).Should().BeEmpty();
    }

    [TestCase(CityRole.ActingMayor)]
    [TestCase(CityRole.Officer)]
    [TestCase(CityRole.None)]
    [TestCase(CityRole.DepartedMayor)]
    public void NoOneButTheSittingMayorNamesAnActingMayor(CityRole role)
    {
        ActingMayorRules.CheckNaming(role, Mayor, Target, true, false).Should().Contain("Only the mayor");
    }

    [Test]
    public void TheTargetMustBeAnotherCitizenOfTheCity()
    {
        ActingMayorRules.CheckNaming(CityRole.Mayor, Mayor, string.Empty, true, false).Should().NotBeEmpty();
        ActingMayorRules.CheckNaming(CityRole.Mayor, Mayor, Mayor, true, false).Should().Contain("own Acting Mayor");
        ActingMayorRules.CheckNaming(CityRole.Mayor, Mayor, Target, false, false).Should().Contain("citizen of this city");
    }

    [Test]
    public void ACitizenWhoOwesTaxesIsNotInGoodStanding()
    {
        ActingMayorRules.CheckNaming(CityRole.Mayor, Mayor, Target, true, true).Should().Contain("good standing");
    }

    #endregion

    #region Ending

    [Test]
    public void OnlyTheMayorEndsATermThatIsRunning()
    {
        ActingMayorRules.CheckEnding(CityRole.Mayor, true).Should().BeEmpty();
        ActingMayorRules.CheckEnding(CityRole.Mayor, false).Should().Contain("no Acting Mayor");
        ActingMayorRules.CheckEnding(CityRole.ActingMayor, true).Should().Contain("Only the mayor");
        ActingMayorRules.CheckEnding(CityRole.Officer, true).Should().Contain("Only the mayor");
    }

    [Test]
    public void AnElectionThatSeatsAMayorEndsTheTerm()
    {
        ActingMayorRules.ElectionEndsTerm(electionHadOutrightWin: true, seatChangedHands: false).Should().BeTrue("the incumbent was confirmed");
        ActingMayorRules.ElectionEndsTerm(electionHadOutrightWin: true, seatChangedHands: true).Should().BeTrue();
        ActingMayorRules.ElectionEndsTerm(electionHadOutrightWin: false, seatChangedHands: true).Should().BeTrue("a step-down status quo seats the interim holder");
    }

    [Test]
    public void AnElectionThatChoosesNobodyLeavesTheTermAlone()
    {
        ActingMayorRules.ElectionEndsTerm(electionHadOutrightWin: false, seatChangedHands: false).Should().BeFalse();
    }

    #endregion

    #region Text

    [Test]
    public void TheGazetteTextsNameNoOneAndUseAsciiPunctuation()
    {
        var named = ActingMayorRules.BuildNamedText(Now.AddDays(30));
        named.Should().Contain("Acting Mayor").And.Contain("2026-12-02 12:00 UTC");
        named.Should().MatchRegex("^[\\x20-\\x7E]+$");

        foreach (var reason in Enum.GetValues<ActingMayorEndReason>())
        {
            ActingMayorRules.BuildEndedText(reason).Should().MatchRegex("^[\\x20-\\x7E]+$", reason.ToString());
        }
    }

    [Test]
    public void EachEndReasonHasItsOwnWords()
    {
        ActingMayorRules.DescribeReason(ActingMayorEndReason.MayorEnded).Should().Contain("mayor ended");
        ActingMayorRules.DescribeReason(ActingMayorEndReason.TermElapsed).Should().Contain("time ran out");
        ActingMayorRules.DescribeReason(ActingMayorEndReason.LeftCity).Should().Contain("left the city");
        ActingMayorRules.DescribeReason(ActingMayorEndReason.ElectionSeatedMayor).Should().Contain("election seated a mayor");
        ActingMayorRules.DescribeReason(ActingMayorEndReason.MayorSteppedDown).Should().Contain("stepped down");
    }

    [Test]
    public void TheStatusLineShowsTheDaysLeft()
    {
        ActingMayorRules.BuildStatusText("Aria", Now.AddDays(3).AddHours(2), Now).Should().Contain("Aria").And.Contain("4 days left");
    }

    #endregion
}
