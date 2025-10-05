using SWLOR.Component.Character.Contracts;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Component.Character.Service
{
    public class ClientVersionCheckService : IClientVersionCheck
    {
        private readonly ILogger _logger;
        private readonly IEventsPluginService _eventsPlugin;
        
        public ClientVersionCheckService(ILogger logger, IEventsPluginService eventsPlugin)
        {
            _logger = logger;
            _eventsPlugin = eventsPlugin;
        }

        /// <summary>
        /// When a player connects to the server, perform a version check on their client.
        /// All of the NUI window features require version 8193.33 or higher but we restrict to 8193.34 or higher
        /// due to fixes applied in .34.
        /// </summary>
        public void CheckVersion()
        {
            const int RequiredMajorVersion = 8193;
            const int RequiredMinorVersion = 34;

            var majorVersion = Convert.ToInt32(_eventsPlugin.GetEventData("VERSION_MAJOR"));
            var minorVersion = Convert.ToInt32(_eventsPlugin.GetEventData("VERSION_MINOR"));

            // Version requirements are met.
            if (majorVersion > RequiredMajorVersion || (majorVersion == RequiredMajorVersion && minorVersion >= RequiredMinorVersion))
                return;

            // Version requirements are not met. Cancel the connection event and provide a reason why as well as instructions to the player on what to do.
            
            var playerName = _eventsPlugin.GetEventData("PLAYER_NAME");
            var cdKey = _eventsPlugin.GetEventData("CDKEY");
            var ipAddress = _eventsPlugin.GetEventData("IP_ADDRESS");
            var platformId = _eventsPlugin.GetEventData("PLATFORM_ID");

            _logger.Write<ConnectionLogGroup>($"{playerName} failed to connect due to old client version. {cdKey} - {ipAddress} - {platformId} - {majorVersion}.{minorVersion}");

            _eventsPlugin.SetEventResult($"Your connection has been denied because you are on an unsupported version of Neverwinter Nights. Please upgrade your game client to {RequiredMajorVersion}.{RequiredMinorVersion} or higher and retry. If you have problems please reach out to us on Discord: https://discord.gg/MyQAM6m");
            _eventsPlugin.SkipEvent();
        }
    }
}
