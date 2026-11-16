using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// Who is in whose party, as the domains below Social read it. Combat owns the interface and the Social party roster
    /// (<c>PartyRosterService</c>) implements it (supply rule 2 of MaintainabilityRefactorDomainOrder.md): the roster takes no
    /// service, so a reader is built without a construction cycle. The ability auras and the ability target queries read it;
    /// P5-7 moves Combat's own party reads onto it. Static callers reach it through <c>GameServices.PartyRoster</c>.
    /// </summary>
    public interface IPartyRoster
    {
        /// <summary>
        /// Retrieves all of the members in a creature's party.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <returns>A list of party members.</returns>
        List<uint> GetAllPartyMembers(uint creature);

        /// <summary>
        /// Retrieves all of the members in a creature's party who are within the specified range from creature.
        /// </summary>
        /// <param name="creature">The creature to check and use as a distance check.</param>
        /// <param name="distance">The amount of distance to use.</param>
        /// <returns>A list of party members within the specified distance.</returns>
        List<uint> GetAllPartyMembersWithinRange(uint creature, float distance);

        /// <summary>
        /// Determines if a creature is in the party of another creature.
        /// </summary>
        /// <param name="creature">The creature whose party will be checked</param>
        /// <param name="toCheck">The creature to determine if is in party</param>
        /// <returns>true if in party, false otherwise</returns>
        bool IsInParty(uint creature, uint toCheck);
    }
}
