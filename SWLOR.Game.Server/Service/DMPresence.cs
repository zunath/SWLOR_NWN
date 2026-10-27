using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of DM avatar visibility and movement used by the NUI DM toolbar windows. The logic and state live
    /// in <see cref="IDMPresenceService"/>; every member here forwards to the container's instance.
    /// </summary>
    public static class DMPresence
    {
        private static IDMPresenceService Instance => FacadeAccessor.Resolve<IDMPresenceService>();

        /// <summary>
        /// Returns true when the DM's avatar is visible to players (the native "Appear" state).
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        public static bool GetIsManifested(uint dm)
        {
            return Instance.GetIsManifested(dm);
        }

        /// <summary>
        /// Shows or hides the DM's avatar from players, matching the native Appear and Disappear buttons.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="isManifested">true to appear to players, false to disappear.</param>
        public static void SetIsManifested(uint dm, bool isManifested)
        {
            Instance.SetIsManifested(dm, isManifested);
        }

        /// <summary>
        /// Fades the DM's avatar on their own screen, or shows it, to match whether players can see it. Nothing is
        /// sent while the DM possesses a creature, so call this again once they return to their avatar.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        public static void RefreshAvatarFade(uint dm)
        {
            Instance.RefreshAvatarFade(dm);
        }

        /// <summary>
        /// Moves the DM, or the creature they possess, to a location.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="location">The destination.</param>
        /// <param name="destination">What the DM jumped to, for the DM log.</param>
        public static void JumpTo(uint dm, Location location, string destination)
        {
            Instance.JumpTo(dm, location, destination);
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
            return Instance.Summon(dm, target);
        }

        /// <summary>
        /// Disconnects a player from the server. Written to the DM log.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="player">The player to disconnect.</param>
        public static void Boot(uint dm, uint player)
        {
            Instance.Boot(dm, player);
        }

        /// <summary>
        /// Describes a DM for audit logs.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        public static string DescribeDM(uint dm)
        {
            return Instance.DescribeDM(dm);
        }

        /// <summary>
        /// Returns the DM avatar behind a creature a DM possesses, or the object itself.
        /// DM windows opened while possessing a creature belong to that creature's view.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        public static uint GetDM(uint actor)
        {
            return Instance.GetDM(actor);
        }

        /// <summary>
        /// Returns the creature a DM is acting through: the creature they possess, or their avatar. Anything else,
        /// including any player from GetFirstPC/GetNextPC, is returned unchanged, so messages, chat, and shader
        /// updates sent to the result reach a possessing DM.
        /// </summary>
        /// <param name="actor">A DM avatar, the creature they possess, or a player.</param>
        public static uint GetControlledCreature(uint actor)
        {
            return Instance.GetControlledCreature(actor);
        }

        /// <summary>
        /// Returns true when an object from GetFirstPC/GetNextPC belongs to a DM. GetFirstPC keeps returning a DM's
        /// avatar while they possess a creature, and GetIsDM, GetIsDMPossessed, and GetIsPC are all false for the
        /// avatar then, so the creature the player controls is checked too.
        /// </summary>
        /// <param name="player">An object returned by GetFirstPC or GetNextPC.</param>
        public static bool IsControlledByDM(uint player)
        {
            return Instance.IsControlledByDM(player);
        }

        /// <summary>
        /// Returns true when a DM's avatar, or a creature a DM possesses, is in the area.
        /// </summary>
        /// <param name="area">The area to check.</param>
        public static bool IsDMPresentInArea(uint area)
        {
            return Instance.IsDMPresentInArea(area);
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
            Instance.ToggleWindow(actor, type, payload, tetherObject);
        }

        /// <summary>
        /// Shows a DM window on the creature the DM controls, moving it from any object it was left on.
        /// Unlike <see cref="ToggleWindow"/>, a window already shown in the right place stays open.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="type">The window to show.</param>
        public static void ShowWindow(uint actor, GuiWindowType type)
        {
            Instance.ShowWindow(actor, type);
        }

        /// <summary>
        /// Returns every valid object a window can be shown on for whoever is acting: the actor itself and, for a DM,
        /// the DM avatar and the creature the window was last opened on through <see cref="ToggleWindow"/>.
        /// </summary>
        /// <param name="actor">A player, a DM avatar, or the creature a DM possesses.</param>
        /// <param name="type">The window.</param>
        public static IEnumerable<uint> GetShownOn(uint actor, GuiWindowType type)
        {
            return Instance.GetShownOn(actor, type);
        }

        /// <summary>
        /// Sends feedback to the DM on the creature they control, so it reaches them while they possess one.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="message">The message.</param>
        public static void SendMessage(uint actor, string message)
        {
            Instance.SendMessage(actor, message);
        }

        /// <summary>
        /// Closes a DM window shown on the creature the DM controls.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="type">The window to close.</param>
        public static void CloseWindow(uint actor, GuiWindowType type)
        {
            Instance.CloseWindow(actor, type);
        }
    }
}
