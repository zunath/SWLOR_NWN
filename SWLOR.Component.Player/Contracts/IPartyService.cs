namespace SWLOR.Component.Player.Contracts
{
    public interface IPartyService
    {
        /// <summary>
        /// When a member of a party accepts an invitation, add them to the caches.
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
        /// When the leader of a party changes, update the caches.
        /// </summary>
        void TransferLeadership();

        /// <summary>
        /// When a player leaves the server, remove them from the party caches.
        /// </summary>
        void LeaveServer();

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
