using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Admin.Feature
{
    public class ServerTasks
    {
        private readonly ILogger _logger;

        private readonly IDatabaseService _db;
        private readonly ITimeService _timeService;
        private readonly IScheduler _scheduler;
        private readonly IAdministrationPluginService _administrationPlugin;
        private readonly IPlayerBanRepository _playerBanRepository;

        public ServerTasks(
            ILogger logger, 
            IDatabaseService db, 
            IScheduler scheduler,
            ITimeService timeService,
            IAdministrationPluginService administrationPlugin,
            IPlayerBanRepository playerBanRepository)
        {
            _logger = logger;
            _db = db;
            _scheduler = scheduler;
            _timeService = timeService;
            _administrationPlugin = administrationPlugin;
            _playerBanRepository = playerBanRepository;
        }

        // This determines what time the server will restart.
        // Restarts happen within a range of 30 seconds of this specified time. 
        // All times are in UTC.
        private TimeSpan RestartTime => new(0, 10, 0, 0); // 0 = Restarts happen at 6 AM eastern time
        private DateTime _nextNotification;
        
        /// <summary>
        /// Every six seconds, the server will check to see if an automated restart is required.
        /// The time must be within 30 seconds of the schedule restart time (see RestartTime above)
        /// Players will be booted with a message stating this is happening and the server will shut down.
        /// The server application is expected to restart the server when it sees it's down.
        /// This isn't handled by the C# code and should be set up on your server.
        /// </summary>
        public void ProcessAutoRestart()
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

                _logger.Write<ServerLogGroup>("Server shutting down for automated restart.");
                
                DelayCommand(0.1f, () =>
                {
                    _administrationPlugin.ShutdownServer();
                });
            }
        }

        /// <summary>
        /// When the server starts up, a log message will be written.
        /// </summary>
        public void ProcessBootUp()
        {
            _logger.Write<ServerLogGroup>("Server is starting up.");
            ConfigureServerSettings();
            ApplyBans();
            ScheduleRestartReminder();
        }

        private void ConfigureServerSettings()
        {
            _administrationPlugin.SetPlayOption(AdministrationOptionType.ExamineChallengeRating, false);
            _administrationPlugin.SetPlayOption(AdministrationOptionType.UseMaxHitpoints, true);
        }

        private void ApplyBans()
        {
            var dbBanCount = (int)_playerBanRepository.GetCount();
            var dbBans = _playerBanRepository.GetAll();

            foreach (var ban in dbBans)
            {
                _administrationPlugin.AddBannedCDKey(ban.CDKey);
            }
        }

        private void ScheduleRestartReminder()
        {
            var bootNow = DateTime.UtcNow;
            _nextNotification = new DateTime(bootNow.Year, bootNow.Month, bootNow.Day, bootNow.Hour, 0, 0)
                .AddMinutes(1);

            _scheduler.ScheduleRepeating(() =>
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
                    var rebootString = _timeService.GetTimeLongIntervals(delta, false);
                    var message = $"Server will automatically reboot in approximately {rebootString}.";

                    _logger.Write<ServerLogGroup>(message);

                    for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())
                    {
                        var playerId = GetObjectUUID(player);
                        var dbPlayer = _db.Get<Player>(playerId);

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
        public void WelcomeMessage()
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
