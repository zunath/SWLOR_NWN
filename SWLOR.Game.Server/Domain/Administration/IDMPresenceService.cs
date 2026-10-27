using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Domain.Administration
{
    /// <summary>
    /// DM avatar visibility and movement used by the NUI DM toolbar windows. The static facade
    /// <c>Service.DMPresence</c> forwards to the container's instance of this interface.
    /// </summary>
    public interface IDMPresenceService
    {
        /// <summary>
        /// Returns true when the DM's avatar is visible to players (the native "Appear" state).
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        bool GetIsManifested(uint dm);

        /// <summary>
        /// Shows or hides the DM's avatar from players, matching the native Appear and Disappear buttons.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="isManifested">true to appear to players, false to disappear.</param>
        void SetIsManifested(uint dm, bool isManifested);

        /// <summary>
        /// Fades the DM's avatar on their own screen, or shows it, to match whether players can see it. Nothing is
        /// sent while the DM possesses a creature, so call this again once they return to their avatar.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        void RefreshAvatarFade(uint dm);

        /// <summary>
        /// Moves the DM, or the creature they possess, to a location.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="location">The destination.</param>
        /// <param name="destination">What the DM jumped to, for the DM log.</param>
        void JumpTo(uint dm, Location location, string destination);

        /// <summary>
        /// Brings a creature or player to the DM, or to the creature they possess. Players piloting a ship in
        /// space cannot be summoned, because moving them would move their ship instead.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="target">The creature or player to bring.</param>
        /// <returns>An error message, or an empty string on success.</returns>
        string Summon(uint dm, uint target);

        /// <summary>
        /// Disconnects a player from the server. Written to the DM log.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        /// <param name="player">The player to disconnect.</param>
        void Boot(uint dm, uint player);

        /// <summary>
        /// Describes a DM for audit logs.
        /// </summary>
        /// <param name="dm">The DM avatar or the creature they possess.</param>
        string DescribeDM(uint dm);

        /// <summary>
        /// Returns the DM avatar behind a creature a DM possesses, or the object itself.
        /// DM windows opened while possessing a creature belong to that creature's view.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        uint GetDM(uint actor);

        /// <summary>
        /// Returns the creature a DM is acting through: the creature they possess, or their avatar. Anything else,
        /// including any player from GetFirstPC/GetNextPC, is returned unchanged, so messages, chat, and shader
        /// updates sent to the result reach a possessing DM.
        /// </summary>
        /// <param name="actor">A DM avatar, the creature they possess, or a player.</param>
        uint GetControlledCreature(uint actor);

        /// <summary>
        /// Returns true when an object from GetFirstPC/GetNextPC belongs to a DM. GetFirstPC keeps returning a DM's
        /// avatar while they possess a creature, and GetIsDM, GetIsDMPossessed, and GetIsPC are all false for the
        /// avatar then, so the creature the player controls is checked too.
        /// </summary>
        /// <param name="player">An object returned by GetFirstPC or GetNextPC.</param>
        bool IsControlledByDM(uint player);

        /// <summary>
        /// Returns true when a DM's avatar, or a creature a DM possesses, is in the area.
        /// </summary>
        /// <param name="area">The area to check.</param>
        bool IsDMPresentInArea(uint area);

        /// <summary>
        /// Opens or closes a DM window on the creature the DM controls, keeping the DM avatar as the window's
        /// owner, the way the /plctools and /music commands do for possessed creatures.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="type">The window to toggle.</param>
        /// <param name="payload">An optional payload for the window.</param>
        /// <param name="tetherObject">An optional object the window closes away from, as in <c>Gui.TogglePlayerWindow</c>.</param>
        void ToggleWindow(uint actor, GuiWindowType type, GuiPayloadBase payload = null, uint tetherObject = OBJECT_INVALID);

        /// <summary>
        /// Shows a DM window on the creature the DM controls, moving it from any object it was left on.
        /// Unlike <see cref="ToggleWindow"/>, a window already shown in the right place stays open.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="type">The window to show.</param>
        void ShowWindow(uint actor, GuiWindowType type);

        /// <summary>
        /// Returns every valid object a window can be shown on for whoever is acting: the actor itself and, for a DM,
        /// the DM avatar and the creature the window was last opened on through <see cref="ToggleWindow"/>.
        /// </summary>
        /// <param name="actor">A player, a DM avatar, or the creature a DM possesses.</param>
        /// <param name="type">The window.</param>
        IEnumerable<uint> GetShownOn(uint actor, GuiWindowType type);

        /// <summary>
        /// Sends feedback to the DM on the creature they control, so it reaches them while they possess one.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="message">The message.</param>
        void SendMessage(uint actor, string message);

        /// <summary>
        /// Closes a DM window shown on the creature the DM controls.
        /// </summary>
        /// <param name="actor">A DM avatar or the creature they possess.</param>
        /// <param name="type">The window to close.</param>
        void CloseWindow(uint actor, GuiWindowType type);
    }
}
