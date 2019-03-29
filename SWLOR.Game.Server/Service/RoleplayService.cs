using System;
using System.Globalization;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.Messaging.Messages;
using SWLOR.Game.Server.NWN.Events.Module;
using SWLOR.Game.Server.NWNX;
namespace SWLOR.Game.Server.Service
{
    public static class RoleplayService
    {
        private static readonly ChatChannelType[] ValidChannels =
        {
            ChatChannelType.PlayerParty,
            ChatChannelType.PlayerShout,
            ChatChannelType.PlayerTalk,
            ChatChannelType.PlayerWhisper
        };

        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<ChatProcessedMessage>(OnChatProcessed);
            MessageHub.Instance.Subscribe<OnModuleHeartbeat>(message => OnModuleHeartbeat());
        }

        private static void OnChatProcessed(ChatProcessedMessage message)
        {
            var sender = message.Sender;
            var channel = message.Channel;

            // Must be one of the valid channels
            if (!ValidChannels.Contains(message.Channel)) return;

            // Must be a player
            if (!message.Sender.IsPlayer) return;

            // Must not be an OOC message
            if (message.IsOutOfCharacter) return;

            // Update the local timestamp variable on the sender.
            DateTime now = DateTime.UtcNow;
            sender.SetLocalString("RP_SYSTEM_LAST_MESSAGE_TIMESTAMP", now.ToString(CultureInfo.InvariantCulture));

            // Very rudimentary spam protection.
            DateTime lastSend = DateTime.Parse(sender.GetLocalString("RP_SYSTEM_LAST_MESSAGE_TIMESTAMP"));
            if (now <= lastSend.AddSeconds(1))
            {
                Console.WriteLine("Spam preventing firing");
                return;
            }
            
            // Validate whether player should receive an RP Point
            bool canReceivePoint = CanReceiveRPPoint(sender.Object, channel);
            if (!canReceivePoint) return;
            
            // Player was allowed to gain this RP point.
            var dbPlayer = DataService.Get<Player>(sender.GlobalID);
            dbPlayer.RoleplayPoints++;
            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);

            Console.WriteLine("RP Points = " + dbPlayer.RoleplayPoints);
        }

        private static void OnModuleHeartbeat()
        {
            var module = NWModule.Get();
            var ticks = module.GetLocalInt("RP_SYSTEM_TICKS") + 1;

            // Is it time to process RP points?
            if (ticks >= 300) // 300 ticks * 6 seconds per HB = 1800 seconds = 30 minutes
            {
                foreach (var player in module.Players)
                {
                    ProcessPlayerRoleplayXP(player);
                }

                ticks = 0;
            }

            module.SetLocalInt("RP_SYSTEM_TICKS", ticks);
        }

        private static void ProcessPlayerRoleplayXP(NWPlayer player)
        {
            // Only fire for players, not DMs.
            if (!player.IsPlayer) return;

            var dbPlayer = DataService.Get<Player>(player.GlobalID);
            if (dbPlayer.RoleplayPoints >= 150)
            {
                float residencyBonus = PlayerStatService.EffectiveResidencyBonus(player);
                const int BaseXP = 500;
                int xp = (int)(BaseXP + BaseXP * residencyBonus);
                dbPlayer.RoleplayXP += xp;
                dbPlayer.RoleplayPoints = 0;
                DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);

                player.SendMessage("You gained " + xp + " roleplay XP.");
            }
        }

        private static bool CanReceiveRPPoint(NWPlayer player, ChatChannelType channel)
        {
            // Party - Must be in a party with another PC.
            if (channel == ChatChannelType.PlayerParty)
            {
                return player.PartyMembers.Any(x => x.GlobalID != player.GlobalID);
            }

            // Shout (Holonet) - Another player must be online.
            else if (channel == ChatChannelType.PlayerShout)
            {
                return NWModule.Get().Players.Count() > 1;
            }

            // Talk - Another player must be nearby. (20.0 units)
            else if(channel == ChatChannelType.PlayerTalk)
            {
                return NWModule.Get().Players.Any(nearby => 
                    player.GlobalID != nearby.GlobalID &&
                    _.GetDistanceBetween(player, nearby) <= 20.0f);
            }
            
            // Whisper - Another player must be nearby. (4.0 units)
            else if (channel == ChatChannelType.PlayerWhisper)
            {
                return NWModule.Get().Players.Any(nearby => 
                    player.GlobalID != nearby.GlobalID &&
                    _.GetDistanceBetween(player, nearby) <= 4.0f);
            }

            return false;
        }
    }
}
