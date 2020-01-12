using System;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.DM;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Logging;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript;
using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;

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
            NWPlayer oPC = (GetEnteringObject());
            string name = oPC.Name;
            string cdKey = GetPCPublicCDKey(oPC.Object);
            string account = GetPCPlayerName(oPC.Object);
            DateTime now = DateTime.UtcNow;
            string nowString = now.ToString("yyyy-MM-dd hh:mm:ss");

            // CD Key and accounts are stored as local strings on the PC
            // because they cannot be retrieved using NWScript functions
            // on the module OnClientLeave event.

            oPC.SetLocalString("PC_CD_KEY", cdKey);
            oPC.SetLocalString("PC_ACCOUNT", account);

            var details = nowString + ": " + name + " (" + account + "/" + cdKey + ") connected to the server.";
            Audit.Write(AuditGroup.Connection, details);
            Console.WriteLine(details);
        }

        private static void OnModuleLeave()
        {
            NWPlayer oPC = (GetExitingObject());
            string name = oPC.Name;
            string cdKey = oPC.GetLocalString("PC_CD_KEY");
            string account = oPC.GetLocalString("PC_ACCOUNT");
            DateTime now = DateTime.UtcNow;
            string nowString = now.ToString("yyyy-MM-dd hh:mm:ss");

            var details = nowString + ": " + name + " (" + account + "/" + cdKey + ") left the server.";
            Audit.Write(AuditGroup.Connection, details);
            Console.WriteLine(details);
        }


        private static int ConvertNWNXChatChannelIDToDatabaseID(NWNXChatChannel nwnxChatChannelID)
        {
            switch (nwnxChatChannelID)
            {
                case NWNXChatChannel.PlayerTalk:
                case NWNXChatChannel.DMTalk:
                    return 3;
                case NWNXChatChannel.PlayerShout:
                case NWNXChatChannel.DMShout:
                    return 1;
                case NWNXChatChannel.PlayerWhisper:
                case NWNXChatChannel.DMWhisper:
                    return 2;
                case NWNXChatChannel.PlayerTell:
                case NWNXChatChannel.DMTell:
                    return 6;
                case NWNXChatChannel.ServerMessage:
                    return 7;
                case NWNXChatChannel.PlayerParty:
                case NWNXChatChannel.DMParty:
                    return 4;
                default:
                    return 5;
            }
        }
        
        private static void OnModuleNWNXChat()
        {
            NWPlayer sender = NWGameObject.OBJECT_SELF;
            if (!sender.IsPlayer && !sender.IsDM) return;
            string text = NWNXChat.GetMessage();
            if (string.IsNullOrWhiteSpace(text)) return;

            var mode = NWNXChat.GetChannel();
            int channel = ConvertNWNXChatChannelIDToDatabaseID(mode);

            string log;
            // We don't log server messages because there isn't a good way to filter them.
            if (channel == (int)NWNXChatChannel.ServerMessage) return;

            if (channel == (int)NWNXChatChannel.DMTell ||
                channel == (int)NWNXChatChannel.PlayerTell)
            {
                log = BuildTellLog();
            }
            else
            {
                log = BuildRegularChatLog();
            }

            Audit.Write(AuditGroup.Chat, log);

        }

        private static string BuildRegularChatLog()
        {
            var sender = NWNXChat.GetSender();
            var channel = NWNXChat.GetChannel();
            var message = NWNXChat.GetMessage();
            var ipAddress = GetPCIPAddress(sender);
            var cdKey = GetPCPublicCDKey(sender);
            var account = GetPCPlayerName(sender);
            var pcName = GetName(sender);

            var log = $"{pcName} - {account} - {cdKey} - {ipAddress} - {channel}: {message}";

            return log;
        }

        private static string BuildTellLog()
        {
            var sender = NWNXChat.GetSender();
            var receiver = NWNXChat.GetTarget();
            var channel = NWNXChat.GetChannel();
            var message = NWNXChat.GetMessage();
            var senderIPAddress = GetPCIPAddress(sender);
            var senderCDKey = GetPCPublicCDKey(sender);
            var senderAccount = GetPCPlayerName(sender);
            var senderPCName = GetName(sender);
            var receiverIPAddress = GetPCIPAddress(receiver);
            var receiverCDKey = GetPCPublicCDKey(receiver);
            var receiverAccount = GetPCPlayerName(receiver);
            var receiverPCName = GetName(receiver);

            var log = $"{senderPCName} - {senderAccount} - {senderCDKey} - {senderIPAddress} - {channel} (SENT TO {receiverPCName} - {receiverAccount} - {receiverCDKey} - {receiverIPAddress}): {message}";
            return log;
        }

        private static void OnDMAction(int actionTypeID)
        {
            string details = ProcessEventAndBuildDetails(actionTypeID);
            Audit.Write(AuditGroup.DM, details);
        }

        private static string ProcessEventAndBuildDetails(int eventID)
        {
            string details = string.Empty;
            NWObject target;
            int amount;

            switch (eventID)
            {
                case 1: // Spawn Creature
                    string areaName = _.GetName(NWNXEvents.OnDMSpawnObject_GetArea());
                    NWCreature creature = NWNXEvents.OnDMSpawnObject_GetObject();
                    var objectTypeID = NWNXEvents.OnDMSpawnObject_GetObjectType();
                    float x = NWNXEvents.OnDMSpawnObject_GetPositionX();
                    float y = NWNXEvents.OnDMSpawnObject_GetPositionY();
                    float z = NWNXEvents.OnDMSpawnObject_GetPositionZ();
                    creature.SetLocalBoolean("DM_SPAWNED", true);
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

        private static void OnModuleDeath()
        {
            NWPlayer player = GetLastPlayerDied();
            var details = $"DEATH - {GetPCPublicCDKey(player)} - {GetPCPlayerName(player)} - {player.Name}";
            Audit.Write(AuditGroup.Death, details);
        }

        private static void OnModuleRespawn()
        {
            NWPlayer player = GetLastRespawnButtonPresser();
            var details = $"RESPAWN - {GetPCPublicCDKey(player)} - {GetPCPlayerName(player)} - {player.Name}";
            Audit.Write(AuditGroup.Death, details);
        }

        private static void OnStoreBankItem(NWPlayer player, BankItem entity)
        {
            var details = $"STORE ITEM - {GetPCPublicCDKey(player)} - {GetPCPlayerName(player)} - Bank #{entity.BankID} - {player.Name} - Item ID {entity.ItemID} - Item Tag {entity.ItemTag} - Item Resref {entity.ItemResref}";
            Audit.Write(AuditGroup.Bank, details);
        }

        private static void OnRemoveBankItem(NWPlayer player, BankItem entity)
        {
            var details = $"REMOVE ITEM - {GetPCPublicCDKey(player)} - {GetPCPlayerName(player)} - Bank #{entity.BankID} - {player.Name} - Item ID {entity.ItemID} - Item Tag {entity.ItemTag} - Item Resref {entity.ItemResref}";
            Audit.Write(AuditGroup.Bank, details);
        }

        private static void OnStoreStructureItem(NWPlayer player, PCBaseStructureItem entity)
        {
            var details = $"STORE ITEM - {GetPCPublicCDKey(player)} - {GetPCPlayerName(player)} - {player.Name} - Item Tag {entity.ItemTag} - Item Resref {entity.ItemResref}";
            Audit.Write(AuditGroup.StructureStorage, details);
        }

        private static void OnRemoveStructureItem(NWPlayer player, PCBaseStructureItem entity)
        {
            var details = $"REMOVE ITEM - {GetPCPublicCDKey(player)} - {GetPCPlayerName(player)} - {player.Name} - Item Tag {entity.ItemTag} - Item Resref {entity.ItemResref}";
            Audit.Write(AuditGroup.StructureStorage, details);
        }

        private static void OnPurchaseLand(OnPurchaseLand message)
        {
            var details = $"PURCHASE LAND - {message.Player.GlobalID} - {GetPCPublicCDKey(message.Player)} - {GetPCPlayerName(message.Player)} - Sector {message.Sector} - Area {message.AreaName} ({message.AreaTag}) [{message.AreaResref}] PCBaseTypeID {message.PCBaseType}";
            Audit.Write(AuditGroup.Territory, details);
        }

        private static void OnPCBaseLeaseExpired(PCBase pcBase)
        {
            Area dbArea = DataService.Area.GetByResref(pcBase.AreaResref);

            var details = $"LEASE EXPIRED - {pcBase.PlayerID} - Sector {pcBase.Sector} - Area {dbArea.Name} ({dbArea.Tag}) [{dbArea.Resref}] - PCBaseTypeID {pcBase.PCBaseTypeID} - Custom Name {pcBase.CustomName} - Date Rent Due {pcBase.DateRentDue}";
            Audit.Write(AuditGroup.Territory, details);
        }

        private static void OnPCBaseDestroyed(PCBase pcBase, NWCreature lastAttacker)
        {
            Area dbArea = DataService.Area.GetByResref(pcBase.AreaResref);
            var attackerDetails = lastAttacker.IsPlayer ? lastAttacker.GlobalID.ToString() : string.Empty;
            var details = $"BASE DESTROYED - {pcBase.PlayerID} - Sector {pcBase.Sector} - Area {dbArea.Name} ({dbArea.Tag}) [{dbArea.Resref}] - PCBaseTypeID {pcBase.PCBaseTypeID} - Custom Name {pcBase.CustomName} - Date Rent Due {pcBase.DateRentDue} - Attacker {attackerDetails}";
            Audit.Write(AuditGroup.Territory, details);
        }

        private static void OnPCBaseLeaseCanceled(PCBase pcBase)
        {
            Area dbArea = DataService.Area.GetByResref(pcBase.AreaResref);
            var details = $"LEASE CANCELLED - {pcBase.PlayerID} - Sector {pcBase.Sector} - Area {dbArea.Name} ({dbArea.Tag}) [{dbArea.Resref}] - PCBaseTypeID {pcBase.PCBaseTypeID} - Custom Name {pcBase.CustomName} - Date Rent Due {pcBase.DateRentDue}";
            Audit.Write(AuditGroup.Territory, details);
        }
    }
}
