using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the saved outfits: the look of a player's clothes and, when one was worn, their helmet. The
    /// logic lives in <see cref="IWardrobeService"/>; every member here forwards to the container's instance, and the
    /// constants stay here. The Appearance Editor's Wardrobe tab and quick wear row use these operations.
    /// </summary>
    public static class Wardrobe
    {
        // Compile-time constants: callers get the values inlined, as before.
        public const int MaxNameLength = Domain.Items.WardrobeService.MaxNameLength;
        public const int SlotsPerExpansion = Domain.Items.WardrobeService.SlotsPerExpansion;
        public const int MaxSlotLimit = Domain.Items.WardrobeService.MaxSlotLimit;
        public const string ExpansionHint = "Vessa Thrane on the Nar Shaddaa Promenade can expand your wardrobe.";

        private static IWardrobeService Instance => FacadeAccessor.Resolve<IWardrobeService>();

        public static int GetSlotLimit(Player dbPlayer)
        {
            return Instance.GetSlotLimit(dbPlayer);
        }

        /// <summary>The player's outfits in display order (see <see cref="SortForDisplay"/>).</summary>
        public static List<PlayerOutfit> GetOutfits(string playerId)
        {
            return Instance.GetOutfits(playerId);
        }

        /// <summary>
        /// Search results come back in no fixed order, so the list is sorted by name with numbers
        /// compared by value ("Outfit #2" before "Outfit #10"), then by id.
        /// </summary>
        public static List<PlayerOutfit> SortForDisplay(IEnumerable<PlayerOutfit> outfits)
        {
            return Instance.SortForDisplay(outfits);
        }

        public static long CountOutfits(string playerId)
        {
            return Instance.CountOutfits(playerId);
        }

        /// <summary>
        /// Adds five wardrobe slots, up to the maximum, and tells the player the new size.
        /// Vessa Thrane's wardrobe expansion quests grant it.
        /// </summary>
        public static int ExpandSlotLimit(uint player)
        {
            return Instance.ExpandSlotLimit(player);
        }

        public static string ClampName(string name)
        {
            return Instance.ClampName(name);
        }

        public static bool HasClothes(PlayerOutfit outfit) => Instance.HasClothes(outfit);

        public static bool HasHelmet(PlayerOutfit outfit) => Instance.HasHelmet(outfit);

        /// <summary>
        /// Saves the worn clothes and helmet into the outfit. A helmet is saved only while one is
        /// worn; storing without a helmet clears an earlier helmet so the outfit matches the look.
        /// </summary>
        public static bool Store(uint creature, PlayerOutfit outfit, out string message)
        {
            return Instance.Store(creature, outfit, out message);
        }

        /// <summary>
        /// Gives the worn clothes, and the worn helmet when the outfit saved one, the outfit's look.
        /// Gameplay properties of the worn items never change.
        /// </summary>
        public static bool Wear(uint creature, PlayerOutfit outfit, out string message)
        {
            return Instance.Wear(creature, outfit, out message);
        }

        /// <summary>Saves the worn look without storing it in an outfit slot, for an undo.</summary>
        public static PlayerOutfit CaptureWornLook(uint creature)
        {
            return Instance.CaptureWornLook(creature);
        }
    }
}
