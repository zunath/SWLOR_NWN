using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.Processor.Contracts;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.ValueObject;


namespace SWLOR.Game.Server.Processor
{
    public static class ServerRestartProcessor
    {
        public static DateTime RestartTime { get; private set; }
        private static DateTime _nextNotification;
        private static bool _isLoaded;
        public static bool IsDisabled { get; private set; }
        private const int DefaultRestartMinutes = 300; // 300 = 5 hours
        private const int NotificationIntervalMinutes = 60;

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnObjectProcessorRan>(message => Run());
        }

        static ServerRestartProcessor()   
        {
            if (!_isLoaded)
            {
                string autoRebootMinutes = Environment.GetEnvironmentVariable("AUTO_REBOOT_MINUTES");
                if(!int.TryParse(autoRebootMinutes, out int minutes))
                {
                    minutes = DefaultRestartMinutes;
                }

                if (minutes <= 0)
                {
                    IsDisabled = true;
                    _isLoaded = true;
                    Console.WriteLine("Server auto-reboot is DISABLED. You can enable this with the AUTO_REBOOT_MINUTES environment variable.");
                }
                else
                {
                    _isLoaded = true;
                    RestartTime = DateTime.UtcNow.AddMinutes(minutes);
                    _nextNotification = DateTime.UtcNow.AddMinutes(minutes < NotificationIntervalMinutes ? 1 : NotificationIntervalMinutes);

                    Console.WriteLine("Server will reboot in " + minutes + " minutes at: " + RestartTime);
                }

            }
        }

        private static void Run()
        {
            if (IsDisabled)
            {
                return;
            }

            using (new Profiler(nameof(ServerRestartProcessor) + "." + nameof(Run)))
            {
                var now = DateTime.UtcNow;
                if (now >= RestartTime)
                {
                    _.ExportAllCharacters();

                    foreach (var player in NWModule.Get().Players)
                    {
                        _.BootPC(player, "Server is automatically rebooting. This is a temporary solution until we can fix performance problems. Thank you for your patience and understanding.");
                    }

                    NWNXAdmin.ShutdownServer();
                }
                else if (now >= _nextNotification)
                {
                    var delta = RestartTime - now;
                    string rebootString = TimeService.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
                    string message = "Server will automatically reboot in " + rebootString;
                    foreach (var player in NWModule.Get().Players)
                    {
                        // Send a message about the next reboot.
                        player.FloatingText(message);

                        // If the player has a lease which is expiring in <= 24 hours, notify them.
                        int leasesExpiring = DataService.PCBase
                            .GetAllByPlayerID(player.GlobalID)
                            .Count(x => x.DateRentDue.AddHours(-24) <= now);
                        if (leasesExpiring > 0)
                        {
                            string leaseDetails = leasesExpiring == 1 ? "1 lease" : leasesExpiring + " leases";
                            player.FloatingText("You have " + leaseDetails + " expiring in less than 24 hours (real world time). Please extend the lease or your land will be forfeited.");
                        }
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
}
