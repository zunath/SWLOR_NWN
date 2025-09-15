using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature
{
    public static class ClientVersionCheck
    {
        /// <summary>
        /// When a player connects to the server, perform a version check on their client.
        /// All of the NUI window features require version 8193.33 or higher but we restrict to 8193.34 or higher
        /// due to fixes applied in .34.
        /// </summary>
        [NWNEventHandler(ScriptName.OnClientConnectBefore)]
        public static void CheckVersion()
        {
            const int RequiredMajorVersion = 8193;
            const int RequiredMinorVersion = 34;

            var majorVersion = Convert.ToInt32(EventsPlugin.GetEventData("VERSION_MAJOR"));
            var minorVersion = Convert.ToInt32(EventsPlugin.GetEventData("VERSION_MINOR"));

            // Version requirements are met.
            if (majorVersion > RequiredMajorVersion || (majorVersion == RequiredMajorVersion && minorVersion >= RequiredMinorVersion))
                return;

            // Version requirements are not met. Cancel the connection event and provide a reason why as well as instructions to the player on what to do.
            
            var playerName = EventsPlugin.GetEventData("PLAYER_NAME");
            var cdKey = EventsPlugin.GetEventData("CDKEY");
            var ipAddress = EventsPlugin.GetEventData("IP_ADDRESS");
            var platformId = EventsPlugin.GetEventData("PLATFORM_ID");

            Log.Write(LogGroup.Connection, $"{playerName} failed to connect due to old client version. {cdKey} - {ipAddress} - {platformId} - {majorVersion}.{minorVersion}");

            EventsPlugin.SetEventResult($"Your connection has been denied because you are on an unsupported version of Neverwinter Nights. Please upgrade your game client to {RequiredMajorVersion}.{RequiredMinorVersion} or higher and retry. If you have problems please reach out to us on Discord: https://discord.gg/MyQAM6m");
            EventsPlugin.SkipEvent();
        }
    }
}
