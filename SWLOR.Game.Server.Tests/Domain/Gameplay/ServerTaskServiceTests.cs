using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Gameplay;

namespace SWLOR.Game.Server.Tests.Domain.Gameplay;

/// <summary>The server task state: the restart reminder schedule and the webhook setting read through the settings.</summary>
[TestFixture]
public class ServerTaskServiceTests
{
    private sealed class CountingSettings : ServerTaskService
    {
        public int Loads { get; private set; }

        protected override ApplicationSettings LoadSettings()
        {
            Loads++;
            return base.LoadSettings();
        }
    }

    [Test]
    public void TheFirstReminderIsDueOneMinuteAfterTheTopOfTheBootHour()
    {
        var service = new ServerTaskService();

        service.StartRestartReminders(new DateTime(2026, 11, 6, 13, 42, 17));

        service.IsRestartReminderDue(new DateTime(2026, 11, 6, 13, 0, 59)).Should().BeFalse();
        service.IsRestartReminderDue(new DateTime(2026, 11, 6, 13, 1, 0)).Should().BeTrue();
        service.IsRestartReminderDue(new DateTime(2026, 11, 6, 14, 0, 0)).Should().BeTrue();
    }

    [Test]
    public void ARestartMoreThanFifteenMinutesAwayIsRemindedAgainAnHourLater()
    {
        var service = new ServerTaskService();
        var now = new DateTime(2026, 11, 6, 8, 0, 0);

        service.ScheduleNextRestartReminder(now, TimeSpan.FromMinutes(120));

        service.IsRestartReminderDue(now.AddMinutes(59)).Should().BeFalse();
        service.IsRestartReminderDue(now.AddHours(1)).Should().BeTrue();
    }

    [Test]
    public void ARestartFifteenMinutesAwayOrLessIsRemindedEveryMinute()
    {
        var service = new ServerTaskService();
        var now = new DateTime(2026, 11, 6, 9, 46, 0);

        service.ScheduleNextRestartReminder(now, TimeSpan.FromMinutes(15));

        service.IsRestartReminderDue(now.AddSeconds(59)).Should().BeFalse();
        service.IsRestartReminderDue(now.AddMinutes(1)).Should().BeTrue();
    }

    [Test]
    public void TheWebhookComesFromTheSettingsAndTheSettingsAreReadOnce()
    {
        var service = new CountingSettings();

        var first = service.ServerNotificationWebhookUrl;
        var second = service.ServerNotificationWebhookUrl;

        second.Should().Be(first);
        service.Loads.Should().Be(1);
        first.Should().Be(ApplicationSettings.Get().ServerNotificationWebhookUrl);
    }
}
