namespace SWLOR.Shared.UI.Model
{
    public static class GuiStandardColor
    {
        // The following hex codes correspond to colors used on GUI elements.
        // Color tokens won't work on Gui elements.
        public static int ColorTransparent { get; } = Convert.ToInt32("0xFFFFFF00", 16);
        public static int ColorWhite { get; } = Convert.ToInt32("0xFFFFFFFF", 16);
        public static int ColorSilver { get; } = Convert.ToInt32("0xC0C0C0FF", 16);
        public static int ColorGray { get; } = Convert.ToInt32("0x808080FF", 16);
        public static int ColorDarkGray { get; } = Convert.ToInt32("0x303030FF", 16);
        public static int ColorBlack { get; } = Convert.ToInt32("0x000000FF", 16);
        public static int ColorRed { get; } = Convert.ToInt32("0xFF0000FF", 16);
        public static int ColorMaroon { get; } = Convert.ToInt32("0x800000FF", 16);
        public static int ColorOrange { get; } = Convert.ToInt32("0xFFA500FF", 16);
        public static int ColorYellow { get; } = Convert.ToInt32("0xFFFF00FF", 16);
        public static int ColorOlive { get; } = Convert.ToInt32("0x808000FF", 16);
        public static int ColorLime { get; } = Convert.ToInt32("0x00FF00FF", 16);
        public static int ColorGreen { get; } = Convert.ToInt32("0x008000FF", 16);
        public static int ColorAqua { get; } = Convert.ToInt32("0x00FFFFFF", 16);
        public static int ColorTeal { get; } = Convert.ToInt32("0x008080FF", 16);
        public static int ColorBlue { get; } = Convert.ToInt32("0x0000FFFF", 16);
        public static int ColorNavy { get; } = Convert.ToInt32("0x000080FF", 16);
        public static int ColorFuschia { get; } = Convert.ToInt32("0xFF00FFFF", 16);
        public static int ColorPurple { get; } = Convert.ToInt32("0x800080FF", 16);
                
        public static int ColorHealthBar { get; } = Convert.ToInt32("0x8B0000FF", 16);
        public static int ColorFPBar { get; } = Convert.ToInt32("0x00008BFF", 16);
        public static int ColorStaminaBar { get; } = Convert.ToInt32("0x008B00FF", 16);
               
        public static int ColorShieldsBar { get; } = Convert.ToInt32("0x00AAE4FF", 16);
        public static int ColorHullBar { get; } = Convert.ToInt32("0x8B0000FF", 16);
        public static int ColorCapacitorBar { get; } = Convert.ToInt32("0xFFA500FF", 16);
    }
}
