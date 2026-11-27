using System;

namespace SWLOR.Game.Server.Domain.Gameplay
{
    /// <summary>
    /// The state that used to be static in <c>ServerTasks</c>, moved here unchanged. The settings are read the first
    /// time they are needed, as the static field did, behind one <c>protected virtual</c> method.
    /// </summary>
    public class ServerTaskService : IServerTaskService
    {
        private ApplicationSettings _appSettings;
        private DateTime _nextNotification;

        private ApplicationSettings AppSettings => _appSettings ??= LoadSettings();

        protected virtual ApplicationSettings LoadSettings() => ApplicationSettings.Get();

        public string ServerNotificationWebhookUrl => AppSettings.ServerNotificationWebhookUrl;

        public void StartRestartReminders(DateTime bootNow)
        {
            _nextNotification = new DateTime(bootNow.Year, bootNow.Month, bootNow.Day, bootNow.Hour, 0, 0)
                .AddMinutes(1);
        }

        public bool IsRestartReminderDue(DateTime now) => now >= _nextNotification;

        public void ScheduleNextRestartReminder(DateTime now, TimeSpan untilRestart)
        {
            _nextNotification = untilRestart.TotalMinutes <= 15
                ? now.AddMinutes(1)
                : now.AddHours(1);
        }
    }
}
