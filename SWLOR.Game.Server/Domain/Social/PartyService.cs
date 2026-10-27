using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.NWN.API.NWNX;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The party caches: the logic and state that used to be static in <c>Service.Party</c>, moved here unchanged. The
    /// facade still carries the event handlers and forwards every call to the container's instance.
    /// </summary>
    public class PartyService : IPartyService
    {
        private readonly Dictionary<Guid, List<uint>> _parties = new();
        private readonly Dictionary<uint, Guid> _creatureToParty = new();
        private readonly Dictionary<Guid, uint> _partyLeaders = new();

        public void JoinParty()
        {
            var creature = OBJECT_SELF;
            var requester = StringToObject(EventsPlugin.GetEventData("INVITED_BY"));

            if (!GetFactionEqual(creature, requester) || IsInParty(requester, creature))
                return;

            // Accepting moves the player out of any party they were already in.
            RemoveCreatureFromParty(creature);
            AddToParty(requester, creature);
        }

        private void AddToParty(uint requester, uint creature)
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

            // Party members already standing in each other's auras never re-fire the aura enter event.
            Ability.EnrollPartyMemberInAuraRanges(creature);

            Telegraph.UpdateShadersForPlayers(GetAllPartyMembers(requester));
        }

        public void AssociateJoinParty()
        {
            var owner = OBJECT_SELF;
            var associate = StringToObject(EventsPlugin.GetEventData("ASSOCIATE_OBJECT_ID"));

            AddToParty(owner, associate);
        }

        public void AssociateLeaveParty()
        {
            var associate = StringToObject(EventsPlugin.GetEventData("ASSOCIATE_OBJECT_ID"));
            RemoveCreatureFromParty(associate);
        }

        public void LeaveParty()
        {
            var creature = StringToObject(EventsPlugin.GetEventData("LEAVING"));
            RemoveCreatureFromParty(creature);
        }

        public void KickFromParty()
        {
            var kicker = OBJECT_SELF;
            var kicked = StringToObject(EventsPlugin.GetEventData("KICKED"));

            // The engine refuses kicks from anyone but the leader.
            if (!IsInParty(kicker, kicked) || GetFactionEqual(kicker, kicked))
                return;

            RemoveCreatureFromParty(kicked);
        }

        public void TransferLeadership()
        {
            var creature = StringToObject(EventsPlugin.GetEventData("NEW_LEADER"));
            var partyId = _creatureToParty[creature];
            _partyLeaders[partyId] = creature;
        }

        public void LeaveServer()
        {
            var creature = GetExitingObject();
            RemoveCreatureFromParty(creature);
        }

        public void RemoveFromCurrentParty(uint player)
        {
            RemoveFromParty(player);
            RemoveCreatureFromParty(player);
        }

        public bool TransferLeadership(uint currentLeader, uint newLeader)
        {
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(currentLeader);
            var faction = creature?.GetFaction();
            if (faction == null || faction.TransferLeadership(newLeader, currentLeader) == 0)
                return false;

            // The faction only reorders its members. Send the new order so every member's party bar moves the crown.
            faction.SendFactionUpdateList();

            if (_creatureToParty.TryGetValue(newLeader, out var partyId))
                _partyLeaders[partyId] = newLeader;

            return true;
        }

        /// <summary>
        /// Removes a creature from a party.
        /// If this would lead to an empty party, or a party with one member, the party gets disbanded.
        /// Otherwise if the leader leaves, a new one is assigned.
        /// </summary>
        /// <param name="creature">The creature being removed from the party.</param>
        private void RemoveCreatureFromParty(uint creature)
        {
            if (!_creatureToParty.ContainsKey(creature)) return;

            var partyId = _creatureToParty[creature];
            var affectedPartyMembers = _parties[partyId].ToList();
            var wasLeader = _partyLeaders[partyId] == creature;

            // Remove this creature from the caches.
            _parties[partyId].Remove(creature);
            _creatureToParty.Remove(creature);

            // Ensure any active aura effects that this creature was receiving from former party members are removed.
            Ability.RefreshPartyAuraEligibility(creature);

            // Party composition changed. Refresh aura recipients for remaining members too,
            // so effects from the departing member are not retained.
            foreach (var member in _parties[partyId])
            {
                Ability.RefreshPartyAuraEligibility(member);
            }

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
            }
            // The party is still valid but the creature who left was its leader.
            // Swap leadership to the next person in the party list.
            else if (wasLeader)
            {
                _partyLeaders[partyId] = _parties[partyId].First();
            }

            Telegraph.UpdateShadersForPlayers(affectedPartyMembers);
        }

        public List<uint> GetAllPartyMembers(uint creature)
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

        public List<uint> GetAllPartyMembersWithinRange(uint creature, float distance)
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

        public bool IsInParty(uint creature, uint toCheck)
        {
            var members = GetAllPartyMembers(creature);
            return members.Contains(toCheck);
        }
    }
}
