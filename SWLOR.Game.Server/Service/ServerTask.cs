using System;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Gameplay;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The runtime state of the server's scheduled tasks. The state lives in <see cref="IServerTaskService"/>; every
    /// member forwards to the container's instance.
    /// </summary>
    public static class ServerTask
    {
        private static IServerTaskService Instance => FacadeAccessor.Resolve<IServerTaskService>();

        public static string ServerNotificationWebhookUrl => Instance.ServerNotificationWebhookUrl;

        public static void StartRestartReminders(DateTime bootNow)
        {
            Instance.StartRestartReminders(bootNow);
        }

        public static bool IsRestartReminderDue(DateTime now)
        {
            return Instance.IsRestartReminderDue(now);
        }

        public static void ScheduleNextRestartReminder(DateTime now, TimeSpan untilRestart)
        {
            Instance.ScheduleNextRestartReminder(now, untilRestart);
        }
    }
}
