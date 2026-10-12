using NWN.Native.API;
using SWLOR.Game.Server.Service.GuiService;
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
        /// Moves the DM, or the creature they possess, to a location.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="location">The destination.</param>
        public static void JumpTo(uint dm, Location location)
        {
            AssignCommand(GetControlledCreature(dm), () =>
            {
                ClearAllActions();
                JumpToLocation(location);
            });
        }

        /// <summary>
        /// Brings a creature or player to the DM, or to the creature they possess. Players piloting a ship in
        /// space cannot be summoned, because moving them would move their ship instead.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="target">The creature or player to bring.</param>
        /// <returns>An error message, or an empty string on success.</returns>
        public static string Summon(uint dm, uint target)
        {
            if (!GetIsObjectValid(target))
                return "That target no longer exists.";

            if (GetIsPC(target) && Space.IsPlayerInSpaceMode(target))
                return $"{GetName(target)} is piloting a ship in space and cannot be summoned.";

            var controlled = GetControlledCreature(dm);
            var location = GetLocation(controlled);
            AssignCommand(target, () =>
            {
                ClearAllActions();
                JumpToLocation(location);
            });

            Log.Write(LogGroup.DM, $"{DescribeDM(dm)} summoned {GetName(target)} ({GetObjectUUID(target)}) to {GetResRef(GetArea(controlled))}.");
            return string.Empty;
        }

        /// <summary>
        /// Describes a DM for audit logs.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        public static string DescribeDM(uint dm)
        {
            dm = GetDM(dm);
            return $"{GetName(dm)} ({GetPCPlayerName(dm)}) [{GetPCPublicCDKey(dm)}]";
        }

        /// <summary>
        /// Returns the DM avatar behind a creature a DM possesses, or the object itself.
        /// DM windows opened while possessing a creature belong to that creature's view.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        public static uint GetDM(uint actor)
        {
            return GetIsDMPossessed(actor) ? GetMaster(actor) : actor;
        }

        /// <summary>
        /// Returns the creature a DM is acting through: the creature they possess, or their avatar.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        public static uint GetControlledCreature(uint actor)
        {
            if (GetIsDMPossessed(actor))
                return actor;

            // A possessed creature is listed among the PCs, with the DM avatar as its master.
            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (GetIsDMPossessed(pc) && GetMaster(pc) == actor)
                    return pc;
            }

            return actor;
        }

        /// <summary>
        /// Opens or closes a DM window on the creature the DM controls, keeping the DM avatar as the window's
        /// owner, the way the /plctools and /music commands do for possessed creatures.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="type">The window to toggle.</param>
        /// <param name="payload">An optional payload for the window.</param>
        public static void ToggleWindow(uint actor, GuiWindowType type, GuiPayloadBase payload = null)
        {
            var dm = GetDM(actor);
            Gui.TogglePlayerWindow(dm, type, payload, uiTarget: GetControlledCreature(dm));
        }

        /// <summary>
        /// Closes a DM window shown on the creature the DM controls.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="type">The window to close.</param>
        public static void CloseWindow(uint actor, GuiWindowType type)
        {
            var dm = GetDM(actor);
            Gui.ClosePlayerWindow(dm, type, GetControlledCreature(dm));
        }
    }
}
