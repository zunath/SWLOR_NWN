using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.DM;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;

using SWLOR.Game.Server.ValueObject;
using ChatChannel = SWLOR.Game.Server.NWNX.ChatChannel;
using PCBaseType = SWLOR.Game.Server.Enumeration.PCBaseType;

namespace SWLOR.Game.Server.Service
{
    public static class ModuleLoggingService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnDMAction>(message => OnDMAction(message.ActionID));
            MessageHub.Instance.Subscribe<OnModuleEnter>(message => OnModuleEnter());
            MessageHub.Instance.Subscribe<OnModuleLeave>(message => OnModuleLeave());
            MessageHub.Instance.Subscribe<OnModuleNWNXChat>(message => OnModuleNWNXChat());
            MessageHub.Instance.Subscribe<OnModuleRespawn>(message => OnModuleRespawn());
            MessageHub.Instance.Subscribe<OnModuleDeath>(message => OnModuleDeath());
            MessageHub.Instance.Subscribe<OnStoreBankItem>(message => OnStoreBankItem(message.Player, message.Entity));
            MessageHub.Instance.Subscribe<OnRemoveBankItem>(message => OnRemoveBankItem(message.Player, message.Entity));
            MessageHub.Instance.Subscribe<OnStoreStructureItem>(message => OnStoreStructureItem(message.Player, message.Entity));
            MessageHub.Instance.Subscribe<OnRemoveStructureItem>(message => OnRemoveStructureItem(message.Player, message.Entity));
            MessageHub.Instance.Subscribe<OnPurchaseLand>(OnPurchaseLand);
            MessageHub.Instance.Subscribe<OnBaseLeaseExpired>(message => OnPCBaseLeaseExpired(message.PCBase));
            MessageHub.Instance.Subscribe<OnBaseDestroyed>(message => OnPCBaseDestroyed(message.PCBase, message.LastAttacker));
            MessageHub.Instance.Subscribe<OnBaseLeaseCancelled>(message => OnPCBaseLeaseCanceled(message.PCBase));
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
            
            ModuleEvent entity = new ModuleEvent
            {
                AccountName = account,
                CDKey = cdKey,
                ModuleEventTypeID = 1,
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

            ModuleEvent entity = new ModuleEvent
            {
                AccountName = account,
                CDKey = cdKey,
                ModuleEventTypeID = 2,
                PlayerID = oPC.IsDM ? null : (Guid?)oPC.GlobalID,
                DateOfEvent = now
            };

            // Bypass the caching logic.
            DataService.DataQueue.Enqueue(new DatabaseAction(entity, DatabaseActionType.Insert));
        }


        private static int ConvertNWNXChatChannelIDToDatabaseID(ChatChannel nwnxChatChannelID)
        {
            switch (nwnxChatChannelID)
            {
                case ChatChannel.PlayerTalk:
                case ChatChannel.DMTalk:
                    return 3;
                case ChatChannel.PlayerShout:
                case ChatChannel.DMShout:
                    return 1;
                case ChatChannel.PlayerWhisper:
                case ChatChannel.DMWhisper:
                    return 2;
                case ChatChannel.PlayerTell:
                case ChatChannel.DMTell:
                    return 6;
                case ChatChannel.ServerMessage:
                    return 7;
                case ChatChannel.PlayerParty:
                case ChatChannel.DMParty:
                    return 4;
                default:
                    return 5;
            }
        }
        
