namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public class LightsaberAppearanceDefinition : WeaponAppearanceBaseDefinition
    {
        public override bool IsSimple => false;

        public override int[] TopParts { get; } =
        {
            114,
            108,
            102
        };

        public override int[] MiddleParts { get; } =
        {
            101
        };

        public override int[] BottomParts { get; } =
        {
            215, 
            315, 
            115  
        };
    }
}