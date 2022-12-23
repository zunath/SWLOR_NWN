namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public class QuarterstaffAppearanceDefinition : WeaponAppearanceBaseDefinition
    {
        public override bool IsSimple => false;

        public override int[] TopParts { get; } =
        {
            101, 102, 103, 104, // Color #1
            201, 202, 203, 204, // Color #2
            302, 303, 304, 305, 325, // Color #3
            401, 402, 403, 404, // Color #4
            // Color #5
            // Color #6
            // Color #7
            // Color #8
            // Color #9
        };
        public override int[] MiddleParts { get; } =
        {
            101, 102, 103, 104, 105, // Color #1
            201, 202, 203, 204, 225, // Color #2
            302, 303, 304, // Color #3
            401, 402, 403, 404, // Color #4
            // Color #5
            // Color #6
            // Color #7
            // Color #8
            // Color #9
        };
        public override int[] BottomParts { get; } =
        {
            101, 102, 103, 104, // Color #1
            201, 202, 203, 204, // Color #2
            301, 302, 303, 304, 305, 325, // Color #3
            402, 403, 404, // Color #4
            // Color #5
            // Color #6
            // Color #7
            // Color #8
            // Color #9
        };
    }
}
