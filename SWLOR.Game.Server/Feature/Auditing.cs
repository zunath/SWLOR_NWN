using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature
{
    public class Auditing
    {
        /// <summary>
        /// Writes an audit log when a player connects to the server.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void AuditClientConnection()
        {
            var player = GetEnteringObject();
            var ipAddress = GetPCIPAddress(player);
            var cdKey = GetPCPublicCDKey(player);
            var account = GetPCPlayerName(player);
            var pcName = GetName(player);

            var log = $"{pcName} - {account} - {cdKey} - {ipAddress}: Connected to server";
            Log.Write(LogGroup.Connection, log, true);
        }

        /// <summary>
        /// Writes an audit log when a player disconnects from the server.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void AuditClientDisconnection()
        {
            var player = GetExitingObject();
            var ipAddress = GetPCIPAddress(player);
            var cdKey = GetPCPublicCDKey(player);
            var account = GetPCPlayerName(player);
            var pcName = GetName(player);

            var log = $"{pcName} - {account} - {cdKey} - {ipAddress}: Disconnected from server";
            Log.Write(LogGroup.Connection, log, true);
        }

        /// <summary>
        /// Writes an audit log when a player sends a chat message.
        /// </summary>
        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public static void AuditChatMessages()
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
            if (channel == ChatChannel.ServerMessage) return;

            if (channel == ChatChannel.DMTell ||
                channel == ChatChannel.PlayerTell)
            {
                log = BuildTellLog();
            }
            else
            {
                log = BuildRegularLog();
            }

            Log.Write(LogGroup.Chat, log);
        }
    }
}
