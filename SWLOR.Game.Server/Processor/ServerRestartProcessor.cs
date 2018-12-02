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
        private const int RestartMinutes = 480;
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
                _isLoaded = true;
                _restartTime = DateTime.UtcNow.AddMinutes(RestartMinutes);
                _nextNotification = DateTime.UtcNow.AddMinutes(NotificationIntervalMinutes);
            }
        }

        public void Run(object[] args)
        {
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

                // Notify every minute when it comes close to the reboot time.
                if (delta.TotalMinutes <= 15)
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
