using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class Party
    {
        private static readonly Dictionary<Guid, List<uint>> _parties = new Dictionary<Guid, List<uint>>();
        private static readonly Dictionary<uint, Guid> _playerToParty = new Dictionary<uint, Guid>();
        private static readonly Dictionary<Guid, uint> _partyLeaders = new Dictionary<Guid, uint>();

        /// <summary>
        /// When a member of a party accepts an invitation, add them to the caches.
        /// </summary>
        [NWNEventHandler("pty_accept_bef")]
        public static void JoinParty()
        {
            var player = OBJECT_SELF;
            var requester = StringToObject(EventsPlugin.GetEventData("INVITED_BY"));

            // This is a brand new party.
            // Add both the requester and the player to the cache.
            // Mark the requester as the party leader.
            if (!_playerToParty.ContainsKey(requester))
            {
                var partyId = Guid.NewGuid();
                _parties[partyId] = new List<uint>
                {
                    requester,
                    player
                };
                _partyLeaders[partyId] = requester;
                _playerToParty[player] = partyId;
                _playerToParty[requester] = partyId;
            }
            // This is an existing party.
            // Add the player to the party cache.
            else
            {
                var partyId = _playerToParty[requester];
                _parties[partyId].Add(player);
                _playerToParty[player] = partyId;
            }
        }

        /// <summary>
        /// When a member of a party leaves, update the caches.
        /// </summary>
        [NWNEventHandler("pty_leave_bef")]
        public static void LeaveParty()
        {
            var player = StringToObject(EventsPlugin.GetEventData("LEAVING"));
            RemovePlayerFromParty(player);
        }

        /// <summary>
        /// When the leader of a party changes, update the caches.
        /// </summary>
        [NWNEventHandler("pty_chgldr_bef")]
        public static void TransferLeadership()
        {
            var player = StringToObject(EventsPlugin.GetEventData("NEW_LEADER"));
            var partyId = _playerToParty[player];
            _partyLeaders[partyId] = player;
        }

        /// <summary>
        /// When a player leaves the server, remove them from the party caches.
        /// </summary>
        [NWNEventHandler("mod_exit")]
        public static void LeaveServer()
        {
            var player = GetExitingObject();
            RemovePlayerFromParty(player);
        }

        /// <summary>
        /// Removes a player from a party.
        /// If this would lead to an empty party, or a party with one member, the party gets disbanded.
        /// Otherwise if
        /// </summary>
        /// <param name="player"></param>
        private static void RemovePlayerFromParty(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (!_playerToParty.ContainsKey(player)) return;
            
            var partyId = _playerToParty[player];

            // Remove this player from the caches.
            _parties[partyId].Remove(player);
            _playerToParty.Remove(player);

            // If there is now only one party member (or fewer)
            // Party needs to be disbanded and caches updated.
            if (_parties[partyId].Count <= 1)
            {
                foreach (var member in _parties[partyId])
                {
                    _playerToParty.Remove(member);
                }

                _parties.Remove(partyId);
                _partyLeaders.Remove(partyId);
                return;
            }

            // The party is still valid but the player who left was its leader. 
            // Swap leadership to the next person in the party list.
            player = _parties[partyId].First();
            if (_partyLeaders[partyId] == player)
            {
                _partyLeaders[partyId] = player;
            }
        }

        /// <summary>
        /// Retrieves all of the members in a player's party.
        /// </summary>
        /// <param name="player">The player to check.</param>
        /// <returns>A list of party members.</returns>
        public static List<uint> GetAllPartyMembers(uint player)
        {
            // Player isn't in a party. Simply return them in a list.
            if(!_playerToParty.ContainsKey(player))
            {
                return new List<uint>
                {
                    player
                };
            }

            var partyId = _playerToParty[player];
            var members = _parties[partyId];
            return members.ToList();
        }

        /// <summary>
        /// Retrieves all of the members in a player's party who are within the specified range from player.
        /// </summary>
        /// <param name="player">The player to check and use as a distance check.</param>
        /// <param name="distance">The amount of distance to use.</param>
        /// <returns>A list of party members within the specified distance.</returns>
        public static List<uint> GetAllPartyMembersWithinRange(uint player, float distance)
        {
            if (distance <= 0.0f) distance = 0.0f;
            var members = GetAllPartyMembers(player);

            var result = new List<uint>();
            foreach (var member in members)
            {
                // Not in the same area
                if (GetArea(member) != GetArea(player)) continue;

                // This is the player we're checking. They should be included.
                if (member == player)
                {
                    result.Add(member);
                    continue;
                }

                // Distance is too great.
                if (GetDistanceBetween(member, player) > distance) continue;

                result.Add(player);
            }

            return result;
        }
    }
}
