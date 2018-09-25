using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data.Entities;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class ActivityLoggingService: IActivityLoggingService
    {
        private readonly INWScript _;
        private readonly IDataContext _db;
        private readonly INWNXChat _nwnxChat;

        public ActivityLoggingService(INWScript script, IDataContext db, INWNXChat nwnxChat)
        {
            _ = script;
            _db = db;
            _nwnxChat = nwnxChat;
        }

        public void OnModuleClientEnter()
        {
            NWPlayer oPC = (_.GetEnteringObject());
            string name = oPC.Name;
            string cdKey = _.GetPCPublicCDKey(oPC.Object);
            string account = _.GetPCPlayerName(oPC.Object);
            DateTime now = DateTime.UtcNow;
            string nowString = now.ToString("yyyy-MM-dd hh:mm:ss");

            // CD Key and accounts are stored as local strings on the PC
            // because they cannot be retrieved using NWScript functions
            // on the module OnClientLeave event.

            oPC.SetLocalString("PC_CD_KEY", cdKey);
            oPC.SetLocalString("PC_ACCOUNT", account);

            Console.WriteLine(nowString + ": " + name + " (" + account + "/" + cdKey + ") connected to the server.");
            
            ClientLogEvent entity = new ClientLogEvent
            {
                AccountName = account,
                CDKey = cdKey,
                ClientLogEventTypeID = 1,
                PlayerID = oPC.IsDM ? null : oPC.GlobalID,
                DateOfEvent = now
            };
            
            _db.ClientLogEvents.Add(entity);
            _db.SaveChanges();
        }

        public void OnModuleClientLeave()
        {
            NWPlayer oPC = (_.GetExitingObject());
            string name = oPC.Name;
            string cdKey = oPC.GetLocalString("PC_CD_KEY");
            string account = oPC.GetLocalString("PC_ACCOUNT");
            DateTime now = DateTime.UtcNow;
            string nowString = now.ToString("yyyy-MM-dd hh:mm:ss");

            Console.WriteLine(nowString + ": " + name + " (" + account + "/" + cdKey + ") left the server.");

            ClientLogEvent entity = new ClientLogEvent
            {
                AccountName = account,
                CDKey = cdKey,
                ClientLogEventTypeID = 2,
                PlayerID = oPC.IsDM ? null : oPC.GlobalID,
                DateOfEvent = now
            };

            _db.ClientLogEvents.Add(entity);
            _db.SaveChanges();
        }


        private static int ConvertNWNXChatChannelIDToDatabaseID(int nwnxChatChannelID)
        {
            switch (nwnxChatChannelID)
            {
                case (int)ChatChannelType.PlayerTalk:
                case (int)ChatChannelType.DMTalk:
                    return 3;
                case (int)ChatChannelType.PlayerShout:
                case (int)ChatChannelType.DMShout:
                    return 1;
                case (int)ChatChannelType.PlayerWhisper:
                case (int)ChatChannelType.DMWhisper:
                    return 2;
                case (int)ChatChannelType.PlayerTell:
                case (int)ChatChannelType.DMTell:
                    return 6;
                case (int)ChatChannelType.ServerMessage:
                    return 7;
                case (int)ChatChannelType.PlayerParty:
                case (int)ChatChannelType.DMParty:
                    return 4;
                default:
                    return 5;
            }
        }
        
        public void OnModuleNWNXChat(NWPlayer sender)
        {
            if (!sender.IsPlayer && !sender.IsDM) return;
            string text = _nwnxChat.GetMessage();
            if (string.IsNullOrWhiteSpace(text)) return;

            int mode = _nwnxChat.GetChannel();
            int channel = ConvertNWNXChatChannelIDToDatabaseID(mode);
            NWObject recipient = _nwnxChat.GetTarget();
            ChatChannelsDomain channelEntity = _db.ChatChannelsDomains.Single(x => x.ChatChannelID == channel);

            // Sender - should always have this data.
            string senderCDKey = _.GetPCPublicCDKey(sender.Object);
            string senderAccountName = sender.Name;
            string senderPlayerID = null;
            string senderDMName = null;

            // DMs do not have PlayerIDs so store their name in another field.
            if (sender.IsDM)
                senderDMName = "[DM: " + sender.Name + " (" + senderCDKey + ")]";
            else
                senderPlayerID = sender.GlobalID;

            // Receiver - may or may not have the data.

            string receiverCDKey = null;
            string receiverAccountName = null;
            string receiverPlayerID = null;
            string receiverDMName = null;

            if (recipient.IsValid)
            {
                receiverCDKey =  _.GetPCPublicCDKey(recipient.Object);
                receiverAccountName = recipient.Name;

                // DMs do not have PlayerIDs so store their name in another field.
                if (recipient.IsDM)
                    receiverDMName = "[DM: " + recipient.Name + " (" + senderCDKey + ")]";
                else
                    receiverPlayerID = recipient.GlobalID;
            }

            ChatLog entity = new ChatLog
            {
                Message = text,
                SenderCDKey = senderCDKey,
                SenderAccountName = senderAccountName,
                SenderPlayerID = senderPlayerID,
                SenderDMName = senderDMName,
                ReceiverCDKey = receiverCDKey,
                ReceiverAccountName = receiverAccountName,
                ReceiverPlayerID = receiverPlayerID,
                ReceiverDMName = receiverDMName,
                ChatChannelID = channelEntity.ChatChannelID,
                DateSent = DateTime.UtcNow
            };
            
            _db.ChatLogs.Add(entity);
            _db.SaveChanges();

        }

    }
}
