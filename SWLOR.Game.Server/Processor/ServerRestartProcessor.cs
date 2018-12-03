using System;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Processor
{
    public class ServerRestartProcessor: IEventProcessor
    {
        private static DateTime _restartTime;
        private static DateTime _nextNotification;
        private static bool _isLoaded;
        private static bool _isDisabled;
        private const int DefaultRestartMinutes = 300; // 300 = 5 hours
        private const int NotificationIntervalMinutes = 60;
        private readonly INWScript _;
        private readonly INWNXAdmin _nwnxAdmin;
        private readonly ITimeService _time;

        public ServerRestartProcessor(
            INWScript script,
            INWNXAdmin nwnxAdmin,
            ITimeService time)
        {
            _ = script;
            _nwnxAdmin = nwnxAdmin;
            _time = time;

            if (!_isLoaded)
            {
                string autoRebootMinutes = Environment.GetEnvironmentVariable("AUTO_REBOOT_MINUTES");
                if(!int.TryParse(autoRebootMinutes, out int minutes))
                {
                    minutes = DefaultRestartMinutes;
                }

                if (minutes <= 0)
                {
                    _isDisabled = true;
                    _isLoaded = true;
                    Console.WriteLine("Server auto-reboot is DISABLED. You can enable this with the AUTO_REBOOT_MINUTES environment variable.");
                }
                else
                {
                    _isLoaded = true;
                    _restartTime = DateTime.UtcNow.AddMinutes(minutes);
                    _nextNotification = DateTime.UtcNow.AddMinutes(minutes < NotificationIntervalMinutes ? 1 : NotificationIntervalMinutes);

                    Console.WriteLine("Server will reboot in " + minutes + " minutes at: " + _restartTime);
                }

            }
        }

        public void Run(object[] args)
        {
            if (_isDisabled)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (now >= _restartTime)
            {
                _.ExportAllCharacters();

                foreach (var player in NWModule.Get().Players)
                {
                    _.BootPC(player, "Server is automatically rebooting. This is a temporary solution until we can fix performance problems. Thank you for your patience and understanding.");
                }

                _nwnxAdmin.ShutdownServer();
            }
            else if(now >= _nextNotification)
            {
                var delta = _restartTime - now;
                string rebootString = _time.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
                string message = "Server will automatically reboot in " + rebootString;
                foreach (var player in NWModule.Get().Players)
                {
                    player.FloatingText(message);
                }
                Console.WriteLine(message);
                
                // We're in the last hour before rebooting. Schedule the next notification 45 minutes from now.
                if (delta.TotalHours <= 1 && delta.TotalMinutes >= 45)
                {
                    _nextNotification = DateTime.UtcNow.AddMinutes(45);
                }
                // Notify every minute when it comes close to the reboot time.
                else if (delta.TotalMinutes <= 15)
                {
                    _nextNotification = DateTime.UtcNow.AddMinutes(1);
                }
                // Otherwise notify on the standard timing.
                else
                {
                    _nextNotification = DateTime.UtcNow.AddMinutes(NotificationIntervalMinutes);
                }
            }
        }
    }
}
