using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using SWLOR.Component.Communication.Contracts;
using SWLOR.NWN.API.Contracts;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Shared.Domain.Properties.Enums;
using ChatChannelType = SWLOR.NWN.API.NWNX.Enum.ChatChannelType;

namespace SWLOR.Component.Communication.Service
{
    public class RoleplayXPService : IRoleplayXPService
    {
        private readonly IDatabaseService _db;
        private readonly IServiceProvider _serviceProvider;
        private readonly IChatPluginService _chatPlugin;
        private const string RPTimestampVariable = "RP_SYSTEM_LAST_MESSAGE_TIMESTAMP";

        public RoleplayXPService(IDatabaseService db, IServiceProvider serviceProvider, IChatPluginService chatPlugin)
        {
            _db = db;
            _serviceProvider = serviceProvider;
            _chatPlugin = chatPlugin;
        }

        // Lazy-loaded service to break circular dependency
        private IPropertyService PropertyService => _serviceProvider.GetRequiredService<IPropertyService>();

        /// <summary>
        /// Once every 30 minutes, the RP system will check all players and distribute RP XP if applicable.
        /// </summary>
        public void DistributeRoleplayXP()
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
        private void ProcessPlayerRoleplayXP(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);

            if (dbPlayer.RoleplayProgress.RPPoints >= 50)
            {
                var socialModifier = GetAbilityModifier(AbilityType.Social, player);
                const int BaseXP = 1500;
                var delta = dbPlayer.RoleplayProgress.RPPoints - 50;
                var bonusXP = delta * 25;
                var xp = BaseXP + bonusXP + socialModifier * (BaseXP / 4);
                var cantinaBonus = PropertyService.GetEffectiveUpgradeLevel(dbPlayer.CitizenPropertyId, PropertyUpgradeType.CantinaLevel);
                xp += (int)(BaseXP * (cantinaBonus * 0.05f));

                dbPlayer.RoleplayProgress.RPPoints = 0;
                dbPlayer.RoleplayProgress.TotalRPExpGained += (ulong)xp;
                dbPlayer.UnallocatedXP += xp;
                _db.Set(dbPlayer);
                
                SendMessageToPC(player, $"You gained {xp} roleplay XP.");
            }
        }

        /// <summary>
        /// Adds RP points to a player's RP progression.
        /// If messages are sent too quickly, the message will be treated as spam and RP point will not be granted.
        /// </summary>
        public void ProcessRPMessage()
        {
            var channel = _chatPlugin.GetChannel();
            var player = _chatPlugin.GetSender();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) return;

            var message = _chatPlugin.GetMessage().Trim();
            var now = DateTime.UtcNow;

            var isInCharacterChat =
                channel == ChatChannelType.PlayerTalk ||
                channel == ChatChannelType.PlayerWhisper ||
                channel == ChatChannelType.PlayerParty ||
                channel == ChatChannelType.PlayerShout;

            // Don't care about other chat channels.
            if (!isInCharacterChat) return;

            // Is the message too short?
            if (message.Length <= 3) return;

            // Is this an OOC message?
            var startingText = message.Substring(0, 2);
            if (startingText == "//" || startingText == "((") return;

            var playerID = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerID);

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
                    _db.Set(dbPlayer);
                    return;
                }
            }

            // Check if players are close enough for the channel in which the message was sent.
            if (!CanReceiveRPPoint(player, channel)) return;

            dbPlayer.RoleplayProgress.RPPoints++;
            _db.Set(dbPlayer);
        }

        /// <summary>
        /// Determines whether a player can receive an additional RP point.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <param name="channel">The chat channel used.</param>
        /// <returns>true if the player can receive an RP point, false otherwise</returns>
        private static bool CanReceiveRPPoint(uint player, ChatChannelType channel)
        {
            var playerId = GetObjectUUID(player);

            // Party - Must be in a party with another PC.
            if (channel == ChatChannelType.PlayerParty)
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
                if (channel == ChatChannelType.PlayerTalk)
                {
                    distance = 20.0f;
                }
                else if (channel == ChatChannelType.PlayerWhisper)
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
