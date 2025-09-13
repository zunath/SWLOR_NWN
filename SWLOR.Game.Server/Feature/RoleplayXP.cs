using System;
using System.Globalization;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.NWN.API.NWScript.Enum;
using Player = SWLOR.Game.Server.Entity.Player;
using ChatChannel = SWLOR.Game.Server.Core.NWNX.Enum.ChatChannel;

namespace SWLOR.Game.Server.Feature
{
    public class RoleplayXP
    {
        private const string RPTimestampVariable = "RP_SYSTEM_LAST_MESSAGE_TIMESTAMP";

        /// <summary>
        /// Once every 30 minutes, the RP system will check all players and distribute RP XP if applicable.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public static void DistributeRoleplayXP()
        {
            const string TrackerVariableName = "RP_SYSTEM_TICKS";
            var player = OBJECT_SELF;
            var ticks = GetLocalInt(player, TrackerVariableName) + 1;

            // Is it time to process RP points?
            if (ticks >= 300) // 300 ticks * 6 seconds per HB = 1800 seconds = 30 minutes
            {
                ProcessPlayerRoleplayXP(player);
                ticks = 0;
            }

            SetLocalInt(player, TrackerVariableName, ticks);
        }

        /// <summary>
        /// Checks to see if it's time to distribute RP XP to a player.
        /// If it is, XP will be sent to their UnallocatedRPXP property for later distribution via the skills menu.
        /// </summary>
        /// <param name="player">The player to process</param>
        private static void ProcessPlayerRoleplayXP(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId) ?? new Player(playerId);

            if (dbPlayer.RoleplayProgress.RPPoints >= 50)
            {
                var socialModifier = GetAbilityModifier(AbilityType.Social, player);
                const int BaseXP = 1500;
                var delta = dbPlayer.RoleplayProgress.RPPoints - 50;
                var bonusXP = delta * 25;
                var xp = BaseXP + bonusXP + socialModifier * (BaseXP / 4);
                var cantinaBonus = Property.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.CantinaLevel);
                xp += (int)(BaseXP * (cantinaBonus * 0.05f));

                dbPlayer.RoleplayProgress.RPPoints = 0;
                dbPlayer.RoleplayProgress.TotalRPExpGained += (ulong)xp;
                dbPlayer.UnallocatedXP += xp;
                DB.Set(dbPlayer);
                
                SendMessageToPC(player, $"You gained {xp} roleplay XP.");
            }
        }

        /// <summary>
        /// Adds RP points to a player's RP progression.
        /// If messages are sent too quickly, the message will be treated as spam and RP point will not be granted.
        /// </summary>
        [NWNEventHandler(ScriptName.OnNWNXChat)]
        public static void ProcessRPMessage()
        {
            var channel = ChatPlugin.GetChannel();
            var player = ChatPlugin.GetSender();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) return;

            var message = ChatPlugin.GetMessage().Trim();
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
            SetLocalString(player, RPTimestampVariable, now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));

            // If there was a timestamp then we'll check for spam and prevent it from counting towards
            // the RP XP points.
            if (!string.IsNullOrWhiteSpace(timestampString))
            {
                var lastSend = DateTime.ParseExact(timestampString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                if (now <= lastSend.AddSeconds(1))
                {
                    dbPlayer.RoleplayProgress.SpamMessageCount++;
                    DB.Set(dbPlayer);
                    return;
                }
            }

            // Check if players are close enough for the channel in which the message was sent.
            if (!CanReceiveRPPoint(player, channel)) return;

            dbPlayer.RoleplayProgress.RPPoints++;
            DB.Set(dbPlayer);
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
