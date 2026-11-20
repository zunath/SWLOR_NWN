using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Social;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Tests.Domain.Social;

/// <summary>
/// The engine-free event rules: who may travel, how a DM's start time is read and shown, when an event expires, how the
/// list is ordered and how its timing is worded.
/// </summary>
[TestFixture]
public class HostedEventRulesTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

    private static PlayerTimeZone Eastern => PlayerTimeZones.Get("America/New_York");

    private static HostedEvent Running() => new()
    {
        Id = "e1",
        Status = HostedEventStatus.Running,
        StartedUtc = Now.AddHours(-1),
        ScheduledStartUtc = Now.AddHours(-2),
        ArrivalAreaResref = "area_a",
    };

    [Test]
    public void ARunningOpenEventWithAnArrivalPoint_LetsAnIdlePlayerJoin()
    {
        HostedEventRules.GetJoinRefusal(Running(), new HostedEventTravelState()).Should().BeEmpty();
    }

    [Test]
    public void JoinRefusals_AreChosenInOrder()
    {
        var running = Running();
        var idle = new HostedEventTravelState();

        HostedEventRules.GetJoinRefusal(running, new HostedEventTravelState { IsDM = true }).Should().Be(HostedEventRules.DMsUseDMTools);
        HostedEventRules.GetJoinRefusal(null, idle).Should().Be(HostedEventRules.EventNotFound);

        running.Status = HostedEventStatus.Scheduled;
        HostedEventRules.GetJoinRefusal(running, idle).Should().Be(HostedEventRules.NotStarted);

        running.Status = HostedEventStatus.Running;
        running.IsJoinOpen = false;
        HostedEventRules.GetJoinRefusal(running, idle).Should().Be(HostedEventRules.JoinClosed);

        running.IsJoinOpen = true;
        running.ArrivalAreaResref = "  ";
        HostedEventRules.GetJoinRefusal(running, idle).Should().Be(HostedEventRules.NoArrivalPoint);

        running.ArrivalAreaResref = "area_a";
        HostedEventRules.GetJoinRefusal(running, new HostedEventTravelState { CurrentEventId = "e1" }).Should().Be(HostedEventRules.AlreadyAtEvent);
        HostedEventRules.GetJoinRefusal(running, new HostedEventTravelState { CurrentEventId = "other" }).Should().Be(HostedEventRules.AtAnotherEvent);
        HostedEventRules.GetJoinRefusal(running, new HostedEventTravelState { IsDead = true }).Should().Be(HostedEventRules.DeadCannotJoin);
        HostedEventRules.GetJoinRefusal(running, new HostedEventTravelState { IsInSpace = true }).Should().Be(HostedEventRules.SpaceCannotJoin);
        HostedEventRules.GetJoinRefusal(running, new HostedEventTravelState { IsInCombat = true }).Should().Be(HostedEventRules.CombatCannotJoin);
        HostedEventRules.GetJoinRefusal(running, new HostedEventTravelState { IsInOutOfCharacterArea = true }).Should().Be(HostedEventRules.OutOfCharacterCannotJoin);
    }

    [Test]
    public void LeaveRefusals_AreChosenInOrder_AndADMSendingEveryoneBackSkipsCombat()
    {
        var at = new HostedEventTravelState { CurrentEventId = "e1" };

        HostedEventRules.GetLeaveRefusal(new HostedEventTravelState { IsDM = true }).Should().Be(HostedEventRules.DMsUseDMTools);
        HostedEventRules.GetLeaveRefusal(new HostedEventTravelState()).Should().Be(HostedEventRules.NotAtEvent);
        HostedEventRules.GetLeaveRefusal(new HostedEventTravelState { CurrentEventId = "e1", IsDead = true }).Should().Be(HostedEventRules.DeadCannotLeave);
        HostedEventRules.GetLeaveRefusal(new HostedEventTravelState { CurrentEventId = "e1", IsInSpace = true }).Should().Be(HostedEventRules.SpaceCannotLeave);
        var fighting = new HostedEventTravelState { CurrentEventId = "e1", IsInCombat = true };
        HostedEventRules.GetLeaveRefusal(fighting).Should().Be(HostedEventRules.CombatCannotLeave);
        HostedEventRules.GetLeaveRefusal(fighting, ignoreCombat: true).Should().BeEmpty();
        HostedEventRules.GetLeaveRefusal(at).Should().BeEmpty();
    }

    [Test]
    public void JoinabilityNeedsARunningEventThatIsOpenWithAnArrivalPoint()
    {
        var running = Running();
        HostedEventRules.IsJoinable(running).Should().BeTrue();
        HostedEventRules.IsJoinable(null).Should().BeFalse();

        running.Status = HostedEventStatus.Scheduled;
        HostedEventRules.IsJoinable(running).Should().BeFalse();
        HostedEventRules.IsOpenToJoin(running).Should().BeTrue("a scheduled event can still be set up to be joined");

        running.IsJoinOpen = false;
        HostedEventRules.IsOpenToJoin(running).Should().BeFalse();
        HostedEventRules.IsOpenToJoin(null).Should().BeFalse();
        HostedEventRules.HasArrivalPoint(null).Should().BeFalse();
    }

    [Test]
    public void ValidateDetails_AcceptsGoodDetailsAndReturnsTheStartInUtc()
    {
        var error = HostedEventRules.ValidateDetails("Race", "Host", "Come along", "2026-11-20 19:30", Eastern, out var startUtc);

        error.Should().BeEmpty();
        startUtc.Should().Be(new DateTime(2026, 11, 21, 0, 30, 0, DateTimeKind.Utc));
    }

    [Test]
    public void ValidateDetails_ReadsAMissingZoneAsUtc()
    {
        HostedEventRules.ValidateDetails("Race", null, null, "2026-11-20 19:30", null, out var startUtc).Should().BeEmpty();
        startUtc.Should().Be(new DateTime(2026, 11, 20, 19, 30, 0, DateTimeKind.Utc));
    }

    [TestCase(null)]
    [TestCase("   ")]
    public void ValidateDetails_RefusesABlankName(string name)
    {
        HostedEventRules.ValidateDetails(name, "h", "d", "2026-11-20 19:30", null, out _).Should().StartWith("Enter a name");
    }

    [Test]
    public void ValidateDetails_RefusesTooLongTexts()
    {
        HostedEventRules.ValidateDetails(new string('n', 65), "h", "d", "2026-11-20 19:30", null, out _).Should().Contain("Event names");
        HostedEventRules.ValidateDetails("n", new string('h', 65), "d", "2026-11-20 19:30", null, out _).Should().Contain("Host names");
        HostedEventRules.ValidateDetails("n", "h", new string('d', 1001), "2026-11-20 19:30", null, out _).Should().Contain("Descriptions");
    }

    [Test]
    public void ValidateDetails_RefusesAStartThatCannotBeReadOrDoesNotExist()
    {
        HostedEventRules.ValidateDetails("n", "h", "d", "tomorrow", Eastern, out _).Should().Contain("Enter the start time");
        HostedEventRules.ValidateDetails("n", "h", "d", "2026-03-08 02:30", Eastern, out _).Should().Contain("daylight saving");
    }

    [Test]
    public void GetStartToSave_KeepsTheStoredStartWhileTheTextIsUnchanged()
    {
        var stored = new DateTime(2026, 11, 21, 0, 30, 0, DateTimeKind.Utc);
        var typed = stored.AddHours(1);

        HostedEventRules.GetStartToSave(stored, " 2026-11-20 19:30 ", Eastern, typed).Should().Be(stored);
        HostedEventRules.GetStartToSave(stored, "2026-11-20 20:30", Eastern, typed).Should().Be(typed);
        HostedEventRules.GetStartToSave(null, "2026-11-20 19:30", null, typed).Should().Be(typed);
    }

    [Test]
    public void TryParseLocalTime_ReadsTheDocumentedFormatOnly()
    {
        HostedEventRules.TryParseLocalTime(" 2026-11-20 19:30 ", out var local).Should().BeTrue();
        local.Should().Be(new DateTime(2026, 11, 20, 19, 30, 0));
        local.Kind.Should().Be(DateTimeKind.Unspecified);
        HostedEventRules.TryParseLocalTime("20/11/2026 19:30", out _).Should().BeFalse();
        HostedEventRules.TryParseLocalTime(null, out _).Should().BeFalse();
    }

    [Test]
    public void TimesAreShownOnTheZonesClockWithItsShortName()
    {
        var utc = new DateTime(2026, 11, 21, 0, 30, 0, DateTimeKind.Utc);

        HostedEventRules.FormatStartTime(utc, Eastern).Should().Be("2026-11-20 19:30");
        HostedEventRules.FormatZoneTime(utc, Eastern).Should().Be("2026-11-20 19:30 EST");
    }

    [Test]
    public void TheDefaultStartIsTheNextWholeHourAtLeastThirtyMinutesAway()
    {
        HostedEventRules.GetDefaultStartTime(new DateTime(2026, 11, 20, 12, 10, 0, DateTimeKind.Utc), PlayerTimeZones.Utc)
            .Should().Be(new DateTime(2026, 11, 20, 13, 0, 0, DateTimeKind.Utc));
        HostedEventRules.GetDefaultStartTime(new DateTime(2026, 11, 20, 12, 40, 0, DateTimeKind.Utc), PlayerTimeZones.Utc)
            .Should().Be(new DateTime(2026, 11, 20, 14, 0, 0, DateTimeKind.Utc));
    }

    [Test]
    public void AScheduledEventExpiresTwelveHoursAfterItsStart_AndARunningOneTwentyFourAfterItStarted()
    {
        var scheduled = new HostedEvent { ScheduledStartUtc = Now.AddHours(-11) };
        HostedEventRules.IsExpired(scheduled, Now).Should().BeFalse();
        HostedEventRules.IsExpired(scheduled, Now.AddHours(1)).Should().BeTrue();

        var running = Running();
        running.StartedUtc = Now.AddHours(-23);
        HostedEventRules.IsExpired(running, Now).Should().BeFalse();
        HostedEventRules.IsExpired(running, Now.AddHours(1)).Should().BeTrue();

        running.StartedUtc = null;
        running.ScheduledStartUtc = Now.AddHours(-25);
        HostedEventRules.IsExpired(running, Now).Should().BeTrue("a running event without a start time counts from its schedule");
    }

    [Test]
    public void TheListShowsRunningEventsFirstThenScheduledSoonestFirst()
    {
        var late = new HostedEvent { Name = "Late", ScheduledStartUtc = Now.AddHours(5) };
        var soon = new HostedEvent { Name = "soon", ScheduledStartUtc = Now.AddHours(1) };
        var tie = new HostedEvent { Name = "Alpha", ScheduledStartUtc = Now.AddHours(1) };
        var newer = new HostedEvent { Name = "Newer", Status = HostedEventStatus.Running, StartedUtc = Now.AddMinutes(-5), ScheduledStartUtc = Now };
        var older = new HostedEvent { Name = "Older", Status = HostedEventStatus.Running, ScheduledStartUtc = Now.AddHours(-3) };

        HostedEventRules.OrderForDisplay(new[] { late, soon, newer, tie, older }).Select(e => e.Name)
            .Should().Equal("Older", "Newer", "Alpha", "soon", "Late");
    }

    [Test]
    public void ListStatusAndTimingLines_DescribeLiveUpcomingAndDelayedEvents()
    {
        var running = Running();
        HostedEventRules.GetListStatus(running, Now).Should().Be("Live");
        HostedEventRules.DescribeTiming(running, Now, PlayerTimeZones.Utc).Should().Be("Running since 2026-11-20 11:00 UTC (1h ago).");
        running.StartedUtc = null;
        HostedEventRules.DescribeTiming(running, Now, PlayerTimeZones.Utc).Should().StartWith("Running since 2026-11-20 10:00 UTC");

        var upcoming = new HostedEvent { ScheduledStartUtc = Now.AddHours(3).AddMinutes(20) };
        HostedEventRules.GetListStatus(upcoming, Now).Should().Be("in 3h 20m");
        HostedEventRules.DescribeTiming(upcoming, Now, PlayerTimeZones.Utc).Should().Be("Starts 2026-11-20 15:20 UTC (in 3h 20m).");

        var delayed = new HostedEvent { ScheduledStartUtc = Now.AddMinutes(-10) };
        HostedEventRules.GetListStatus(delayed, Now).Should().Be("Delayed");
        HostedEventRules.DescribeTiming(delayed, Now, PlayerTimeZones.Utc).Should().Be("Scheduled for 2026-11-20 11:50 UTC. Waiting for the host to start it.");
    }

    [TestCase(2 * 24 * 60 + 3 * 60, "2d 3h")]
    [TestCase(2 * 24 * 60, "2d")]
    [TestCase(3 * 60 + 20, "3h 20m")]
    [TestCase(3 * 60, "3h")]
    [TestCase(45, "45m")]
    [TestCase(0, "1m")]
    [TestCase(-30, "1m")]
    public void FormatDuration_RoundsToTheLargestUnits(int minutes, string expected)
    {
        HostedEventRules.FormatDuration(TimeSpan.FromMinutes(minutes)).Should().Be(expected);
    }
}
