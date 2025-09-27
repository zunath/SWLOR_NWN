using SWLOR.Component.Admin.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;

namespace SWLOR.Component.Admin.Service
{
    public class AuditingService : IAuditingService
    {
        private readonly ILogger _logger;

        public AuditingService(ILogger logger)
        {
            _logger = logger;
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
            static string BuildRegularLog()
            {
                var sender = ChatPlugin.GetSender();
                var chatChannel = ChatPlugin.GetChannel();
                var message = ChatPlugin.GetMessage();
                var ipAddress = GetPCIPAddress(sender);
                var cdKey = GetPCPublicCDKey(sender);
                var account = GetPCPlayerName(sender);
                var pcName = GetName(sender);

                var logMessage = $"{pcName} - {account} - {cdKey} - {ipAddress} - {chatChannel}: {message}";

                return logMessage;
            }

            static string BuildTellLog()
            {
                var sender = ChatPlugin.GetSender();
                var receiver = ChatPlugin.GetTarget();
                var chatChannel = ChatPlugin.GetChannel();
                var message = ChatPlugin.GetMessage();
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

            var channel = ChatPlugin.GetChannel();
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