        private static void OnModuleNWNXChat()
        {
            NWPlayer sender = _.OBJECT_SELF;
            if (!sender.IsPlayer && !sender.IsDM) return;
            string text = NWNXChat.GetMessage();
            if (string.IsNullOrWhiteSpace(text)) return;

            var mode = NWNXChat.GetChannel();
            int channel = ConvertNWNXChatChannelIDToDatabaseID(mode);
            NWObject recipient = NWNXChat.GetTarget();
            var channelEntity = DataService.ChatChannel.GetByID(channel);

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

            NWObject dm = _.OBJECT_SELF;

            var record = new DMAction
            {
                DMActionTypeID = actionTypeID,
                Name = dm.Name,
                CDKey = _.GetPCPublicCDKey(dm),
                Details = details
            };

            // Bypass the caching logic
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
                    var area = NWNXObject.StringToObject(NWNXEvents.GetEventData("AREA"));
                    string areaName = _.GetName(area);
                    NWCreature creature = NWNXObject.StringToObject(NWNXEvents.GetEventData("OBJECT"));
                    int objectTypeID = Convert.ToInt32(NWNXEvents.GetEventData("OBJECT_TYPE"));
                    float x = (float)Convert.ToDouble(NWNXEvents.GetEventData("POS_X"));
                    float y = (float)Convert.ToDouble(NWNXEvents.GetEventData("POS_Y"));
                    float z = (float)Convert.ToDouble(NWNXEvents.GetEventData("POS_Z"));
                    creature.SetLocalInt("DM_SPAWNED", _.TRUE);
                    details = areaName + "," + creature.Name + "," + objectTypeID + "," + x + "," + y + "," + z;
                    break;
                case 22: // Give XP
                    amount = Convert.ToInt32(NWNXEvents.GetEventData("AMOUNT"));
                    target = NWNXObject.StringToObject(NWNXEvents.GetEventData("OBJECT"));
                    details = amount + "," + target.Name;
                    break;
                case 23: // Give Level
                    amount = Convert.ToInt32(NWNXEvents.GetEventData("AMOUNT"));
                    target = NWNXObject.StringToObject(NWNXEvents.GetEventData("OBJECT"));
                    details = amount + "," + target.Name;
                    break;
                case 24: // Give Gold
                    amount = Convert.ToInt32(NWNXEvents.GetEventData("AMOUNT"));
                    target = NWNXObject.StringToObject(NWNXEvents.GetEventData("OBJECT"));
                    details = amount + "," + target.Name;
                    break;
            }

