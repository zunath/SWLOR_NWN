using SWLOR.Component.Admin.Contracts;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Component.Admin.Service
{
    public class AuditingService : IAuditingService
    {
        private readonly ILogger _logger;
        private readonly IChatPluginService _chatPlugin;

        public AuditingService(ILogger logger, IChatPluginService chatPlugin)
        {
            _logger = logger;
            _chatPlugin = chatPlugin;
        }
        /// <summary>
        /// Writes an audit log when a player connects to the server.
        /// </summary>
        public void AuditClientConnection()
        {
            var player = GetEnteringObject();
            var ipAddress = GetPCIPAddress(player);
            var cdKey = GetPCPublicCDKey(player);
            var account = GetPCPlayerName(player);
            var pcName = GetName(player);

            var log = $"{pcName} - {account} - {cdKey} - {ipAddress}: Connected to server";
            _logger.Write<ConnectionLogGroup>(log);
        }

        /// <summary>
        /// Writes an audit log when a player disconnects from the server.
        /// </summary>
        public void AuditClientDisconnection()
        {
            var player = GetExitingObject();
            var ipAddress = GetPCIPAddress(player);
            var cdKey = GetPCPublicCDKey(player);
            var account = GetPCPlayerName(player);
            var pcName = GetName(player);

            var log = $"{pcName} - {account} - {cdKey} - {ipAddress}: Disconnected from server";
            _logger.Write<ConnectionLogGroup>(log);
        }

        /// <summary>
        /// Writes an audit log when a player sends a chat message.
        /// </summary>
        public void AuditChatMessages()
        {
            string BuildRegularLog()
            {
                var sender = _chatPlugin.GetSender();
                var chatChannel = _chatPlugin.GetChannel();
                var message = _chatPlugin.GetMessage();
                var ipAddress = GetPCIPAddress(sender);
                var cdKey = GetPCPublicCDKey(sender);
                var account = GetPCPlayerName(sender);
                var pcName = GetName(sender);

                var logMessage = $"{pcName} - {account} - {cdKey} - {ipAddress} - {chatChannel}: {message}";

                return logMessage;
            }

            string BuildTellLog()
            {
                var sender = _chatPlugin.GetSender();
                var receiver = _chatPlugin.GetTarget();
                var chatChannel = _chatPlugin.GetChannel();
                var message = _chatPlugin.GetMessage();
                var senderIPAddress = GetPCIPAddress(sender);
                var senderCDKey = GetPCPublicCDKey(sender);
                var senderAccount = GetPCPlayerName(sender);
                var senderPCName = GetName(sender);
                var receiverIPAddress = GetPCIPAddress(receiver);
                var receiverCDKey = GetPCPublicCDKey(receiver);
                var receiverAccount = GetPCPlayerName(receiver);
                var receiverPCName = GetName(receiver);

                var logMessage = $"{senderPCName} - {senderAccount} - {senderCDKey} - {senderIPAddress} - {chatChannel} (SENT TO {receiverPCName} - {receiverAccount} - {receiverCDKey} - {receiverIPAddress}): {message}";
                return logMessage;
            }

            var channel = _chatPlugin.GetChannel();
            string log;

            // We don't log server messages because there isn't a good way to filter them.
            if (channel == ChatChannelType.ServerMessage) return;

            if (channel == ChatChannelType.DMTell ||
                channel == ChatChannelType.PlayerTell)
            {
                log = BuildTellLog();
            }
            else
            {
                log = BuildRegularLog();
            }

            _logger.Write<ChatLogGroup>(log);
        }
    }
}
