using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWN.Events.DM;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.NWNX;

using SWLOR.Game.Server.ValueObject;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Service
{
    public static class ActivityLoggingService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnDMAction>(message => OnDMAction(message.ActionID));
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
            MessageHub.Instance.Subscribe<OnModuleLeave>(message => OnModuleLeave());
            MessageHub.Instance.Subscribe<OnModuleNWNXChat>(message => OnModuleNWNXChat());
        }

        private static void OnModuleEnter()
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
                PlayerID = oPC.IsDM ? null : (Guid?)oPC.GlobalID,
                DateOfEvent = now
            };

            // Bypass the caching logic.
            DataService.DataQueue.Enqueue(new DatabaseAction(entity, DatabaseActionType.Insert));
        }

        private static void OnModuleLeave()
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
                PlayerID = oPC.IsDM ? null : (Guid?)oPC.GlobalID,
                DateOfEvent = now
            };

            // Bypass the caching logic.
            DataService.DataQueue.Enqueue(new DatabaseAction(entity, DatabaseActionType.Insert));
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
        
        private static void OnModuleNWNXChat()
        {
            NWPlayer sender = Object.OBJECT_SELF;
            if (!sender.IsPlayer && !sender.IsDM) return;
            string text = NWNXChat.GetMessage();
            if (string.IsNullOrWhiteSpace(text)) return;

            int mode = NWNXChat.GetChannel();
            int channel = ConvertNWNXChatChannelIDToDatabaseID(mode);
            NWObject recipient = NWNXChat.GetTarget();
            ChatChannel channelEntity = DataService.Single<ChatChannel>(x => x.ID == channel);

            // Sender - should always have this data.
            string senderCDKey = _.GetPCPublicCDKey(sender.Object);
            string senderAccountName = sender.Name;
            Guid? senderPlayerID = null;
            string senderDMName = null;

            // DMs do not have PlayerIDs so store their name in another field.
            if (sender.IsDM)
                senderDMName = "[DM: " + sender.Name + " (" + senderCDKey + ")]";
            else
                senderPlayerID = sender.GlobalID;

            // Receiver - may or may not have the data.

            string receiverCDKey = null;
            string receiverAccountName = null;
            Guid? receiverPlayerID = null;
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
                ChatChannelID = channelEntity.ID,
                DateSent = DateTime.UtcNow
            };
            
            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(entity, DatabaseActionType.Insert));
        }

        private static void OnDMAction(int actionTypeID)
        {
            string details = ProcessEventAndBuildDetails(actionTypeID);

            NWObject dm = Object.OBJECT_SELF;

            var record = new DMAction
            {
                DMActionTypeID = actionTypeID,
                Name = dm.Name,
                CDKey = _.GetPCPublicCDKey(dm),
                DateOfAction = DateTime.UtcNow,
                Details = details
            };

            // Don't cache DM actions.
            DataService.DataQueue.Enqueue(new DatabaseAction(record, DatabaseActionType.Insert));
        }

        private static string ProcessEventAndBuildDetails(int eventID)
        {
            string details = string.Empty;
            NWObject target;
            int amount;

            switch (eventID)
            {
                case 1: // Spawn Creature
                    string areaName = NWNXEvents.OnDMSpawnObject_GetArea().Name;
                    NWCreature creature = NWNXEvents.OnDMSpawnObject_GetObject().Object;
                    int objectTypeID = NWNXEvents.OnDMSpawnObject_GetObjectType();
                    float x = NWNXEvents.OnDMSpawnObject_GetPositionX();
                    float y = NWNXEvents.OnDMSpawnObject_GetPositionY();
                    float z = NWNXEvents.OnDMSpawnObject_GetPositionZ();
                    creature.SetLocalInt("DM_SPAWNED", _.TRUE);
                    details = areaName + "," + creature.Name + "," + objectTypeID + "," + x + "," + y + "," + z;
                    break;
                case 22: // Give XP
                    amount = NWNXEvents.OnDMGiveXP_GetAmount();
                    target = NWNXEvents.OnDMGiveXP_GetTarget();
                    details = amount + "," + target.Name;
                    break;
                case 23: // Give Level
                    amount = NWNXEvents.OnDMGiveLevels_GetAmount();
                    target = NWNXEvents.OnDMGiveLevels_GetTarget();
                    details = amount + "," + target.Name;
                    break;
                case 24: // Give Gold
                    amount = NWNXEvents.OnDMGiveGold_GetAmount();
                    target = NWNXEvents.OnDMGiveGold_GetTarget();
                    details = amount + "," + target.Name;
                    break;
            }

            return details;
        }


    }
}
