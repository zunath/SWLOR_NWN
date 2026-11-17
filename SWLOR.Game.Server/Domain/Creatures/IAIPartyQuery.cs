using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Creatures
{
    /// <summary>
    /// The party read the creature AI makes to build an ally list. Creatures owns the interface and the Social party service
    /// (<c>PartyService</c>) implements it (supply rule 2 of MaintainabilityRefactorDomainOrder.md; P5-8d of
    /// Readmes/MaintainabilityRefactorCreaturesSplit.md), so <see cref="AIService"/> needs nothing from <c>IPartyService</c>.
    /// </summary>
    public interface IAIPartyQuery
    {
        /// <summary>
        /// Retrieves all of the members in a creature's party.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <returns>A list of party members.</returns>
        List<uint> GetAllPartyMembers(uint creature);
    }
}
