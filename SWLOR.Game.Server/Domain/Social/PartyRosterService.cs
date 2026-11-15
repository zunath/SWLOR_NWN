using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The party roster: who is in whose party and who leads it (a part of the party service, P5-3c), moved from
    /// <c>PartyService</c> unchanged. The party service keeps the roster as the party events arrive; the reads are
    /// <see cref="SWLOR.Game.Server.Domain.Combat.IPartyRoster"/>, which Combat owns, so the ability auras and the ability
    /// target queries read the parties without reaching the party service. It takes no service.
    /// </summary>
    public class PartyRosterService : IPartyRosterService
    {
        [ReloadState(ReloadStateKind.Carried)]
        private readonly Dictionary<Guid, List<uint>> _parties = new();
        [ReloadState(ReloadStateKind.Carried)]
        private readonly Dictionary<uint, Guid> _creatureToParty = new();
        [ReloadState(ReloadStateKind.Carried)]
        private readonly Dictionary<Guid, uint> _partyLeaders = new();

        public Dictionary<Guid, List<uint>> Parties => _parties;

        public Dictionary<uint, Guid> CreatureToParty => _creatureToParty;

        public Dictionary<Guid, uint> PartyLeaders => _partyLeaders;

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
