using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public static class ArmorColorIndexCalculator
    {
        private const string PerPartOverridePrefix = "APC";

        public static int CalculatePerPart(
            AppearanceArmor armorPart,
            AppearanceArmorColor colorChannel)
        {
            return (int)AppearanceArmorColor.NumColors +
                   (int)armorPart * (int)AppearanceArmorColor.NumColors +
                   (int)colorChannel;
        }

        public static string GetPerPartOverrideVariableName(
            AppearanceArmor armorPart,
            AppearanceArmorColor colorChannel)
        {
            return $"{PerPartOverridePrefix}_{(int)armorPart}_{(int)colorChannel}";
        }

        public static bool IsPerPartOverrideVariableName(string variableName)
        {
            return variableName?.StartsWith(
                $"{PerPartOverridePrefix}_",
                System.StringComparison.Ordinal) == true;
        }

        public static bool ShouldUsePerPartColor(int colorId, bool hasExplicitOverride)
        {
            if (colorId == 255)
                return false;

            // Legacy armor blueprints do not serialize per-part color fields. The engine reports
            // those missing fields as zero, so only a marker can distinguish an intentionally
            // selected palette color zero from an absent value that should inherit the global dye.
            return colorId != 0 || hasExplicitOverride;
        }
    }
}
