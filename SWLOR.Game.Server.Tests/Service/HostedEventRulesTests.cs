using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.HostedEventService;

namespace SWLOR.Game.Server.Tests.Service;

public class HostedEventRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 15, 18, 40, 0, DateTimeKind.Utc);

    private static HostedEvent Running(bool joinOpen = true, string arrival = "veles_spaceport") => new()
    {
        Name = "Cantina Night",
        Status = HostedEventStatus.Running,
        StartedUtc = Now.AddMinutes(-45),
        ScheduledStartUtc = Now.AddHours(-1),
        IsJoinOpen = joinOpen,
        ArrivalAreaResref = arrival,
    };

    [Test]
    public void Join_AllowsAPlayerWhenTheEventIsOpen()
    {
        HostedEventRules.GetJoinRefusal(Running(), new HostedEventTravelState()).Should().BeEmpty();
    }

    [Test]
    public void Join_RefusesEventsPlayersCannotReachYet()
    {
        var scheduled = Running();
        scheduled.Status = HostedEventStatus.Scheduled;

        HostedEventRules.GetJoinRefusal(null, new HostedEventTravelState()).Should().Be(HostedEventRules.EventNotFound);
        HostedEventRules.GetJoinRefusal(scheduled, new HostedEventTravelState()).Should().Be(HostedEventRules.NotStarted);
        HostedEventRules.GetJoinRefusal(Running(joinOpen: false), new HostedEventTravelState()).Should().Be(HostedEventRules.JoinClosed);
        HostedEventRules.GetJoinRefusal(Running(arrival: ""), new HostedEventTravelState()).Should().Be(HostedEventRules.NoArrivalPoint);
    }

    [Test]
    public void Join_RefusesDeadFightingFlyingAndOutOfCharacterPlayers()
    {
        HostedEventRules.GetJoinRefusal(Running(), new HostedEventTravelState { IsDead = true }).Should().Be(HostedEventRules.DeadCannotJoin);
        HostedEventRules.GetJoinRefusal(Running(), new HostedEventTravelState { IsInCombat = true }).Should().Be(HostedEventRules.CombatCannotJoin);
        HostedEventRules.GetJoinRefusal(Running(), new HostedEventTravelState { IsInSpace = true }).Should().Be(HostedEventRules.SpaceCannotJoin);
        HostedEventRules.GetJoinRefusal(Running(), new HostedEventTravelState { IsInOutOfCharacterArea = true }).Should().Be(HostedEventRules.OutOfCharacterCannotJoin);
        HostedEventRules.GetJoinRefusal(Running(), new HostedEventTravelState { IsDM = true }).Should().Be(HostedEventRules.DMsUseDMTools);
    }

    [Test]
    public void Join_OneEventAtATime()
    {
        var hostedEvent = Running();

        HostedEventRules.GetJoinRefusal(hostedEvent, new HostedEventTravelState { CurrentEventId = hostedEvent.Id })
            .Should().Be(HostedEventRules.AlreadyAtEvent);
        HostedEventRules.GetJoinRefusal(hostedEvent, new HostedEventTravelState { CurrentEventId = "another" })
            .Should().Be(HostedEventRules.AtAnotherEvent);
    }

    [Test]
    public void OpenToJoin_IgnoresWhetherTheEventHasStarted()
    {
        var scheduled = Running();
        scheduled.Status = HostedEventStatus.Scheduled;

        HostedEventRules.IsOpenToJoin(scheduled).Should().BeTrue("the start prompt asks before the event runs");
        HostedEventRules.IsJoinable(scheduled).Should().BeFalse();
        HostedEventRules.IsOpenToJoin(Running(joinOpen: false)).Should().BeFalse();
        HostedEventRules.IsOpenToJoin(Running(arrival: "")).Should().BeFalse();
    }

    [Test]
    public void Leave_NeedsAnEventAndBlocksCombatUnlessADMSendsPlayersBack()
    {
        var atEvent = new HostedEventTravelState { CurrentEventId = "event" };

        HostedEventRules.GetLeaveRefusal(new HostedEventTravelState()).Should().Be(HostedEventRules.NotAtEvent);
        HostedEventRules.GetLeaveRefusal(atEvent).Should().BeEmpty();
        HostedEventRules.GetLeaveRefusal(atEvent with { IsInCombat = true }).Should().Be(HostedEventRules.CombatCannotLeave);
        HostedEventRules.GetLeaveRefusal(atEvent with { IsInCombat = true }, ignoreCombat: true).Should().BeEmpty();
        HostedEventRules.GetLeaveRefusal(atEvent with { IsDead = true }, ignoreCombat: true).Should().Be(HostedEventRules.DeadCannotLeave);
        HostedEventRules.GetLeaveRefusal(atEvent with { IsInSpace = true }, ignoreCombat: true).Should().Be(HostedEventRules.SpaceCannotLeave);
    }

    [TestCase("2026-11-20 19:30", true)]
    [TestCase(" 2026-11-20 07:05 ", true)]
    [TestCase("2026-11-20", false)]
    [TestCase("20/11/2026 19:30", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void StartTime_ParsesOnlyTheUtcFormat(string text, bool expected)
    {
        HostedEventRules.TryParseStartTime(text, out var start).Should().Be(expected);
        if (expected)
            start.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public void StartTime_RoundTripsWithoutShifting()
    {
        HostedEventRules.TryParseStartTime("2026-11-20 19:30", out var start).Should().BeTrue();

        start.Should().Be(new DateTime(2026, 11, 20, 19, 30, 0, DateTimeKind.Utc));
        HostedEventRules.FormatStartTime(start).Should().Be("2026-11-20 19:30");
    }

    [Test]
    public void DefaultStartTime_IsAWholeHourAtLeastHalfAnHourAway()
    {
        HostedEventRules.GetDefaultStartTime(Now).Should().Be(new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc));
        HostedEventRules.GetDefaultStartTime(Now.AddMinutes(-30)).Should().Be(new DateTime(2026, 11, 15, 19, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public void ValidateDetails_ChecksNameLengthAndStartTime()
    {
        HostedEventRules.ValidateDetails("Cantina Night", "Host", "", "2026-11-20 19:30", out _).Should().BeEmpty();
        HostedEventRules.ValidateDetails(" ", "Host", "", "2026-11-20 19:30", out _).Should().NotBeEmpty();
        HostedEventRules.ValidateDetails(new string('a', HostedEventRules.MaxNameLength + 1), "Host", "", "2026-11-20 19:30", out _).Should().NotBeEmpty();
        HostedEventRules.ValidateDetails("Cantina Night", "Host", new string('a', HostedEventRules.MaxDescriptionLength + 1), "2026-11-20 19:30", out _).Should().NotBeEmpty();
        HostedEventRules.ValidateDetails("Cantina Night", "Host", "", "tomorrow", out _).Should().Contain("UTC");
    }

    [Test]
    public void Expiry_DropsUnstartedEventsAfterTwelveHoursAndRunningOnesAfterADay()
    {
        var scheduled = new HostedEvent { Status = HostedEventStatus.Scheduled, ScheduledStartUtc = Now.AddHours(-11) };
        HostedEventRules.IsExpired(scheduled, Now).Should().BeFalse();
        scheduled.ScheduledStartUtc = Now.AddHours(-12);
        HostedEventRules.IsExpired(scheduled, Now).Should().BeTrue();

        var running = Running();
        running.ScheduledStartUtc = Now.AddDays(-3);
        running.StartedUtc = Now.AddHours(-23);
        HostedEventRules.IsExpired(running, Now).Should().BeFalse();
        running.StartedUtc = Now.AddHours(-24);
        HostedEventRules.IsExpired(running, Now).Should().BeTrue();
    }

    [Test]
    public void Order_ListsRunningEventsFirstThenTheSoonestUpcoming()
    {
        var later = new HostedEvent { Name = "Later", ScheduledStartUtc = Now.AddDays(2) };
        var sooner = new HostedEvent { Name = "Sooner", ScheduledStartUtc = Now.AddHours(2) };
        var running = Running();

        HostedEventRules.OrderForDisplay(new[] { later, sooner, running })
            .Select(e => e.Name).Should().Equal("Cantina Night", "Sooner", "Later");
    }

    [Test]
    public void Display_DescribesLiveUpcomingAndDelayedEvents()
    {
        var upcoming = new HostedEvent { ScheduledStartUtc = Now.AddHours(3).AddMinutes(20) };
        var delayed = new HostedEvent { ScheduledStartUtc = Now.AddMinutes(-10) };

        HostedEventRules.GetListStatus(Running(), Now).Should().Be("Live");
        HostedEventRules.GetListStatus(upcoming, Now).Should().Be("in 3h 20m");
        HostedEventRules.GetListStatus(delayed, Now).Should().Be("Delayed");
        HostedEventRules.DescribeTiming(Running(), Now).Should().Be("Running since 2026-11-15 17:55 UTC (45m ago).");
        HostedEventRules.DescribeTiming(upcoming, Now).Should().Be("Starts 2026-11-15 22:00 UTC (in 3h 20m).");
        HostedEventRules.DescribeTiming(delayed, Now).Should().Contain("Waiting for the host");
    }

    [TestCase(0, "1m")]
    [TestCase(45, "45m")]
    [TestCase(60, "1h")]
    [TestCase(200, "3h 20m")]
    [TestCase(1440, "1d")]
    [TestCase(1620, "1d 3h")]
    public void FormatDuration_Rounds(int minutes, string expected)
    {
        HostedEventRules.FormatDuration(TimeSpan.FromMinutes(minutes)).Should().Be(expected);
    }
}
