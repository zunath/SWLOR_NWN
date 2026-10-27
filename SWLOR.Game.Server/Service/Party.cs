using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Social;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the party caches. The logic and state live in <see cref="IPartyService"/>; every member here
    /// forwards to the container's instance, and the event handlers stay here so their names in the handler order
    /// manifest do not change.
    /// </summary>
    public static class Party
    {
        private static IPartyService Instance => FacadeAccessor.Resolve<IPartyService>();

        /// <summary>
        /// When a player accepts an invitation and the engine puts them in the inviter's party, add them to the caches.
        /// The engine can still refuse the acceptance after the Before event, such as for an expired invitation.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPartyAcceptInvitationAfter)]
        public static void JoinParty()
        {
            Instance.JoinParty();
        }

        /// <summary>
        /// When an associate (droid, pet, henchman, etc.) joins a party, add them to the caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAssociateAddBefore)]
        public static void AssociateJoinParty()
        {
            Instance.AssociateJoinParty();
        }

        /// <summary>
        /// When an associate (droid, pet, henchman, etc.) is removed from the party or leaves, remove them from the caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAssociateRemoveBefore)]
        public static void AssociateLeaveParty()
        {
            Instance.AssociateLeaveParty();
        }

        /// <summary>
        /// When a member of a party leaves, update the caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPartyLeaveBefore)]
        public static void LeaveParty()
        {
            Instance.LeaveParty();
        }

        /// <summary>
        /// When a party member is kicked, update the caches.
        /// A kick raises only the kick events, never the leave events.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPartyKickAfter)]
        public static void KickFromParty()
        {
            Instance.KickFromParty();
        }

        /// <summary>
        /// When the leader of a party changes, update the caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPartyTransferLeadershipBefore)]
        public static void TransferLeadership()
        {
            Instance.TransferLeadership();
        }

        /// <summary>
        /// When a player leaves the server, remove them from the party caches.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void LeaveServer()
        {
            Instance.LeaveServer();
        }

        /// <summary>
        /// Takes a player out of their party, as leaving or being kicked through the native party controls does.
        /// Script-driven removal skips the NWNX party events, so the caches are updated here instead.
        /// </summary>
        /// <param name="player">The player leaving or being removed.</param>
        public static void RemoveFromCurrentParty(uint player)
        {
            Instance.RemoveFromCurrentParty(player);
        }

        /// <summary>
        /// Hands party leadership to another member, as the native party controls do.
        /// </summary>
        /// <param name="currentLeader">The party's current leader.</param>
        /// <param name="newLeader">The member who becomes leader.</param>
        /// <returns>true if the engine accepted the transfer.</returns>
        public static bool TransferLeadership(uint currentLeader, uint newLeader)
        {
            return Instance.TransferLeadership(currentLeader, newLeader);
        }

        /// <summary>
        /// Retrieves all of the members in a creature's party.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <returns>A list of party members.</returns>
        public static List<uint> GetAllPartyMembers(uint creature)
        {
            return Instance.GetAllPartyMembers(creature);
        }

        /// <summary>
        /// Retrieves all of the members in a creature's party who are within the specified range from creature.
        /// </summary>
        /// <param name="creature">The creature to check and use as a distance check.</param>
        /// <param name="distance">The amount of distance to use.</param>
        /// <returns>A list of party members within the specified distance.</returns>
        public static List<uint> GetAllPartyMembersWithinRange(uint creature, float distance)
        {
            return Instance.GetAllPartyMembersWithinRange(creature, distance);
        }

        /// <summary>
        /// Determines if a creature is in the party of another creature.
        /// </summary>
        /// <param name="creature">The creature whose party will be checked</param>
        /// <param name="toCheck">The creature to determine if is in party</param>
        /// <returns>true if in party, false otherwise</returns>
        public static bool IsInParty(uint creature, uint toCheck)
        {
            return Instance.IsInParty(creature, toCheck);
        }
    }
}
