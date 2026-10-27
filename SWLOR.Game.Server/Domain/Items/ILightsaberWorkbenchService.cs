using System.Collections.Generic;
using SWLOR.Game.Server.Service.LightsaberWorkbenchService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// The lightsaber workbench: the part catalog (hilts and blade colors), the rules for what carries onto a built
    /// saber and for who may use the bench. The static facade <c>Service.LightsaberWorkbench</c> forwards to the
    /// container's instance of this interface and keeps the event handler and the constants.
    /// </summary>
    public interface ILightsaberWorkbenchService
    {
        /// <summary>
        /// Determines whether a Weapon Submission Token property carries onto the saber
        /// built from it. Crafted stats, DMG, and an elemental damage type transfer;
        /// the token blueprint's attack delay, skill requirement, and anti-equip Use
        /// Limitation: Perk lock do not, and neither does a Physical damage type, which
        /// sabers already deal.
        /// </summary>
        bool ShouldTransferSubmissionProperty(ItemPropertyType type, int subType);

        /// <summary>
        /// Retrieves the bottom hilt models available for a given saber weapon type.
        /// </summary>
        IReadOnlyList<SaberHiltPart> GetHilts(BaseItem weaponType);

        /// <summary>
        /// Retrieves the blade colors available for a weapon type and hilt style.
        /// </summary>
        IReadOnlyList<SaberBladeColor> GetBladeColors(BaseItem weaponType, bool isCurvedHilt);

        /// <summary>
        /// Resolves the light color for the workbench selector. "Match Blade" uses the selected
        /// blade's catalog light color; any other selection is kept as the player's explicit choice.
        /// </summary>
        int ResolveLightColor(SaberBladeColor blade, int selectedLightColor);

        /// <summary>
        /// Resolves the top model part value for a color on a specific weapon configuration.
        /// Returns -1 if the color is unavailable for that configuration.
        /// </summary>
        int GetTopValue(SaberBladeColor color, BaseItem weaponType, bool isCurvedHilt);

        /// <summary>
        /// Locates an unattuned Kyber Token item in the player's inventory,
        /// or OBJECT_INVALID if none is carried.
        /// </summary>
        uint GetKyberTokenItem(uint player);

        /// <summary>
        /// Validates whether a player may use the lightsaber workbench.
        /// Returns an empty string when valid, otherwise the error to display.
        /// </summary>
        string ValidateAccess(uint player);

        /// <summary>
        /// When a lightsaber workbench is used, open the construction menu
        /// for players carrying a Kyber Token.
        /// </summary>
        void UseLightsaberWorkbench();
    }
}
