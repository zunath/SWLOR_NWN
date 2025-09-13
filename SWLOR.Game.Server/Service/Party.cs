using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    public static class Party
    {
        private static readonly Dictionary<Guid, List<uint>> _parties = new();
        private static readonly Dictionary<uint, Guid> _creatureToParty = new();
        private static readonly Dictionary<Guid, uint> _partyLeaders = new();

        /// <summary>
        /// When a member of a party accepts an invitation, add them to the caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPartyAcceptBefore)]
        public static void JoinParty()
        {
            var creature = OBJECT_SELF;
            var requester = StringToObject(EventsPlugin.GetEventData("INVITED_BY"));

            AddToParty(requester, creature);
        }

        private static void AddToParty(uint requester, uint creature)
        {
            // This is a brand new party.
            // Add both the requester and the creature to the cache.
            // Mark the requester as the party leader.
            if (!_creatureToParty.ContainsKey(requester))
            {
                var partyId = Guid.NewGuid();
                _parties[partyId] = new List<uint>
                {
                    requester,
                    creature
                };
                _partyLeaders[partyId] = requester;
                _creatureToParty[creature] = partyId;
                _creatureToParty[requester] = partyId;
            }
            // This is an existing party.
            // Add the creature to the party cache.
            else
            {
                var partyId = _creatureToParty[requester];
                _parties[partyId].Add(creature);
                _creatureToParty[creature] = partyId;
            }
        }

        /// <summary>
        /// When an associate (droid, pet, henchman, etc.) joins a party, add them to the caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAssociateAddBefore)]
        public static void AssociateJoinParty()
        {
            var owner = OBJECT_SELF;
            var associate = StringToObject(EventsPlugin.GetEventData("ASSOCIATE_OBJECT_ID"));

            AddToParty(owner, associate);
        }

        /// <summary>
        /// When an associate (droid, pet, henchman, etc.) is removed from the party or leaves, remove them from the caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAssociateRemoveBefore)]
        public static void AssociateLeaveParty()
        {
            var associate = StringToObject(EventsPlugin.GetEventData("ASSOCIATE_OBJECT_ID"));
            RemoveCreatureFromParty(associate);
        }

        /// <summary>
        /// When a member of a party leaves, update the caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPartyLeaveBefore)]
        public static void LeaveParty()
        {
            var creature = StringToObject(EventsPlugin.GetEventData("LEAVING"));
            RemoveCreatureFromParty(creature);
        }

        /// <summary>
        /// When the leader of a party changes, update the caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPartyChangeLeaderBefore)]
        public static void TransferLeadership()
        {
            var creature = StringToObject(EventsPlugin.GetEventData("NEW_LEADER"));
            var partyId = _creatureToParty[creature];
            _partyLeaders[partyId] = creature;
        }

        /// <summary>
        /// When a player leaves the server, remove them from the party caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void LeaveServer()
        {
            var creature = GetExitingObject();
            RemoveCreatureFromParty(creature);
        }

        /// <summary>
        /// Removes a creature from a party.
        /// If this would lead to an empty party, or a party with one member, the party gets disbanded.
        /// Otherwise if the leader leaves, a new one is assigned.
        /// </summary>
        /// <param name="creature">The creature being removed from the party.</param>
        private static void RemoveCreatureFromParty(uint creature)
        {
            if (!_creatureToParty.ContainsKey(creature)) return;
            
            var partyId = _creatureToParty[creature];

            // Remove this creature from the caches.
            _parties[partyId].Remove(creature);
            _creatureToParty.Remove(creature);

            // If there is now only one party member (or fewer)
            // Party needs to be disbanded and caches updated.
            if (_parties[partyId].Count <= 1)
            {
                foreach (var member in _parties[partyId])
                {
                    _creatureToParty.Remove(member);
                }

                _parties.Remove(partyId);
                _partyLeaders.Remove(partyId);
                return;
            }

            // The party is still valid but the creature who left was its leader. 
            // Swap leadership to the next person in the party list.
            creature = _parties[partyId].First();
            if (_partyLeaders[partyId] == creature)
            {
                _partyLeaders[partyId] = creature;
            }
        }

        /// <summary>
        /// Retrieves all of the members in a creature's party.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <returns>A list of party members.</returns>
        public static List<uint> GetAllPartyMembers(uint creature)
        {
            // Creature isn't in a party. Simply return them in a list.
            if(!_creatureToParty.ContainsKey(creature))
            {
                return new List<uint>
                {
                    creature
                };
            }

            var partyId = _creatureToParty[creature];
            var members = _parties[partyId];
            return members.ToList();
        }

        /// <summary>
        /// Retrieves all of the members in a creature's party who are within the specified range from creature.
        /// </summary>
        /// <param name="creature">The creature to check and use as a distance check.</param>
        /// <param name="distance">The amount of distance to use.</param>
        /// <returns>A list of party members within the specified distance.</returns>
        public static List<uint> GetAllPartyMembersWithinRange(uint creature, float distance)
        {
            if (distance <= 0.0f) distance = 0.0f;
            var members = GetAllPartyMembers(creature);

            var result = new List<uint>();
            foreach (var member in members)
            {
                // Not in the same area
                if (GetArea(member) != GetArea(creature)) continue;

                // This is the creature we're checking. They should be included.
                if (member == creature)
                {
                    result.Add(member);
                    continue;
                }

                // Distance is too great.
                if (GetDistanceBetween(member, creature) > distance) continue;

                result.Add(member);
            }

            return result;
        }

        /// <summary>
        /// Determines if a creature is in the party of another creature.
        /// </summary>
        /// <param name="creature">The creature whose party will be checked</param>
        /// <param name="toCheck">The creature to determine if is in party</param>
        /// <returns>true if in party, false otherwise</returns>
        public static bool IsInParty(uint creature, uint toCheck)
        {
            var members = GetAllPartyMembers(creature);
            return members.Contains(toCheck);
        }
    }
}
