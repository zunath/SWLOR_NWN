using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.LogService;

namespace SWLOR.Game.Server.Feature
{
    public static class ServerTasks
    {
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
        [NWNEventHandler("mod_heartbeat")]
        public static void ProcessAutoRestart()
        {
            var now = DateTime.UtcNow.TimeOfDay;
            var restartRange = RestartTime.Add(new TimeSpan(0, 0, 0, 30));

            // Current time is within 30 seconds of the specified restart time.
            if ((now > RestartTime) && (now < restartRange))
            {
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
        [NWNEventHandler("mod_load")]
        public static void ProcessBootUp()
        {
            Log.Write(LogGroup.Server, "Server is starting up.");
            ConfigureServerSettings();
            ApplyBans();
            ScheduleRestartReminder();
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
        [NWNEventHandler("mod_enter")]
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
