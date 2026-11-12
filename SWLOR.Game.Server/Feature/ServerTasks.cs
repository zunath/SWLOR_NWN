using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Gameplay;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWNX;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature
{
    public static class ServerTasks
    {
        private const int LifecycleNotificationColor = 15158332; // #E74C3C (red)
        // This determines what time the server will restart.
        // Restarts happen within a range of 30 seconds of this specified time.
        // All times are in UTC.
        private static readonly TimeSpan RestartTime = new TimeSpan(0, 10, 0, 0); // 0 = Restarts happen at 6 AM eastern time

        /// <summary>
        /// Every six seconds, the server will check to see if an automated restart is required.
        /// The time must be within 30 seconds of the schedule restart time (see RestartTime above)
        /// Players will be booted with a message stating this is happening and the server will shut down.
        /// The server application is expected to restart the server when it sees it's down.
        /// This isn't handled by the C# code and should be set up on your server.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorHeartbeat)]
        public static void ProcessAutoRestart()
        {
            var now = DateTime.UtcNow.TimeOfDay;
            var restartRange = RestartTime.Add(new TimeSpan(0, 0, 0, 30));

            // Current time is within 30 seconds of the specified restart time.
            if ((now > RestartTime) && (now < restartRange))
            {
                SendServerLifecycleNotificationForShutdown("Automated restart has started. Server is shutting down now.");

                ServerRestart.BootAllPlayers("The server is automatically restarting.");

                Log.Write(LogGroup.Server, "Server shutting down for automated restart.", true);

                DelayCommand(0.1f, () =>
                {
                    AdministrationPlugin.ShutdownServer();
                });
            }
        }

        /// <summary>
        /// When the server starts up, a log message will be written, the play options and bans are set in the engine and
        /// the boot-complete notification is sent. Boot only; <see cref="StartRestartReminders"/> runs right after it.
        /// </summary>
        [BootPhase(BootReload.BootOnly, NothingToAdopt = "The play options and bans stay set in the engine, and a reload sends no boot notification.")]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ProcessBootUp()
        {
            Log.Write(LogGroup.Server, "Server is starting up.");
            ConfigureServerSettings();
            ApplyBans();
            _ = SendServerLifecycleNotification("Server boot process is complete. Server is fully online and available for play.");
        }

        /// <summary>
        /// Starts the restart reminders after <see cref="ProcessBootUp"/>: the reminder state and its one-minute timer live
        /// in the game build, so a hot reload runs it again.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void StartRestartReminders()
        {
            ScheduleRestartReminder();
        }

        [BootPhase(BootReload.BootOnly, NothingToAdopt = "A reload sends no boot notification.")]
        [NWNEventHandler(ScriptName.OnModulePreload)]
        public static void ProcessBootStart()
        {
            _ = SendServerLifecycleNotification("Server boot process has started. Please wait until it is fully online before joining.");
        }


        public static void SendServerLifecycleNotificationForShutdown(string message)
        {
            if (string.IsNullOrWhiteSpace(ServerTask.ServerNotificationWebhookUrl))
                return;

            try
            {
                var enqueueTask = SendServerLifecycleNotification(message);
                var completedInTime = enqueueTask.Wait(TimeSpan.FromSeconds(2));
                if (!completedInTime)
                {
                    Log.Write(LogGroup.Error, "SendServerLifecycleNotificationForShutdown: Timed out waiting for SendServerLifecycleNotification before shutdown.");
                }
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"SendServerLifecycleNotificationForShutdown: Unexpected error while waiting for SendServerLifecycleNotification before shutdown. {ex}");
            }
        }
        public static async Task<bool> SendServerLifecycleNotification(string message)
        {
            var url = ServerTask.ServerNotificationWebhookUrl;
            if (string.IsNullOrWhiteSpace(url)) return false;

            var authorName = "SWLOR Server";

            try
            {
                var enqueued = await BackgroundJob.EnqueueDiscordWebhook(url, authorName, message, LifecycleNotificationColor);
                if (!enqueued)
                {
                    Log.Write(LogGroup.Error, $"SendServerLifecycleNotification: BackgroundJob.EnqueueDiscordWebhook returned false for message: {message}");
                }

                return enqueued;
            }
            catch (Exception ex)
            {
                Log.Write(LogGroup.Error, $"SendServerLifecycleNotification: BackgroundJob.EnqueueDiscordWebhook threw for message: {message}. {ex}");
                return false;
            }
        }

        private static void ConfigureServerSettings()
        {
            AdministrationPlugin.SetPlayOption(AdministrationOption.ExamineChallengeRating, false);
            AdministrationPlugin.SetPlayOption(AdministrationOption.UseMaxHitpoints, true);
        }

        private static void ApplyBans()
        {
            var query = new DBQuery<PlayerBan>();

            var dbBanCount = (int)DB.SearchCount(query);
            var dbBans = DB.Search(query.AddPaging(dbBanCount, 0));

            foreach (var ban in dbBans)
            {
                AdministrationPlugin.AddBannedCDKey(ban.CDKey);
            }
        }

        private static void ScheduleRestartReminder()
        {
            var bootNow = DateTime.UtcNow;
            ServerTask.StartRestartReminders(bootNow);

            Scheduler.ScheduleRepeating(() =>
            {
                var now = DateTime.UtcNow;
                var restartDate = new DateTime(now.Year, now.Month, now.Day, RestartTime.Hours, RestartTime.Minutes, RestartTime.Seconds);

                if (RestartTime < now.TimeOfDay)
                {
                    restartDate = restartDate.AddDays(1);
                }

                if (ServerTask.IsRestartReminderDue(now))
                {
                    var delta = restartDate - now;
                    var rebootString = Time.GetTimeLongIntervals(delta, false);
                    var message = $"Server will automatically reboot in approximately {rebootString}.";

                    Log.Write(LogGroup.Server, message, true);

                    for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                    {
                        if (DMPresence.IsControlledByDM(player))
                        {
                            DMPresence.SendMessage(player, message);
                            continue;
                        }

                        var playerId = GetObjectUUID(player);
                        var dbPlayer = DB.Get<Player>(playerId);

                        if (dbPlayer != null && dbPlayer.Settings.DisplayServerResetReminders)
                            SendMessageToPC(player, message);
                    }

                    ServerTask.ScheduleNextRestartReminder(now, delta);
                }
            }, TimeSpan.FromMinutes(1));
        }


        /// <summary>
        /// When a player enters the server, send them a greeting and a link to the Discord server.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void WelcomeMessage()
        {
            var player = GetEnteringObject();
            DelayCommand(2f, () =>
            {
                SendMessageToPC(player, ColorToken.Green("Welcome to Star Wars: Legends of the Old Republic!\n") +
                                        ColorToken.White("Join our Discord at: https://discord.gg/MyQAM6m"));
            });
        }
    }
}
