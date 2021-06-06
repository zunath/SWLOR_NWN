using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Service;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class ServerTasks
    {
        // This determines what time the server will restart.
        // Restarts happen within a range of 30 seconds of this specified time. 
        // All times are in UTC.
        private static TimeSpan RestartTime => new TimeSpan(0, 10, 0, 0); // 0 = Restarts happen at 6 AM eastern time

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
                    BootPC(player, "The server is automatically restarting.");
                }

                Log.Write(LogGroup.Server, "Server shutting down for automated restart.", true);
                Administration.ShutdownServer();
            }
        }

        /// <summary>
        /// When the server starts up, a log message will be written.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void ProcessBootUp()
        {
            Log.Write(LogGroup.Server, "Server is starting up.");
        }
    }
}
