namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public class PistolAppearanceDefinition : WeaponAppearanceBaseDefinition
    {
        public override bool IsSimple => false;

        public override int[] TopParts { get; } =
        {
            101, // Color #1
            // Color #2
            // Color #3
            // Color #4
            // Color #5
            // Color #6
            // Color #7
            // Color #8
            // Color #9
        };
        public override int[] MiddleParts { get; } =
        {
            101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 114, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, // Color #1
            // Color #2
            301, 302, 303, 305, 307, 308, 309, 310, 311, 320, 322, 324, 325, // Color #3
            // Color #4
            // Color #5
            // Color #6
            701, // Color #7
            // Color #8
            // Color #9
        };
        public override int[] BottomParts { get; } =
        {
            101, // Color #1
            // Color #2
            // Color #3
            // Color #4
            // Color #5
            // Color #6
            // Color #7
            // Color #8
            // Color #9
        };
    }
}
