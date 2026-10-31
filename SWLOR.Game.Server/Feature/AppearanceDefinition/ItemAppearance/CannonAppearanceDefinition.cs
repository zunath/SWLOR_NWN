namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public class CannonAppearanceDefinition : WeaponAppearanceBaseDefinition
    {
        public override bool IsSimple => false;

        public override int[] TopParts { get; } =
        {
            101, // Color #1
        };
        public override int[] MiddleParts { get; } =
        {
            101, 102, // Color #1
        };
        public override int[] BottomParts { get; } =
        {
            101, // Color #1
        };
    }
}
