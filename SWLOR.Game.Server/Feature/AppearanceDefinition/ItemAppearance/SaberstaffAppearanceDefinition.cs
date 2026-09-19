namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public class SaberstaffAppearanceDefinition : WeaponAppearanceBaseDefinition
    {
        public override bool IsSimple => false;

        // Appearance Editor encodes color * 100 + model.
        public override int[] TopParts { get; } = { 102, 107, 111 };
        public override int[] MiddleParts { get; } = { 101 };
        public override int[] BottomParts { get; } = { 107, 207, 307 };
    }
}