            return details;
        }

        private static void OnModuleDeath()
        {
            NWPlayer player = _.GetLastPlayerDied();
            var @event = new ModuleEvent
            {
                ModuleEventTypeID = 3,
                PlayerID = player.GlobalID,
                CDKey = _.GetPCPublicCDKey(player),
                AccountName = _.GetPCPlayerName(player),
            };

            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(@event, DatabaseActionType.Insert));
        }

        private static void OnModuleRespawn()
        {
            NWPlayer player = _.GetLastRespawnButtonPresser();
            var @event = new ModuleEvent
            {
                ModuleEventTypeID = 4,
                PlayerID = player.GlobalID,
                CDKey = _.GetPCPublicCDKey(player),
                AccountName = _.GetPCPlayerName(player),
            };

            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(@event, DatabaseActionType.Insert));
        }

        private static void OnStoreBankItem(NWPlayer player, BankItem entity)
        {
            var @event = new ModuleEvent
            {
                ModuleEventTypeID = 5,
                PlayerID = entity.PlayerID,
                CDKey = _.GetPCPublicCDKey(player),
                AccountName = _.GetPCPlayerName(player),
                BankID = entity.BankID,
                ItemID = new Guid(entity.ItemID),
                ItemName = entity.ItemName,
                ItemTag = entity.ItemTag,
                ItemResref = entity.ItemResref
            };

            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(@event, DatabaseActionType.Insert));
        }

        private static void OnRemoveBankItem(NWPlayer player, BankItem entity)
        {
            var @event = new ModuleEvent
            {
                ModuleEventTypeID = 6,
                PlayerID = entity.PlayerID,
                CDKey = _.GetPCPublicCDKey(player),
                AccountName = _.GetPCPlayerName(player),
                BankID = entity.BankID,
                ItemID = new Guid(entity.ItemID),
                ItemName = entity.ItemName,
                ItemTag = entity.ItemTag,
                ItemResref = entity.ItemResref
            };

            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(@event, DatabaseActionType.Insert));
        }

        private static void OnStoreStructureItem(NWPlayer player, PCBaseStructureItem entity)
        {
            PCBaseStructure pcBaseStructure = DataService.PCBaseStructure.GetByID(entity.PCBaseStructureID);

            var @event = new ModuleEvent
            {
                ModuleEventTypeID = 7,
                PlayerID = player.GlobalID,
                CDKey = _.GetPCPublicCDKey(player),
                AccountName = _.GetPCPlayerName(player),
                ItemID = new Guid(entity.ItemGlobalID),
                ItemName = entity.ItemName,
                ItemTag = entity.ItemTag,
                ItemResref = entity.ItemResref,
                PCBaseID = pcBaseStructure.PCBaseID,
                PCBaseStructureID = entity.PCBaseStructureID,
                BaseStructureID = pcBaseStructure.BaseStructureID,
                CustomName = pcBaseStructure.CustomName
            };

            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(@event, DatabaseActionType.Insert));
        }

        private static void OnRemoveStructureItem(NWPlayer player, PCBaseStructureItem entity)
        {
            PCBaseStructure pcBaseStructure = DataService.PCBaseStructure.GetByID(entity.PCBaseStructureID);

            var @event = new ModuleEvent
            {
                ModuleEventTypeID = 8,
                PlayerID = player.GlobalID,
                CDKey = _.GetPCPublicCDKey(player),
                AccountName = _.GetPCPlayerName(player),
                ItemID = new Guid(entity.ItemGlobalID),
                ItemName = entity.ItemName,
                ItemTag = entity.ItemTag,
                ItemResref = entity.ItemResref,
                PCBaseID = pcBaseStructure.PCBaseID,
                PCBaseStructureID = entity.PCBaseStructureID,
                BaseStructureID = pcBaseStructure.BaseStructureID,
                CustomName = pcBaseStructure.CustomName
            };

            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(@event, DatabaseActionType.Insert));
        }

        private static void OnPurchaseLand(OnPurchaseLand message)
        {
            var @event = new ModuleEvent
            {
                ModuleEventTypeID = 9,
                PlayerID = message.Player.GlobalID,
                CDKey = _.GetPCPublicCDKey(message.Player),
                AccountName = _.GetPCPlayerName(message.Player),
                AreaSector = message.Sector,
                AreaName = message.AreaName,
                AreaTag = message.AreaTag,
                AreaResref = message.AreaResref,
                PCBaseTypeID = message.PCBaseType
            };

            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(@event, DatabaseActionType.Insert));
        }

        private static void OnPCBaseLeaseExpired(PCBase pcBase)
        {
            Area dbArea = DataService.Area.GetByResref(pcBase.AreaResref);

            var @event = new ModuleEvent
            {
                ModuleEventTypeID = 10,
                PlayerID = pcBase.PlayerID,
                AreaSector = pcBase.Sector,
                AreaName = dbArea.Name,
                AreaTag = dbArea.Tag,
                AreaResref = pcBase.AreaResref,
                PCBaseTypeID = (PCBaseType)pcBase.PCBaseTypeID, 
                CustomName = pcBase.CustomName,
                DateRentDue = pcBase.DateRentDue
            };

            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(@event, DatabaseActionType.Insert));
        }

        private static void OnPCBaseDestroyed(PCBase pcBase, NWCreature lastAttacker)
        {
            Area dbArea = DataService.Area.GetByResref(pcBase.AreaResref);

            var @event = new ModuleEvent
            {
                ModuleEventTypeID = 11,
                PlayerID = pcBase.PlayerID,
                AreaSector = pcBase.Sector,
                AreaName = dbArea.Name,
                AreaTag = dbArea.Tag,
                AreaResref = pcBase.AreaResref,
                PCBaseTypeID = (PCBaseType)pcBase.PCBaseTypeID,
                CustomName = pcBase.CustomName,
                DateRentDue = pcBase.DateRentDue,
                AttackerPlayerID = lastAttacker.IsPlayer ? (Guid?)lastAttacker.GlobalID : null
            };

            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(@event, DatabaseActionType.Insert));
        }

        private static void OnPCBaseLeaseCanceled(PCBase pcBase)
        {
            Area dbArea = DataService.Area.GetByResref(pcBase.AreaResref);

            var @event = new ModuleEvent
            {
                ModuleEventTypeID = 12,
                PlayerID = pcBase.PlayerID,
                AreaSector = pcBase.Sector,
                AreaName = dbArea.Name,
                AreaTag = dbArea.Tag,
                AreaResref = pcBase.AreaResref,
                PCBaseTypeID = (PCBaseType)pcBase.PCBaseTypeID,
                CustomName = pcBase.CustomName,
                DateRentDue = pcBase.DateRentDue,
            };

            // Bypass the caching logic
            DataService.DataQueue.Enqueue(new DatabaseAction(@event, DatabaseActionType.Insert));
        }
    }
}
