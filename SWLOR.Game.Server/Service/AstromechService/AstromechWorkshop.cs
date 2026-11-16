using System;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Service.AstromechService
{
    /// <summary>
    /// The Chassis Workshop: where an astromech changes its chassis, paint and size. It takes the place of the
    /// Appearance Editor, which only fits a humanoid body.
    /// </summary>
    public static class AstromechWorkshop
    {
        public const float ScaleStep = 0.01f;

        public const string NotAstromechMessage = "Only an astromech can use the Chassis Workshop.";
        public const string InSpaceMessage = "The Chassis Workshop cannot be used while flying a ship.";
        public const string HeadScaleMessage =
            "An astromech has no head to scale. Use the Chassis Workshop (/customize) to change its size.";

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
        /// A size rounded to the 0.01 steps of the slider and kept within the limits.
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
        /// The chassis on a row of the chassis list, or null when the row is not one.
        /// </summary>
        public static AstromechChassis GetChassisAt(int index)
        {
            return index >= 0 && index < AstromechChassisCatalog.All.Count
                ? AstromechChassisCatalog.All[index]
                : null;
        }
    }
}
