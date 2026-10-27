using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// The party caches: who is in whose party and who leads it. The static facade <c>Service.Party</c> forwards to the
    /// container's instance of this interface and keeps the event handlers.
    /// </summary>
    public interface IPartyService
    {
        /// <summary>
        /// When a player accepts an invitation and the engine puts them in the inviter's party, add them to the caches.
        /// The engine can still refuse the acceptance after the Before event, such as for an expired invitation.
        /// </summary>
        void JoinParty();

        /// <summary>
        /// When an associate (droid, pet, henchman, etc.) joins a party, add them to the caches.
        /// </summary>
        void AssociateJoinParty();

        /// <summary>
        /// When an associate (droid, pet, henchman, etc.) is removed from the party or leaves, remove them from the caches.
        /// </summary>
        void AssociateLeaveParty();

        /// <summary>
        /// When a member of a party leaves, update the caches.
        /// </summary>
        void LeaveParty();

        /// <summary>
        /// When a party member is kicked, update the caches.
        /// A kick raises only the kick events, never the leave events.
        /// </summary>
        void KickFromParty();

        /// <summary>
        /// When the leader of a party changes, update the caches.
        /// </summary>
        void TransferLeadership();

        /// <summary>
        /// When a player leaves the server, remove them from the party caches.
        /// </summary>
        void LeaveServer();

        /// <summary>
        /// Takes a player out of their party, as leaving or being kicked through the native party controls does.
        /// Script-driven removal skips the NWNX party events, so the caches are updated here instead.
        /// </summary>
        /// <param name="player">The player leaving or being removed.</param>
        void RemoveFromCurrentParty(uint player);

        /// <summary>
        /// Hands party leadership to another member, as the native party controls do.
        /// </summary>
        /// <param name="currentLeader">The party's current leader.</param>
        /// <param name="newLeader">The member who becomes leader.</param>
        /// <returns>true if the engine accepted the transfer.</returns>
        bool TransferLeadership(uint currentLeader, uint newLeader);

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
