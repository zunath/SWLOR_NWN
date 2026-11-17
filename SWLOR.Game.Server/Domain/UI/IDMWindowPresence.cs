using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Domain.UI
{
    /// <summary>
    /// The DM presence reads the window service makes: whether a DM controls a player, and where DM windows are shown. UI owns
    /// the interface and <c>IDMPresenceService</c> extends it (supply rule 2 of MaintainabilityRefactorDomainOrder.md; P5-15a).
    /// </summary>
    public interface IDMWindowPresence
    {
        /// <summary>
        /// Returns true when an object from GetFirstPC/GetNextPC belongs to a DM. GetFirstPC keeps returning a DM's
        /// avatar while they possess a creature, and GetIsDM, GetIsDMPossessed, and GetIsPC are all false for the
        /// avatar then, so the creature the player controls is checked too.
        /// </summary>
        /// <param name="player">An object returned by GetFirstPC or GetNextPC.</param>
        bool IsControlledByDM(uint player);

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
        /// Returns every valid object a window can be shown on for whoever is acting: the actor itself and, for a DM,
        /// the DM avatar and the creature the window was last opened on through <see cref="ToggleWindow"/>.
        /// </summary>
        /// <param name="actor">A player, a DM avatar, or the creature a DM possesses.</param>
        /// <param name="type">The window.</param>
        IEnumerable<uint> GetShownOn(uint actor, GuiWindowType type);
    }
}
