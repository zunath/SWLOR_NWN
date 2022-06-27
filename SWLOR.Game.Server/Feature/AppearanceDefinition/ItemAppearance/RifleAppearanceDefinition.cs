namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public class RifleAppearanceDefinition : WeaponAppearanceBaseDefinition
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
            101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 124, 125, // Color #1
            201, 202, 205, 207, 208, 210, 211, 212, 213, 214, 218, 219, 220, 221, 222, // Color #2
            301, 302, 303, 305, 307, 308, 309, 311, 312, 313, 314, 318, 319, // Color #3
            419, 420, // Color #4
            // Color #5
            // Color #6
            // Color #7
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
