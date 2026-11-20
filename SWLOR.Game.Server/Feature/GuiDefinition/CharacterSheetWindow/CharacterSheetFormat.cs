using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow
{
    /// <summary>
    /// Text formatting shared by the character sheet's tabs. No state and no service.
    /// </summary>
    public static class CharacterSheetFormat
    {
        /// <summary>
        /// Adds the off-hand weapon's value to a main-hand stat's tooltip; with no off-hand weapon the description
        /// stays as it is. The off hand uses its own weapon's skill, so a mixed pair can differ from the main hand.
        /// </summary>
        public static string FormatOffHandTooltip(string description, string offHandValue) =>
            string.IsNullOrEmpty(offHandValue) ? description : $"{description} Off hand: {offHandValue}.";

        /// <summary>
        /// Explains a dual-wield cycle: the delay covers one attack with each hand, so the time per attack is half
        /// of it. Off-Hand Haste shortens only the off hand's share. NUI tooltips show one line, so this appends
        /// to the delay tooltip on the same line.
        /// </summary>
        public static string FormatPairedDelay(int mainHandDelay, int offHandDelay, int offHandHastePercent,
            int effectiveDelayMilliseconds)
        {
            var offHandHaste = offHandHastePercent > 0 ? $", off hand -{offHandHastePercent}%" : string.Empty;
            return $" | 2 attacks per cycle, about {effectiveDelayMilliseconds / 2000f:0.##}s each " +
                   $"(Delay {mainHandDelay} main + {offHandDelay} off{offHandHaste})";
        }

        public static string FormatPercent(int value)
        {
            return $"{value}%";
        }

        public static string FormatMultiplier(float value)
        {
            return $"{value:0.##}x";
        }

        public static string GetStatusDurationLabel(int score)
        {
            return score switch
            {
                < -60 => "Greatly extended",
                < -25 => "Extended",
                < 0 => "Slightly extended",
                0 => "Full Duration",
                < 25 => "Slightly reduced",
                < 45 => "Reduced",
                < 60 => "Strongly reduced",
                >= ResistanceRules.MaximumResistance => "Immune",
                _ => "Greatly reduced"
            };
        }
    }
}
