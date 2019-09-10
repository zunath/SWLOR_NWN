using System;
using System.Globalization;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Messaging;
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
            MessageHub.Instance.Subscribe<OnChatProcessed>(OnChatProcessed);
            MessageHub.Instance.Subscribe<OnModuleHeartbeat>(message => OnModuleHeartbeat());
        }

        private static void OnChatProcessed(OnChatProcessed message)
        {
            var sender = message.Sender;
            var channel = message.Channel;

            // Must be one of the valid channels
            if (!ValidChannels.Contains(message.Channel)) return;

            // Must be a player
            if (!message.Sender.IsPlayer) return;

            // Must not be an OOC message
            if (message.IsOutOfCharacter) return;
            
            // Grab the current timestamp
            string timestampString = sender.GetLocalString("RP_SYSTEM_LAST_MESSAGE_TIMESTAMP");

            // Regardless if the message makes it through spam prevention, we want to update the latest timestamp.
            DateTime now = DateTime.UtcNow;
            sender.SetLocalString("RP_SYSTEM_LAST_MESSAGE_TIMESTAMP", now.ToString(CultureInfo.InvariantCulture));

            // If there was a timestamp then we'll check for spam and prevent it from counting towards
            // the RP XP points.
            if (!string.IsNullOrWhiteSpace(timestampString))
            {
                DateTime lastSend = DateTime.Parse(timestampString);
                if (now <= lastSend.AddSeconds(1))
                {
                    Console.WriteLine("Spam preventing firing");
                    return;
                }
            }
            
            // Validate whether player should receive an RP Point
            bool canReceivePoint = CanReceiveRPPoint(sender.Object, channel);
            if (!canReceivePoint) return;
            
            // Player was allowed to gain this RP point.
            var dbPlayer = DataService.Player.GetByID(sender.GlobalID);
            dbPlayer.RoleplayPoints++;
            DataService.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
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

            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            if (dbPlayer.RoleplayPoints >= 50)
            {
                const int BaseXP = 500;
                float residencyBonus = PlayerStatService.EffectiveResidencyBonus(player);
                int xp = (int)(BaseXP + BaseXP * residencyBonus);
                float dmBonusModifier = dbPlayer.XPBonus * 0.01f;
                if (dmBonusModifier > 0.25f)
                    dmBonusModifier = 0.25f;
                xp += (int)(xp * dmBonusModifier);

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
