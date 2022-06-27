namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public class SpearAppearanceDefinition : WeaponAppearanceBaseDefinition
    {
        public override bool IsSimple => false;

        public override int[] TopParts { get; } =
        {
            101, 102, 103, 104, 106, 108, 109, 110, 111, 114, // Color #1
            201, 202, 203, 204, 206, 207, 208, 209, 210, 214, // Color #2
            301, 302, 303, 304, 306, 307, 308, 309, 310, // Color #3
            401, 402, 403, 404, 406, 407, 408, 409, 410, // Color #4
            // Color #5
            // Color #6
            // Color #7
            // Color #8
            // Color #9
        };
        public override int[] MiddleParts { get; } =
        {
            101, 102, 103, 104, 106, 107,  // Color #1
            201, 202, 203, 204, 206, // Color #2
            301, 302, 303, 304, 306, // Color #3
            401, 402, 403, 404, 406, // Color #4
            // Color #5
            // Color #6
            // Color #7
            // Color #8
            // Color #9
        };
        public override int[] BottomParts { get; } =
        {
            101, 102, 103, 104, 106, 109, 110, // Color #1
            201, 202, 203, 204, 206, 209, 210, // Color #2
            301, 302, 303, 304, 306, 309, 310, // Color #3
            401, 402, 403, 404, 406, 409, 410, // Color #4
            // Color #5
            // Color #6
            // Color #7
            // Color #8
            // Color #9
        };
    }
}
