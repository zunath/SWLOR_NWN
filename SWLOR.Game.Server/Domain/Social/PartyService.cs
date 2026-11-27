using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWNX;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The party caches: the logic that used to be static in <c>Party</c>, moved here unchanged. The event handlers run on the
    /// container's instance; static callers reach it through <c>GameServices.Party</c>. The caches themselves are the party
    /// roster (<see cref="IPartyRosterService"/>), which the domains below read through Combat's
    /// <see cref="IPartyRoster"/>; this service keeps them as the party events arrive and answers its reads from them. The
    /// auras it refreshes on a party change reach back to the parties through the roster.
    /// </summary>
    public class PartyService : IPartyService
    {
        private readonly IPartyRosterService _roster;
        private readonly IAbilityAuraService _abilityAura;
        private readonly ITelegraphService _telegraph;

        public PartyService(IPartyRosterService roster, IAbilityAuraService abilityAura, ITelegraphService telegraph)
        {
            _roster = roster;
            _abilityAura = abilityAura;
            _telegraph = telegraph;
        }

        [NWNEventHandler(ScriptName.OnPartyAcceptInvitationAfter)]
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
            if (!_roster.CreatureToParty.ContainsKey(requester))
            {
                var partyId = Guid.NewGuid();
                _roster.Parties[partyId] = new List<uint>
                {
                    requester,
                    creature
                };
                _roster.PartyLeaders[partyId] = requester;
                _roster.CreatureToParty[creature] = partyId;
                _roster.CreatureToParty[requester] = partyId;
            }
            // This is an existing party.
            // Add the creature to the party cache.
            else
            {
                var partyId = _roster.CreatureToParty[requester];
                _roster.Parties[partyId].Add(creature);
                _roster.CreatureToParty[creature] = partyId;
            }

            // Party members already standing in each other's auras never re-fire the aura enter event.
            _abilityAura.EnrollPartyMemberInAuraRanges(creature);

            _telegraph.UpdateShadersForPlayers(GetAllPartyMembers(requester));
        }

        [NWNEventHandler(ScriptName.OnAssociateAddBefore)]
        public void AssociateJoinParty()
        {
            var owner = OBJECT_SELF;
            var associate = StringToObject(EventsPlugin.GetEventData("ASSOCIATE_OBJECT_ID"));

            AddToParty(owner, associate);
        }

        [NWNEventHandler(ScriptName.OnAssociateRemoveBefore)]
        public void AssociateLeaveParty()
        {
            var associate = StringToObject(EventsPlugin.GetEventData("ASSOCIATE_OBJECT_ID"));
            RemoveCreatureFromParty(associate);
        }

        [NWNEventHandler(ScriptName.OnPartyLeaveBefore)]
        public void LeaveParty()
        {
            var creature = StringToObject(EventsPlugin.GetEventData("LEAVING"));
            RemoveCreatureFromParty(creature);
        }

        [NWNEventHandler(ScriptName.OnPartyKickAfter)]
        public void KickFromParty()
        {
            var kicker = OBJECT_SELF;
            var kicked = StringToObject(EventsPlugin.GetEventData("KICKED"));

            // The engine refuses kicks from anyone but the leader.
            if (!IsInParty(kicker, kicked) || GetFactionEqual(kicker, kicked))
                return;

            RemoveCreatureFromParty(kicked);
        }

        [NWNEventHandler(ScriptName.OnPartyTransferLeadershipBefore)]
        public void TransferLeadership()
        {
            var creature = StringToObject(EventsPlugin.GetEventData("NEW_LEADER"));
            var partyId = _roster.CreatureToParty[creature];
            _roster.PartyLeaders[partyId] = creature;
        }

        [PlayerSession(PlayerReload.LogoutOnly,
            "Takes the player out of their party; the parties are carried.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
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

            if (_roster.CreatureToParty.TryGetValue(newLeader, out var partyId))
                _roster.PartyLeaders[partyId] = newLeader;

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
            if (!_roster.CreatureToParty.ContainsKey(creature)) return;

            var partyId = _roster.CreatureToParty[creature];
            var affectedPartyMembers = _roster.Parties[partyId].ToList();
            var wasLeader = _roster.PartyLeaders[partyId] == creature;

            // Remove this creature from the caches.
            _roster.Parties[partyId].Remove(creature);
            _roster.CreatureToParty.Remove(creature);

            // Ensure any active aura effects that this creature was receiving from former party members are removed.
            _abilityAura.RefreshPartyAuraEligibility(creature);

            // Party composition changed. Refresh aura recipients for remaining members too,
            // so effects from the departing member are not retained.
            foreach (var member in _roster.Parties[partyId])
            {
                _abilityAura.RefreshPartyAuraEligibility(member);
            }

            // If there is now only one party member (or fewer)
            // Party needs to be disbanded and caches updated.
            if (_roster.Parties[partyId].Count <= 1)
            {
                foreach (var member in _roster.Parties[partyId])
                {
                    _roster.CreatureToParty.Remove(member);
                }

                _roster.Parties.Remove(partyId);
                _roster.PartyLeaders.Remove(partyId);
            }
            // The party is still valid but the creature who left was its leader.
            // Swap leadership to the next person in the party list.
            else if (wasLeader)
            {
                _roster.PartyLeaders[partyId] = _roster.Parties[partyId].First();
            }

            _telegraph.UpdateShadersForPlayers(affectedPartyMembers);
        }

        public List<uint> GetAllPartyMembers(uint creature) => _roster.GetAllPartyMembers(creature);

        public List<uint> GetAllPartyMembersWithinRange(uint creature, float distance) =>
            _roster.GetAllPartyMembersWithinRange(creature, distance);

        public bool IsInParty(uint creature, uint toCheck) => _roster.IsInParty(creature, toCheck);
    }
}
