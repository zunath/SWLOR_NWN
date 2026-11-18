using System;
using SWLOR.Game.Server.Feature.AppearanceDefinition.AppearancePreview;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Service.AstromechService
{
    /// <summary>
    /// The Chassis Workshop: where an astromech changes its chassis, paint and size. It takes the place of the
    /// Appearance Editor, which only fits a humanoid body, and works the same way.
    /// </summary>
    public static class AstromechWorkshop
    {
        public const float ScaleStep = 0.01f;

        /// <summary>The palette is drawn sixteen colours to a row, as in the Appearance Editor.</summary>
        public const int PaletteColumns = 16;

        public const string ClothPaletteSheet = "gui_pal_tattoo";
        public const string MetalPaletteSheet = "gui_pal_armor01";

        public const string NotAstromechMessage = "Only an astromech can use the Chassis Workshop.";
        public const string InSpaceMessage = "The Chassis Workshop cannot be used while flying a ship.";
        public const string HeadScaleMessage =
            "An astromech has no head to scale. Use the Chassis Workshop (/customize) to change its size.";
        public const string LargestMessage = "You cannot increase your size any further.";
        public const string SmallestMessage = "You cannot decrease your size any further.";

        /// <summary>
        /// Opens the Chassis Workshop for an astromech player instead of the Appearance Editor.
        /// Returns false, and does nothing, for anyone else so the caller opens the Appearance Editor.
        /// </summary>
        public static bool TryOpenInsteadOfAppearanceEditor(uint player)
        {
            if (!Astromech.IsAstromech(player))
                return false;

            if (Space.IsPlayerInSpaceMode(player))
            {
                SendMessageToPC(player, InSpaceMessage);
                return true;
            }

            Gui.TogglePlayerWindow(player, GuiWindowType.ChassisWorkshop);
            return true;
        }

        /// <summary>
        /// A size rounded to its 0.01 steps and kept within the limits.
        /// </summary>
        public static float RoundScale(float scale)
        {
            var clamped = AstromechChassisCatalog.ClampScale(scale);
            var rounded = MathF.Round(clamped / ScaleStep) * ScaleStep;
            return AstromechChassisCatalog.ClampScale(MathF.Round(rounded, 2));
        }

        /// <summary>
        /// One step up (direction above zero) or down from a size, kept within the limits.
        /// </summary>
        public static float StepScale(float current, int direction)
        {
            var step = direction > 0 ? ScaleStep : direction < 0 ? -ScaleStep : 0f;
            return RoundScale(RoundScale(current) + step);
        }

        public static string FormatScale(float scale)
        {
            return $"{(int)MathF.Round(RoundScale(scale) * 100f)}%";
        }

        /// <summary>
        /// The chassis at a position in the chassis pictures, or null when the position is not one.
        /// </summary>
        public static AstromechChassis GetChassisAt(int index)
        {
            return index >= 0 && index < AstromechChassisCatalog.All.Count
                ? AstromechChassisCatalog.All[index]
                : null;
        }

        /// <summary>
        /// The position of the chassis one step after (direction above zero) or before another, going round
        /// at either end, as the previous and next buttons move.
        /// </summary>
        public static int StepChassis(int index, int direction)
        {
            var count = AstromechChassisCatalog.All.Count;
            var step = direction > 0 ? 1 : direction < 0 ? -1 : 0;
            return ((index + step) % count + count) % count;
        }

        /// <summary>The picture of a chassis, in the paint it comes with.</summary>
        public static string GetPictureKey(AstromechChassis chassis)
        {
            return chassis.Model;
        }

        /// <summary>The view after another: front, back, angle, then front again.</summary>
        public static AppearancePreviewView NextView(AppearancePreviewView view)
        {
            return view switch
            {
                AppearancePreviewView.Front => AppearancePreviewView.Back,
                AppearancePreviewView.Back => AppearancePreviewView.Angle,
                _ => AppearancePreviewView.Front
            };
        }

        /// <summary>The palette sheet a paint zone picks from: metal for the trim, cloth for the other two.</summary>
        public static string GetPaletteSheet(AstromechPaintZone zone)
        {
            return zone == AstromechPaintZone.Trim ? MetalPaletteSheet : ClothPaletteSheet;
        }

        /// <summary>The colour of a palette cell for a paint zone.</summary>
        public static TintMapColor GetPaletteColor(AstromechPaintZone zone, int paletteIndex)
        {
            return TintMapPaletteColors.GetColor(AstromechChassisCatalog.GetLayer(zone), paletteIndex);
        }

        /// <summary>
        /// The palette cell whose colour a zone's paint is, or -1 when the paint is not a palette colour.
        /// Several cells can hold the same colour; the first one is the answer.
        /// </summary>
        public static int FindPaletteIndex(AstromechPaintZone zone, TintMapColor color)
        {
            for (var index = 0; index < TintMapMaterialRegistry.PaletteColorCount; index++)
            {
                if (GetPaletteColor(zone, index) == color)
                    return index;
            }

            return -1;
        }

        /// <summary>
        /// Reads the three RGB boxes. True only when each holds a whole number from 0 to 255, so a colour
        /// half typed is never applied.
        /// </summary>
        public static bool TryParseRgb(string red, string green, string blue, out TintMapColor color)
        {
            color = default;
            if (!TryParseChannel(red, out var r) || !TryParseChannel(green, out var g) || !TryParseChannel(blue, out var b))
                return false;

            color = new TintMapColor(r, g, b);
            return true;
        }

        /// <summary>
        /// What an RGB box keeps of what was typed into it: the digits, and 255 for any number above it.
        /// </summary>
        public static string NormalizeChannel(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var digits = string.Empty;
            foreach (var character in text)
            {
                if (character >= '0' && character <= '9')
                    digits += character;
            }

            return int.TryParse(digits, out var value) && value > byte.MaxValue ? byte.MaxValue.ToString() : digits;
        }

        private static bool TryParseChannel(string text, out byte value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text) || text.Length > 3)
                return false;

            var total = 0;
            foreach (var character in text)
            {
                if (character < '0' || character > '9')
                    return false;
                total = total * 10 + (character - '0');
            }

            if (total > byte.MaxValue)
                return false;

            value = (byte)total;
            return true;
        }
    }
}
