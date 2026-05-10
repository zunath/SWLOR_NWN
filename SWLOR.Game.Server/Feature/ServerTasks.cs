using System;
using System.Threading.Tasks;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature
{
    public static class ServerTasks
    {
        private static readonly ApplicationSettings _appSettings = ApplicationSettings.Get();
        private const int LifecycleNotificationColor = 15158332; // #E74C3C (red)
        // This determines what time the server will restart.
        // Restarts happen within a range of 30 seconds of this specified time. 
        // All times are in UTC.
        private static TimeSpan RestartTime => new TimeSpan(0, 10, 0, 0); // 0 = Restarts happen at 6 AM eastern time
        private static DateTime _nextNotification;
        
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

                for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                {
                    ExportSingleCharacter(player);
                    BootPC(player, "The server is automatically restarting.");
                }

                Log.Write(LogGroup.Server, "Server shutting down for automated restart.", true);
                DelayCommand(0.1f, () =>
                {
                    AdministrationPlugin.ShutdownServer();
                });
            }
        }

        /// <summary>
        /// When the server starts up, a log message will be written.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ProcessBootUp()
        {
            Log.Write(LogGroup.Server, "Server is starting up.");
            ConfigureServerSettings();
            ApplyBans();
            ScheduleRestartReminder();
            _ = SendServerLifecycleNotification("Server boot process is complete. Server is fully online and available for play.");
        }

        [NWNEventHandler(ScriptName.OnModulePreload)]
        public static void ProcessBootStart()
        {
            _ = SendServerLifecycleNotification("Server boot process has started. Please wait until it is fully online before joining.");
        }


        public static void SendServerLifecycleNotificationForShutdown(string message)
        {
            if (string.IsNullOrWhiteSpace(_appSettings.ServerNotificationWebhookUrl))
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
            var url = _appSettings.ServerNotificationWebhookUrl;
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
            _nextNotification = new DateTime(bootNow.Year, bootNow.Month, bootNow.Day, bootNow.Hour, 0, 0)
                .AddMinutes(1);

            Scheduler.ScheduleRepeating(() =>
            {
                var now = DateTime.UtcNow;
                var restartDate = new DateTime(now.Year, now.Month, now.Day, RestartTime.Hours, RestartTime.Minutes, RestartTime.Seconds);

                if (RestartTime < now.TimeOfDay)
                {
                    restartDate = restartDate.AddDays(1);
                }
                
                if (now >= _nextNotification)
                {
                    var delta = restartDate - now;
                    var rebootString = Time.GetTimeLongIntervals(delta, false);
                    var message = $"Server will automatically reboot in approximately {rebootString}.";

                    Log.Write(LogGroup.Server, message, true);

                    for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                    {
                        var playerId = GetObjectUUID(player);
                        var dbPlayer = DB.Get<Player>(playerId);

                        if(GetIsDM(player) || GetIsDMPossessed(player) || (dbPlayer != null && dbPlayer.Settings.DisplayServerResetReminders))
                            SendMessageToPC(player, message);
                    }

                    _nextNotification = delta.TotalMinutes <= 15 
                        ? now.AddMinutes(1) 
                        : now.AddHours(1);
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
