using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CurrencyService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.LightsaberWorkbenchService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using Player = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.Game.Server.Service
{
    public static class LightsaberWorkbench
    {
        public const string KyberTokenTag = "kyber_token";
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

        private static readonly List<SaberHiltPart> _lightsaberHilts = new()
        {
            new SaberHiltPart(11, "Hilt 1.1", "ui_lsh_011"),
            new SaberHiltPart(12, "Hilt 1.2", "ui_lsh_012"),
            new SaberHiltPart(13, "Hilt 1.3", "ui_lsh_013"),
            new SaberHiltPart(14, "Hilt 1.4", "ui_lsh_014"),
            new SaberHiltPart(21, "Hilt 2.1", "ui_lsh_021"),
            new SaberHiltPart(22, "Hilt 2.2", "ui_lsh_022"),
            new SaberHiltPart(23, "Hilt 2.3", "ui_lsh_023"),
            new SaberHiltPart(24, "Hilt 2.4", "ui_lsh_024"),
            new SaberHiltPart(31, "Hilt 3.1", "ui_lsh_031"),
            new SaberHiltPart(32, "Hilt 3.2", "ui_lsh_032"),
            new SaberHiltPart(33, "Hilt 3.3", "ui_lsh_033"),
            new SaberHiltPart(34, "Hilt 3.4", "ui_lsh_034"),
            new SaberHiltPart(41, "Hilt 4.1", "ui_lsh_041"),
            new SaberHiltPart(42, "Hilt 4.2", "ui_lsh_042"),
            new SaberHiltPart(43, "Hilt 4.3", "ui_lsh_043"),
            new SaberHiltPart(51, "Hilt 5.1", "ui_lsh_051"),
            new SaberHiltPart(52, "Hilt 5.2", "ui_lsh_052"),
            new SaberHiltPart(53, "Hilt 5.3", "ui_lsh_053"),
            new SaberHiltPart(54, "Hilt 5.4", "ui_lsh_054"),
            new SaberHiltPart(61, "Hilt 6.1", "ui_lsh_061"),
            new SaberHiltPart(81, "Hilt 8.1", "ui_lsh_081", true),
            new SaberHiltPart(111, "Hilt 11.1", "ui_lsh_111"),
            new SaberHiltPart(121, "Hilt 12.1", "ui_lsh_121"),
            new SaberHiltPart(131, "Hilt 13.1", "ui_lsh_131"),
            new SaberHiltPart(141, "Hilt 14.1", "ui_lsh_141"),
            new SaberHiltPart(161, "Hilt 16.1", "ui_lsh_161"),
            new SaberHiltPart(171, "Hilt 17.1", "ui_lsh_171", true),
            new SaberHiltPart(181, "Hilt 18.1", "ui_lsh_181", true),
            new SaberHiltPart(191, "Hilt 19.1", "ui_lsh_191", true),
            new SaberHiltPart(201, "Hilt 20.1", "ui_lsh_201", true),
            new SaberHiltPart(211, "Hilt 21.1", "ui_lsh_211"),
            new SaberHiltPart(221, "Hilt 22.1", "ui_lsh_221"),
            new SaberHiltPart(231, "Hilt 23.1", "ui_lsh_231", true),
            new SaberHiltPart(232, "Hilt 23.2", "ui_lsh_232", true),
            new SaberHiltPart(241, "Hilt 24.1", "ui_lsh_241"),
            new SaberHiltPart(242, "Hilt 24.2", "ui_lsh_242", true),
            new SaberHiltPart(243, "Hilt 24.3", "ui_lsh_243", true),
            new SaberHiltPart(244, "Hilt 24.4", "ui_lsh_244"),
            new SaberHiltPart(251, "Hilt 25.1", "ui_lsh_251"),
            new SaberHiltPart(252, "Hilt 25.2", "ui_lsh_252"),
            new SaberHiltPart(253, "Hilt 25.3", "ui_lsh_253"),
            new SaberHiltPart(254, "Hilt 25.4", "ui_lsh_254"),
        };

        private static readonly List<SaberHiltPart> _saberstaffHilts = new()
        {
            new SaberHiltPart(11, "Hilt 1.1", "ui_ssh_011"),
            new SaberHiltPart(12, "Hilt 1.2", "ui_ssh_012"),
            new SaberHiltPart(13, "Hilt 1.3", "ui_ssh_013"),
            new SaberHiltPart(14, "Hilt 1.4", "ui_ssh_014"),
            new SaberHiltPart(21, "Hilt 2.1", "ui_ssh_021"),
            new SaberHiltPart(22, "Hilt 2.2", "ui_ssh_022"),
            new SaberHiltPart(23, "Hilt 2.3", "ui_ssh_023"),
            new SaberHiltPart(31, "Hilt 3.1", "ui_ssh_031"),
            new SaberHiltPart(41, "Hilt 4.1", "ui_ssh_041"),
            // Hilt 5.1 (model 51) is deliberately excluded: it is the only S-curved
            // staff hilt and every staff top model is a straight center-axis blade,
            // so no top can visually connect to it.
            new SaberHiltPart(61, "Hilt 6.1", "ui_ssh_061"),
        };

        private static readonly List<SaberBladeColor> _bladeColors = new()
        {
            new SaberBladeColor("Orange", 11, 31, 11, "ui_lsc_orange", LightColor.Orange),
            new SaberBladeColor("Blue", 12, 32, 12, "ui_lsc_blue", LightColor.Blue),
            new SaberBladeColor("Green 1", 13, 33, 13, "ui_lsc_green", LightColor.Green),
            new SaberBladeColor("Red", 14, 34, 14, "ui_lsc_red", LightColor.Red),
            new SaberBladeColor("White", 15, 35, 25, "ui_lsc_white", LightColor.White),
            new SaberBladeColor("Yellow", 21, 71, 21, "ui_lsc_yellow", LightColor.Yellow),
            new SaberBladeColor("Purple 1", 22, 72, 22, "ui_lsc_purple", LightColor.Purple),
            new SaberBladeColor("Teal", 23, 73, 23, "ui_lsc_teal", LightColor.White),
            new SaberBladeColor("Pink", 24, 74, 24, "ui_lsc_pink", LightColor.White),
            new SaberBladeColor("Brown", 51, 61, 51, "ui_lsc_brown", LightColor.White),
            new SaberBladeColor("Green 2", 52, 62, 52, "ui_lsc_rotjgrn", LightColor.Green),
            new SaberBladeColor("Purple 2", 53, 63, 53, "ui_lsc_windu", LightColor.Purple),
            new SaberBladeColor("Lavender", 54, 64, 54, "ui_lsc_lavendr", LightColor.White),
            new SaberBladeColor("Cyan", 55, 65, 55, "ui_lsc_cyan", LightColor.White),

            // Current HAK emitters have straight hilts only; -1 hides unsupported configurations.
            new SaberBladeColor("shoto - orange", -1, -1, 61, "ui_sst_061", LightColor.Orange),
            new SaberBladeColor("shoto - blue", -1, -1, 62, "ui_sst_062", LightColor.Blue),
            new SaberBladeColor("shoto - green", -1, -1, 63, "ui_sst_063", LightColor.Green),
            new SaberBladeColor("shoto - red", -1, -1, 64, "ui_sst_064", LightColor.Red),
            new SaberBladeColor("shoto - yellow", -1, -1, 71, "ui_sst_071", LightColor.Yellow),
            new SaberBladeColor("shoto - purple", -1, -1, 72, "ui_sst_072", LightColor.Purple),
            new SaberBladeColor("shoto - teal", -1, -1, 73, "ui_sst_073", LightColor.White),
            new SaberBladeColor("shoto - pink", -1, -1, 74, "ui_sst_074", LightColor.White),
            new SaberBladeColor("shoto - brown", -1, -1, 81, "ui_sst_081", LightColor.White),
            new SaberBladeColor("shoto - green 2", -1, -1, 82, "ui_sst_082", LightColor.Green),
            new SaberBladeColor("shoto - purple 2", -1, -1, 83, "ui_sst_083", LightColor.Purple),
            new SaberBladeColor("shoto - lavender", -1, -1, 84, "ui_sst_084", LightColor.White),
            new SaberBladeColor("shoto - red (variant 2)", -1, -1, 91, "ui_sst_091", LightColor.Red),
            new SaberBladeColor("shoto - pink (variant 2)", -1, -1, 92, "ui_sst_092", LightColor.White),
            new SaberBladeColor("shoto - cyan", -1, -1, 93, "ui_sst_093", LightColor.White),
            new SaberBladeColor("pike - orange", -1, -1, 101, "ui_sst_101", LightColor.Orange),
            new SaberBladeColor("pike - blue", -1, -1, 102, "ui_sst_102", LightColor.Blue),
            new SaberBladeColor("pike - green", -1, -1, 103, "ui_sst_103", LightColor.Green),
            new SaberBladeColor("pike - red", -1, -1, 104, "ui_sst_104", LightColor.Red),
            new SaberBladeColor("pike - yellow", -1, -1, 111, "ui_sst_111", LightColor.Yellow),
            new SaberBladeColor("pike - purple", -1, -1, 112, "ui_sst_112", LightColor.Purple),
            new SaberBladeColor("pike - teal", -1, -1, 113, "ui_sst_113", LightColor.White),
            new SaberBladeColor("pike - pink", -1, -1, 114, "ui_sst_114", LightColor.White),
            new SaberBladeColor("pike - brown", -1, -1, 121, "ui_sst_121", LightColor.White),
            new SaberBladeColor("pike - green 2", -1, -1, 122, "ui_sst_122", LightColor.Green),
            new SaberBladeColor("pike - purple 2", -1, -1, 123, "ui_sst_123", LightColor.Purple),
            new SaberBladeColor("pike - lavender", -1, -1, 124, "ui_sst_124", LightColor.White),
            new SaberBladeColor("pike - red (variant 2)", -1, -1, 131, "ui_sst_131", LightColor.Red),
            new SaberBladeColor("pike - pink (variant 2)", -1, -1, 132, "ui_sst_132", LightColor.White),
            new SaberBladeColor("pike - cyan", -1, -1, 133, "ui_sst_133", LightColor.White),
            new SaberBladeColor("pike - cyan (variant 2)", -1, -1, 141, "ui_sst_141", LightColor.White),
            new SaberBladeColor("shoto - cyan (variant 2)", -1, -1, 142, "ui_sst_142", LightColor.White),
            new SaberBladeColor("shoto - cyan (variant 3)", -1, -1, 143, "ui_sst_143", LightColor.White),
            new SaberBladeColor("crossguard - red", 191, -1, -1, "ui_lst_191", LightColor.Red),
            new SaberBladeColor("crossguard - blue", 192, -1, -1, "ui_lst_192", LightColor.Blue),
            new SaberBladeColor("crossguard - green", 193, -1, -1, "ui_lst_193", LightColor.Green),
            new SaberBladeColor("crossguard - red (variant 2)", 194, -1, -1, "ui_lst_194", LightColor.Red),
            new SaberBladeColor("crossguard - white", 75, -1, -1, "ui_lst_075", LightColor.White),
            new SaberBladeColor("crossguard - yellow", 81, -1, -1, "ui_lst_081", LightColor.Yellow),
            new SaberBladeColor("crossguard - purple", 82, -1, -1, "ui_lst_082", LightColor.Purple),
            new SaberBladeColor("crossguard - teal", 83, -1, -1, "ui_lst_083", LightColor.White),
            new SaberBladeColor("crossguard - pink", 84, -1, -1, "ui_lst_084", LightColor.White),
            new SaberBladeColor("crossguard - white (variant 2)", 85, -1, -1, "ui_lst_085", LightColor.White),
            new SaberBladeColor("crossguard - orange", 91, -1, -1, "ui_lst_091", LightColor.Orange),
            new SaberBladeColor("crossguard - blue (variant 2)", 92, -1, -1, "ui_lst_092", LightColor.Blue),
            new SaberBladeColor("crossguard - green (variant 2)", 93, -1, -1, "ui_lst_093", LightColor.Green),
            new SaberBladeColor("crossguard - red (variant 3)", 94, -1, -1, "ui_lst_094", LightColor.Red),
            new SaberBladeColor("crossguard - white (variant 3)", 95, -1, -1, "ui_lst_095", LightColor.White),
            new SaberBladeColor("crossguard - orange (variant 2)", 101, -1, -1, "ui_lst_101", LightColor.Orange),
            new SaberBladeColor("crossguard - blue (variant 3)", 102, -1, -1, "ui_lst_102", LightColor.Blue),
            new SaberBladeColor("crossguard - green (variant 3)", 103, -1, -1, "ui_lst_103", LightColor.Green),
            new SaberBladeColor("crossguard - red (variant 4)", 104, -1, -1, "ui_lst_104", LightColor.Red),
            new SaberBladeColor("crossguard - white (variant 4)", 105, -1, -1, "ui_lst_105", LightColor.White),
            new SaberBladeColor("crossguard - brown", 111, -1, -1, "ui_lst_111", LightColor.White),
            new SaberBladeColor("crossguard - green 2", 112, -1, -1, "ui_lst_112", LightColor.Green),
            new SaberBladeColor("crossguard - purple 2", 113, -1, -1, "ui_lst_113", LightColor.Purple),
            new SaberBladeColor("crossguard - lavender", 114, -1, -1, "ui_lst_114", LightColor.White),
            new SaberBladeColor("crossguard - cyan", 115, -1, -1, "ui_lst_115", LightColor.White),
            new SaberBladeColor("crossguard - brown (variant 2)", 121, -1, -1, "ui_lst_121", LightColor.White),
            new SaberBladeColor("crossguard - green 2 (variant 2)", 122, -1, -1, "ui_lst_122", LightColor.Green),
            new SaberBladeColor("crossguard - purple 2 (variant 2)", 123, -1, -1, "ui_lst_123", LightColor.Purple),
            new SaberBladeColor("crossguard - lavender (variant 2)", 124, -1, -1, "ui_lst_124", LightColor.White),
            new SaberBladeColor("crossguard - cyan (variant 2)", 125, -1, -1, "ui_lst_125", LightColor.White),
            new SaberBladeColor("shoto - orange", 131, -1, -1, "ui_lst_131", LightColor.Orange),
            new SaberBladeColor("shoto - blue", 132, -1, -1, "ui_lst_132", LightColor.Blue),
            new SaberBladeColor("shoto - green", 133, -1, -1, "ui_lst_133", LightColor.Green),
            new SaberBladeColor("shoto - red", 134, -1, -1, "ui_lst_134", LightColor.Red),
            new SaberBladeColor("shoto - white", 135, -1, -1, "ui_lst_135", LightColor.White),
            new SaberBladeColor("shoto - yellow", 141, -1, -1, "ui_lst_141", LightColor.Yellow),
            new SaberBladeColor("shoto - purple", 142, -1, -1, "ui_lst_142", LightColor.Purple),
            new SaberBladeColor("shoto - teal", 143, -1, -1, "ui_lst_143", LightColor.White),
            new SaberBladeColor("shoto - pink", 144, -1, -1, "ui_lst_144", LightColor.White),
            new SaberBladeColor("shoto - orange (variant 2)", 151, -1, -1, "ui_lst_151", LightColor.Orange),
            new SaberBladeColor("shoto - blue (variant 2)", 152, -1, -1, "ui_lst_152", LightColor.Blue),
            new SaberBladeColor("shoto - green (variant 2)", 153, -1, -1, "ui_lst_153", LightColor.Green),
            new SaberBladeColor("shoto - red (variant 2)", 154, -1, -1, "ui_lst_154", LightColor.Red),
            new SaberBladeColor("shoto - white (variant 2)", 155, -1, -1, "ui_lst_155", LightColor.White),
            new SaberBladeColor("shoto - teal (variant 2)", 161, -1, -1, "ui_lst_161", LightColor.White),
            new SaberBladeColor("shoto - teal (variant 3)", 165, -1, -1, "ui_lst_165", LightColor.White),
            new SaberBladeColor("shoto - brown", 171, -1, -1, "ui_lst_171", LightColor.White),
            new SaberBladeColor("shoto - green 2", 172, -1, -1, "ui_lst_172", LightColor.Green),
            new SaberBladeColor("shoto - purple 2", 173, -1, -1, "ui_lst_173", LightColor.Purple),
            new SaberBladeColor("shoto - lavender", 174, -1, -1, "ui_lst_174", LightColor.White),
            new SaberBladeColor("shoto - cyan", 175, -1, -1, "ui_lst_175", LightColor.White),
            new SaberBladeColor("shoto - brown (variant 2)", 181, -1, -1, "ui_lst_181", LightColor.White),
            new SaberBladeColor("shoto - green 2 (variant 2)", 182, -1, -1, "ui_lst_182", LightColor.Green),
            new SaberBladeColor("shoto - purple 2 (variant 2)", 183, -1, -1, "ui_lst_183", LightColor.Purple),
            new SaberBladeColor("shoto - lavender (variant 2)", 184, -1, -1, "ui_lst_184", LightColor.White),
            new SaberBladeColor("shoto - cyan (variant 2)", 185, -1, -1, "ui_lst_185", LightColor.White),
        };

        /// <summary>
        /// Retrieves the bottom hilt models available for a given saber weapon type.
        /// </summary>
        public static IReadOnlyList<SaberHiltPart> GetHilts(BaseItem weaponType)
        {
            return weaponType == BaseItem.Saberstaff
                ? _saberstaffHilts.AsReadOnly()
                : _lightsaberHilts.AsReadOnly();
        }

        /// <summary>
        /// Retrieves the blade colors available for a weapon type and hilt style.
        /// </summary>
        public static IReadOnlyList<SaberBladeColor> GetBladeColors(BaseItem weaponType, bool isCurvedHilt)
        {
            return _bladeColors
                .Where(x => GetTopValue(x, weaponType, isCurvedHilt) > -1)
                .ToList();
        }

        /// <summary>
        /// Resolves the top model part value for a color on a specific weapon configuration.
        /// Returns -1 if the color is unavailable for that configuration.
        /// </summary>
        public static int GetTopValue(SaberBladeColor color, BaseItem weaponType, bool isCurvedHilt)
        {
            if (weaponType == BaseItem.Saberstaff)
                return color.SaberstaffTopValue;

            return isCurvedHilt
                ? color.CurvedTopValue
                : color.StraightTopValue;
        }

        /// <summary>
        /// Locates an unattuned Kyber Token item in the player's inventory,
        /// or OBJECT_INVALID if none is carried.
        /// </summary>
        public static uint GetKyberTokenItem(uint player)
        {
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetTag(item) == KyberTokenTag)
                    return item;
            }

            return OBJECT_INVALID;
        }

        /// <summary>
        /// Validates whether a player may use the lightsaber workbench.
        /// Returns an empty string when valid, otherwise the error to display.
        /// </summary>
        public static string ValidateAccess(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return "Only players may use the workbench.";

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer.CharacterType != CharacterType.ForceSensitive)
                return "Only force sensitive characters may use the workbench.";

            if (Currency.GetCurrency(player, CurrencyType.KyberToken) < 1)
            {
                return GetKyberTokenItem(player) != OBJECT_INVALID
                    ? "Use your Kyber Token to attune it before working the bench."
                    : "You need a Kyber Token to use the workbench. Kyber Tokens are issued by staff.";
            }

            return string.Empty;
        }

        /// <summary>
        /// When a lightsaber workbench is used, open the construction menu
        /// for players carrying a Kyber Token.
        /// </summary>
        [NWNEventHandler(ScriptName.OnLightsaberWorkbenchUsed)]
        public static void UseLightsaberWorkbench()
        {
            var player = GetLastUsedBy();
            var error = ValidateAccess(player);
            if (!string.IsNullOrWhiteSpace(error))
            {
                Log.Write(LogGroup.Crafting, $"{GetName(player)} ({GetObjectUUID(player)}) was denied access to a lightsaber workbench: {error}");
                FloatingTextStringOnCreature(error, player, false);
                return;
            }

            Log.Write(LogGroup.Crafting, $"{GetName(player)} ({GetObjectUUID(player)}) opened a lightsaber workbench.");
            Gui.TogglePlayerWindow(player, GuiWindowType.LightsaberWorkbench, null, OBJECT_SELF);
        }
    }
}
