namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public  class DaggerAppearanceDefinition: WeaponAppearanceBaseDefinition
    {
        public override bool IsSimple => false;

        public override int[] TopParts { get; } =
        {
            101, 102, 104, 105, 106, 107, 118, 120,  // Color #1
            201, 202, 204, 205, 206, 207, 216, 218, 220, // Color #2
            301, 302, 304, 305, 306, 307, 318, 320, // Color #3
            401, 402, 404, 405, 406, 407, 416, 418, 420, // Color #4
            // Color #5
            // Color #6
            701, 702, // Color #7
            801, 802, 803, 804, 805, 806, // Color #8
            901, 902, 903, 905, 906, // Color #9
        };
        public override int[] MiddleParts { get; } =
        {
            101, 102, 104, 105, 106, 107, 122, // Color #1
            201, 202, 204, 205, 206, 216, 222, // Color #2
            302, 304, 305, 306, 322, // Color #3
            402, 404, 405, 406, 422, // Color #4
            // Color #5
            // Color #6
            // Color #7
            802, 803, 804, 805, 806, // Color #8
            903, 905, 906, // Color #9
        };
        public override int[] BottomParts { get; } =
        {
            101, 102, 104, 105, 106, 121, 122, // Color #1
            201, 202, 204, 205, 206, 221, 222, // Color #2
            301, 302, 304, 305, 306, 316, 321, 322, // Color #3
            401, 402, 404, 405, 406, 416, 421, 422, // Color #4
            // Color #5
            // Color #6
            // Color #7
            // Color #8
            // Color #9
        };
    }
}
