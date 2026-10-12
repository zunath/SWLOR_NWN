using NWN.Native.API;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// DM avatar visibility and movement used by the NUI DM toolbar windows.
    /// </summary>
    public static class DMPresence
    {
        /// <summary>
        /// Returns true when the DM's avatar is visible to players (the native "Appear" state).
        /// </summary>
        /// <param name="dm">The DM avatar.</param>
        public static bool GetIsManifested(uint dm)
        {
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(dm);
            return creature?.m_pStats != null && creature.m_pStats.m_bDMManifested != 0;
        }

        /// <summary>
        /// Shows or hides the DM's avatar from players, matching the native Appear and Disappear buttons.
        /// </summary>
        /// <param name="dm">The DM avatar.</param>
        /// <param name="isManifested">true to appear to players, false to disappear.</param>
        public static void SetIsManifested(uint dm, bool isManifested)
        {
            if (!GetIsDM(dm))
                return;

            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(dm);
            if (creature?.m_pStats == null)
                return;

            creature.m_pStats.m_bDMManifested = isManifested ? 1 : 0;
            Log.Write(LogGroup.DM, $"{DescribeDM(dm)} {(isManifested ? "appeared" : "disappeared")} in {GetResRef(GetArea(dm))}.");
        }

        /// <summary>
        /// Moves the DM to a location.
        /// </summary>
        /// <param name="dm">The DM avatar.</param>
        /// <param name="location">The destination.</param>
        public static void JumpTo(uint dm, Location location)
        {
            AssignCommand(dm, () =>
            {
                ClearAllActions();
                JumpToLocation(location);
            });
        }

        /// <summary>
        /// Brings a creature or player to the DM. Players piloting a ship in space cannot be summoned,
        /// because moving them would move their ship instead.
        /// </summary>
        /// <param name="dm">The DM avatar.</param>
        /// <param name="target">The creature or player to bring.</param>
        /// <returns>An error message, or an empty string on success.</returns>
        public static string Summon(uint dm, uint target)
        {
            if (!GetIsObjectValid(target))
                return "That target no longer exists.";

            if (GetIsPC(target) && Space.IsPlayerInSpaceMode(target))
                return $"{GetName(target)} is piloting a ship in space and cannot be summoned.";

            var location = GetLocation(dm);
            AssignCommand(target, () =>
            {
                ClearAllActions();
                JumpToLocation(location);
            });

            Log.Write(LogGroup.DM, $"{DescribeDM(dm)} summoned {GetName(target)} ({GetObjectUUID(target)}) to {GetResRef(GetArea(dm))}.");
            return string.Empty;
        }

        /// <summary>
        /// Describes a DM for audit logs.
        /// </summary>
        /// <param name="dm">The DM avatar.</param>
        public static string DescribeDM(uint dm)
        {
            return $"{GetName(dm)} ({GetPCPlayerName(dm)}) [{GetPCPublicCDKey(dm)}]";
        }
    }
}
