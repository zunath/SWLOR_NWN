using System.Collections.Generic;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// Saved outfits: the look of a player's clothes and, when one was worn, their helmet. The static facade
    /// <c>Service.Wardrobe</c> forwards to the container's instance of this interface and keeps the constants.
    /// </summary>
    public interface IWardrobeService
    {
        /// <summary>
        /// How many outfits a player can save: their own limit, or the default when they have none (or no record).
        /// </summary>
        /// <param name="dbPlayer">The player record, or null.</param>
        /// <returns>The number of outfit slots.</returns>
        int GetSlotLimit(Player dbPlayer);

        /// <summary>The player's outfits in display order (see <see cref="SortForDisplay"/>).</summary>
        List<PlayerOutfit> GetOutfits(string playerId);

        /// <summary>
        /// Search results come back in no fixed order, so the list is sorted by name with numbers
        /// compared by value ("Outfit #2" before "Outfit #10"), then by id.
        /// </summary>
        List<PlayerOutfit> SortForDisplay(IEnumerable<PlayerOutfit> outfits);

        /// <summary>How many outfits the player has saved.</summary>
        long CountOutfits(string playerId);

        /// <summary>
        /// Adds five wardrobe slots, up to the maximum, and tells the player the new size.
        /// Vessa Thrane's wardrobe expansion quests grant it.
        /// </summary>
        int ExpandSlotLimit(uint player);

        /// <summary>Trims an outfit name and cuts it to the longest name an outfit can have.</summary>
        string ClampName(string name);

        /// <summary>Whether the outfit has saved clothes.</summary>
        bool HasClothes(PlayerOutfit outfit);

        /// <summary>Whether the outfit has a saved helmet.</summary>
        bool HasHelmet(PlayerOutfit outfit);

        /// <summary>
        /// Saves the worn clothes and helmet into the outfit. A helmet is saved only while one is
        /// worn; storing without a helmet clears an earlier helmet so the outfit matches the look.
        /// </summary>
        bool Store(uint creature, PlayerOutfit outfit, out string message);

        /// <summary>
        /// Gives the worn clothes, and the worn helmet when the outfit saved one, the outfit's look.
        /// Gameplay properties of the worn items never change.
        /// </summary>
        bool Wear(uint creature, PlayerOutfit outfit, out string message);

        /// <summary>Saves the worn look without storing it in an outfit slot, for an undo.</summary>
        PlayerOutfit CaptureWornLook(uint creature);
    }
}
