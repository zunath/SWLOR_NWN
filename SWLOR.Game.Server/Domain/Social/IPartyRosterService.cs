using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The party roster (a part of the party service): the reads of <see cref="IPartyRoster"/>, which Combat owns, and the
    /// caches <see cref="PartyService"/> keeps as the party events arrive. Only the party service writes the caches.
    /// </summary>
    public interface IPartyRosterService : IPartyRoster
    {
        /// <summary>The members of each party, by party id.</summary>
        Dictionary<Guid, List<uint>> Parties { get; }

        /// <summary>The party each creature in a party belongs to.</summary>
        Dictionary<uint, Guid> CreatureToParty { get; }

        /// <summary>The leader of each party, by party id.</summary>
        Dictionary<Guid, uint> PartyLeaders { get; }
    }
}
