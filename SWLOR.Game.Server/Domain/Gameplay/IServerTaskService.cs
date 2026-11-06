using System;

namespace SWLOR.Game.Server.Domain.Gameplay
{
    /// <summary>
    /// The runtime state of the server's scheduled tasks: the lifecycle notification webhook setting and the time of the
    /// next automatic-restart reminder. The tasks themselves (boot, restart, reminders, welcome message) stay in the
    /// <c>ServerTasks</c> Feature class.
    /// </summary>
    public interface IServerTaskService
    {
        /// <summary>The Discord webhook the server lifecycle notifications are sent to, or empty when none is set.</summary>
        string ServerNotificationWebhookUrl { get; }

        /// <summary>Sets the first reminder time to one minute after the top of the hour of <paramref name="bootNow"/>.</summary>
        void StartRestartReminders(DateTime bootNow);

        /// <summary>Whether the next restart reminder is due at <paramref name="now"/>.</summary>
        bool IsRestartReminderDue(DateTime now);

        /// <summary>
        /// Schedules the reminder after one sent at <paramref name="now"/>: a minute later when the restart is
        /// 15 minutes away or less, otherwise an hour later.
        /// </summary>
        void ScheduleNextRestartReminder(DateTime now, TimeSpan untilRestart);
    }
}
