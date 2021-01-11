using System;
using System.Globalization;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Service;
using Player = SWLOR.Game.Server.Entity.Player;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public class RoleplayXP
    {
        private const string RPTimestampVariable = "RP_SYSTEM_LAST_MESSAGE_TIMESTAMP";

        /// <summary>
        /// Once every 30 minutes, the RP system will check all players and distribute RP XP if applicable.
        /// </summary>
        [NWNEventHandler("mod_heartbeat")]
        public static void DistributeRoleplayXP()
        {
            var module = GetModule();
            var ticks = GetLocalInt(module, "RP_SYSTEM_TICKS") + 1;

            // Is it time to process RP points?
            if (ticks >= 300) // 300 ticks * 6 seconds per HB = 1800 seconds = 30 minutes
            {
                var player = GetFirstPC();
                while (GetIsObjectValid(player))
                {
                    ProcessPlayerRoleplayXP(player);

                    player = GetNextPC();
                }

                ticks = 0;
            }

            SetLocalInt(module, "RP_SYSTEM_TICKS", ticks);
        }

        /// <summary>
        /// Checks to see if it's time to distribute RP XP to a player.
        /// If it is, XP will be sent to their UnallocatedRPXP property for later distribution via the skills menu.
        /// </summary>
        /// <param name="player">The player to process</param>
        private static void ProcessPlayerRoleplayXP(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerID = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerID) ?? new Player();

            if (dbPlayer.RoleplayProgress.RPPoints >= 50)
            {
                const int BaseXP = 1500;
                int delta = dbPlayer.RoleplayProgress.RPPoints - 50;
                int bonusXP = delta * 25;
                int xp = BaseXP + bonusXP;

                dbPlayer.RoleplayProgress.RPPoints = 0;
                dbPlayer.RoleplayProgress.TotalRPExpGained += (ulong)xp;
                dbPlayer.UnallocatedXP += xp;
                DB.Set(playerID, dbPlayer);
                
                SendMessageToPC(player, $"You gained {xp} roleplay XP.");
            }
        }

        /// <summary>
        /// Adds RP points to a player's RP progression.
        /// If messages are sent too quickly, the message will be treated as spam and RP point will not be granted.
        /// </summary>
        [NWNEventHandler("on_nwnx_chat")]
        public static void ProcessRPMessage()
        {
            var channel = Chat.GetChannel();
            var player = Chat.GetSender();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var message = Chat.GetMessage().Trim();
            var now = DateTime.UtcNow;

            var isInCharacterChat =
                channel == ChatChannel.PlayerTalk ||
                channel == ChatChannel.PlayerWhisper ||
                channel == ChatChannel.PlayerParty ||
                channel == ChatChannel.PlayerShout;

            // Don't care about other chat channels.
            if (!isInCharacterChat) return;

            // Is the message too short?
            if (message.Length <= 3) return;

            // Is this an OOC message?
            var startingText = message.Substring(0, 2);
            if (startingText == "//" || startingText == "((") return;

            var playerID = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerID);

            // Spam prevention
            var timestampString = GetLocalString(player, RPTimestampVariable);
            SetLocalString(player, RPTimestampVariable, now.ToString(CultureInfo.InvariantCulture));

            // If there was a timestamp then we'll check for spam and prevent it from counting towards
            // the RP XP points.
            if (!string.IsNullOrWhiteSpace(timestampString))
            {
                var lastSend = DateTime.Parse(timestampString);
                if (now <= lastSend.AddSeconds(1))
                {
                    dbPlayer.RoleplayProgress.SpamMessageCount++;
                    DB.Set(playerID, dbPlayer);
                    return;
                }
            }

            // Check if players are close enough for the channel in which the message was sent.
            if (!CanReceiveRPPoint(player, channel)) return;

            dbPlayer.RoleplayProgress.RPPoints++;
            DB.Set(playerID, dbPlayer);
        }

        /// <summary>
        /// Determines whether a player can receive an additional RP point.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="channel">The chat channel used.</param>
        /// <returns>true if the player can receive an RP point, false otherwise</returns>
        private static bool CanReceiveRPPoint(uint player, ChatChannel channel)
        {
            var playerId = GetObjectUUID(player);

            // Party - Must be in a party with another PC.
            if (channel == ChatChannel.PlayerParty)
            {
                for (var member = GetFirstFactionMember(player); GetIsObjectValid(member); member = GetNextFactionMember(player))
                {
                    if (GetObjectUUID(member) == playerId) continue;
                    return true;
                }
                
                return false;
            }

            for (var currentPlayer = GetFirstPC(); GetIsObjectValid(currentPlayer); currentPlayer = GetNextPC())
            {
                float distance;
                if (channel == ChatChannel.PlayerTalk)
                {
                    distance = 20.0f;
                }
                else if (channel == ChatChannel.PlayerWhisper)
                {
                    distance = 4.0f;
                }
                else break;
                
                if (GetDistanceBetween(player, currentPlayer) <= distance)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
