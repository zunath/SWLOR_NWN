namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public class ShurikenAppearanceDefinition : WeaponAppearanceBaseDefinition
    {
        public override bool IsSimple => true;

        public override int[] SimpleParts { get; } =
        {
            11, 12, 13, 
            21, 22, 23,  
            31, 32, 33,  
            41, 42, 43
        };
    }
}
