using System.Collections.Generic;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Service.LightsaberWorkbenchService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The static API of the lightsaber workbench. The logic and state live in <see cref="ILightsaberWorkbenchService"/>;
    /// every member here forwards to the container's instance, and the event handler stays here so its name in the
    /// handler order manifest does not change.
    /// </summary>
    public static class LightsaberWorkbench
    {
        public const string KyberTokenTag = Domain.Items.LightsaberWorkbenchService.KyberTokenTag;
        public const string WeaponSubmissionTokenTag = "wpn_sub_token";
        public const string LightsaberResref = "ls_custom";
        public const string SaberstaffResref = "ss_custom";

        /// <summary>
        /// Enhancements socketed at the workbench are validated against this
        /// crafting level, matching the tier 5 saber upgrade kit recipes. This
        /// only bounds which enhancements may be socketed; the saber itself is
        /// built at tier 1 and is raised a tier at a time by the upgrade kits.
        /// </summary>
        public const int EnhancementRecipeLevel = 48;

        /// <summary>
        /// The haks ship a single middle grip model per weapon type, so the middle
        /// slot is not player-selectable and always uses this value.
        /// </summary>
        public const int MiddlePartValue = 11;

        /// <summary>
        /// Light Color selector value meaning the light follows the selected blade's catalog
        /// light color. Any other value is an explicit LightColor choice.
        /// </summary>
        public const int MatchBladeLightColor = Domain.Items.LightsaberWorkbenchService.MatchBladeLightColor;

        private static ILightsaberWorkbenchService Instance => FacadeAccessor.Resolve<ILightsaberWorkbenchService>();

        /// <summary>
        /// Determines whether a Weapon Submission Token property carries onto the saber
        /// built from it. Crafted stats, DMG, and an elemental damage type transfer;
        /// the token blueprint's attack delay, skill requirement, and anti-equip Use
        /// Limitation: Perk lock do not, and neither does a Physical damage type, which
        /// sabers already deal.
        /// </summary>
        public static bool ShouldTransferSubmissionProperty(ItemPropertyType type, int subType)
        {
            return Instance.ShouldTransferSubmissionProperty(type, subType);
        }

        /// <summary>
        /// Retrieves the bottom hilt models available for a given saber weapon type.
        /// </summary>
        public static IReadOnlyList<SaberHiltPart> GetHilts(BaseItem weaponType)
        {
            return Instance.GetHilts(weaponType);
        }

        /// <summary>
        /// Retrieves the blade colors available for a weapon type and hilt style.
        /// </summary>
        public static IReadOnlyList<SaberBladeColor> GetBladeColors(BaseItem weaponType, bool isCurvedHilt)
        {
            return Instance.GetBladeColors(weaponType, isCurvedHilt);
        }

        /// <summary>
        /// Resolves the light color for the workbench selector. "Match Blade" uses the selected
        /// blade's catalog light color; any other selection is kept as the player's explicit choice.
        /// </summary>
        public static int ResolveLightColor(SaberBladeColor blade, int selectedLightColor)
        {
            return Instance.ResolveLightColor(blade, selectedLightColor);
        }

        /// <summary>
        /// Resolves the top model part value for a color on a specific weapon configuration.
        /// Returns -1 if the color is unavailable for that configuration.
        /// </summary>
        public static int GetTopValue(SaberBladeColor color, BaseItem weaponType, bool isCurvedHilt)
        {
            return Instance.GetTopValue(color, weaponType, isCurvedHilt);
        }

        /// <summary>
        /// Locates an unattuned Kyber Token item in the player's inventory,
        /// or OBJECT_INVALID if none is carried.
        /// </summary>
        public static uint GetKyberTokenItem(uint player)
        {
            return Instance.GetKyberTokenItem(player);
        }

        /// <summary>
        /// Validates whether a player may use the lightsaber workbench.
        /// Returns an empty string when valid, otherwise the error to display.
        /// </summary>
        public static string ValidateAccess(uint player)
        {
            return Instance.ValidateAccess(player);
        }

        /// <summary>
        /// When a lightsaber workbench is used, open the construction menu
        /// for players carrying a Kyber Token.
        /// </summary>
        [NWNEventHandler(ScriptName.OnLightsaberWorkbenchUsed)]
        public static void UseLightsaberWorkbench()
        {
            Instance.UseLightsaberWorkbench();
        }
    }
}
