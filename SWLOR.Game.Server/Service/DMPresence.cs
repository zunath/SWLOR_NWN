using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// DM avatar visibility and movement used by the NUI DM toolbar windows.
    /// </summary>
    public static class DMPresence
    {
        private static readonly Dictionary<(string DMId, GuiWindowType Type), uint> _windowTargets = new();

        /// <summary>
        /// Returns true when the DM's avatar is visible to players (the native "Appear" state).
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        public static bool GetIsManifested(uint dm)
        {
            dm = GetDM(dm);
            var creature = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(dm);
            return creature?.m_pStats != null && creature.m_pStats.m_bDMManifested != 0;
        }

        /// <summary>
        /// Shows or hides the DM's avatar from players, matching the native Appear and Disappear buttons.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="isManifested">true to appear to players, false to disappear.</param>
        public static void SetIsManifested(uint dm, bool isManifested)
        {
            dm = GetDM(dm);
            if (!GetIsDM(dm))
                return;

            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var creature = server.GetCreatureByGameObjectID(dm);
            if (creature?.m_pStats == null)
                return;

            creature.m_pStats.m_bDMManifested = isManifested ? 1 : 0;
            if (!isManifested)
                creature.PacifyCreature();

            // The server never tells the DM's client about the change itself. The login state message runs the
            // client's own Appear/Disappear handler, which fades the avatar on the DM's screen like the native button.
            var player = server.GetClientObjectByObjectId(dm);
            if (player != null)
                server.GetNWSMessage().SendServerToPlayerDungeonMasterLoginState(player, 1, isManifested ? 1 : 0);

            Log.Write(LogGroup.DM, $"{DescribeDM(dm)} {(isManifested ? "appeared" : "disappeared")} in {GetResRef(GetArea(dm))}.");
        }

        /// <summary>
        /// Moves the DM, or the creature they possess, to a location.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="location">The destination.</param>
        /// <param name="destination">What the DM jumped to, for the DM log.</param>
        public static void JumpTo(uint dm, Location location, string destination)
        {
            AssignCommand(GetControlledCreature(dm), () =>
            {
                ClearAllActions();
                JumpToLocation(location);
            });

            Log.Write(LogGroup.DM, $"{DescribeDM(dm)} jumped to {destination} in {GetResRef(GetAreaFromLocation(location))}.");
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

            // Moving a possessed creature would drag the DM controlling it along.
            if (GetIsDMPossessed(target))
                return "A DM is possessing that creature.";

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
        /// Returns the creature a DM is acting through: the creature they possess, or their avatar. Anything else,
        /// including any player from GetFirstPC/GetNextPC, is returned unchanged, so messages, chat, and shader
        /// updates sent to the result reach a possessing DM.
        /// </summary>
        /// <param name="actor">A DM avatar, the creature they possess, or a player.</param>
        public static uint GetControlledCreature(uint actor)
        {
            // GetIsPC is true for whatever a player controls, and false for a possessing DM's avatar.
            if (GetIsPC(actor))
                return actor;

            // Possession only changes the object a player controls; GetFirstPC keeps returning the avatar.
            var controlled = PlayerPlugin.GetGameObject(actor);
            return GetIsDMPossessed(controlled) ? controlled : actor;
        }

        /// <summary>
        /// Returns true when an object from GetFirstPC/GetNextPC belongs to a DM. GetFirstPC keeps returning a DM's
        /// avatar while they possess a creature, and GetIsDM, GetIsDMPossessed, and GetIsPC are all false for the
        /// avatar then, so the creature the player controls is checked too.
        /// </summary>
        /// <param name="player">An object returned by GetFirstPC or GetNextPC.</param>
        public static bool IsControlledByDM(uint player)
        {
            if (GetIsDM(player) || GetIsDMPossessed(player))
                return true;

            var controlled = PlayerPlugin.GetGameObject(player);
            return GetIsDM(controlled) || GetIsDMPossessed(controlled);
        }

        /// <summary>
        /// Returns true when a DM's avatar, or a creature a DM possesses, is in the area.
        /// </summary>
        /// <param name="area">The area to check.</param>
        public static bool IsDMPresentInArea(uint area)
        {
            for (var pc = GetFirstPC(); GetIsObjectValid(pc); pc = GetNextPC())
            {
                if (IsControlledByDM(pc) &&
                    (GetArea(pc) == area || GetArea(GetControlledCreature(pc)) == area))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Opens or closes a DM window on the creature the DM controls, keeping the DM avatar as the window's
        /// owner, the way the /plctools and /music commands do for possessed creatures.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="type">The window to toggle.</param>
        /// <param name="payload">An optional payload for the window.</param>
        /// <param name="tetherObject">An optional object the window closes away from, as in <see cref="Gui.TogglePlayerWindow"/>.</param>
        public static void ToggleWindow(uint actor, GuiWindowType type, GuiPayloadBase payload = null, uint tetherObject = OBJECT_INVALID)
        {
            var dm = GetDM(actor);
            var controlled = GetControlledCreature(dm);

            // After possession starts or ends, the window can still be open on the object the DM no longer
            // controls. Move it to the controlled creature instead of opening a second copy over the same view model.
            var moved = false;
            foreach (var target in GetWindowTargets(dm, type))
            {
                if (target == controlled || !GetIsObjectValid(target) || !Gui.IsWindowOpen(target, type))
                    continue;

                Gui.ClosePlayerWindow(dm, type, target);
                moved = true;
            }

            if (!moved || !Gui.IsWindowOpen(controlled, type))
                Gui.TogglePlayerWindow(dm, type, payload, tetherObject, controlled);

            _windowTargets[(GetObjectUUID(dm), type)] = controlled;
        }

        /// <summary>
        /// Shows a DM window on the creature the DM controls, moving it from any object it was left on.
        /// Unlike <see cref="ToggleWindow"/>, a window already shown in the right place stays open.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="type">The window to show.</param>
        public static void ShowWindow(uint actor, GuiWindowType type)
        {
            var dm = GetDM(actor);
            var controlled = GetControlledCreature(dm);
            foreach (var target in GetWindowTargets(dm, type))
            {
                if (target != controlled && GetIsObjectValid(target) && Gui.IsWindowOpen(target, type))
                    Gui.ClosePlayerWindow(dm, type, target);
            }

            if (!Gui.IsWindowOpen(controlled, type))
                Gui.TogglePlayerWindow(dm, type, uiTarget: controlled);

            _windowTargets[(GetObjectUUID(dm), type)] = controlled;
        }

        /// <summary>
        /// Returns every valid object a window can be shown on for whoever is acting: the actor itself and, for a DM,
        /// the DM avatar and the creature the window was last opened on through <see cref="ToggleWindow"/>.
        /// </summary>
        /// <param name="actor">A player, a DM avatar, or the creature a DM possesses.</param>
        /// <param name="type">The window.</param>
        public static IEnumerable<uint> GetShownOn(uint actor, GuiWindowType type)
        {
            var dm = GetDM(actor);
            var targets = GetIsDM(dm)
                ? GetWindowTargets(dm, type).Append(actor)
                : new[] { actor };
            return targets.Distinct().Where(target => GetIsObjectValid(target));
        }

        // The DM avatar, and the object a window was last opened on through ToggleWindow.
        private static IEnumerable<uint> GetWindowTargets(uint dm, GuiWindowType type)
        {
            yield return dm;
            if (_windowTargets.TryGetValue((GetObjectUUID(dm), type), out var previous) && previous != dm)
                yield return previous;
        }

        /// <summary>
        /// Sends feedback to the DM on the creature they control, so it reaches them while they possess one.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="message">The message.</param>
        public static void SendMessage(uint actor, string message)
        {
            SendMessageToPC(GetControlledCreature(actor), message);
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
            foreach (var target in GetWindowTargets(dm, type))
            {
                if (GetIsObjectValid(target))
                    Gui.ClosePlayerWindow(dm, type, target);
            }
        }
    }
}
