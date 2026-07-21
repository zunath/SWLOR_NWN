using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public static class ArmorColorIndexCalculator
    {
        public static int CalculatePerPart(
            AppearanceArmor armorPart,
            AppearanceArmorColor colorChannel)
        {
            return (int)AppearanceArmorColor.NumColors +
                   (int)armorPart * (int)AppearanceArmorColor.NumColors +
                   (int)colorChannel;
        }
    }
}
