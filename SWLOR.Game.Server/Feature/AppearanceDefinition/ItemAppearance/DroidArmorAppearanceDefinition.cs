namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public class DroidArmorAppearanceDefinition: IArmorAppearanceDefinition
    {
        public int[] Helmet { get; } = {0 };
        public int[] Cloak { get; } = {0 };
        public int[] Neck { get; } = {0, 1, 2, 7, 200, 201, 202, 203, 204, 205, 206, };
        public int[] Torso { get; } = {1, 2, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214 };
        public int[] Belt { get; } = { 0 };
        public int[] Pelvis { get; } = {1, 2, 3, 4, 5, 6, 7, 8 };

        public int[] Shoulder { get; } = {0, };
        public int[] Bicep { get; } = {1, 2, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212 };
        public int[] Forearm { get; } = {1, 2, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212 };
        public int[] Hand { get; } = {1, 2, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211 };

        public int[] Thigh { get; } = {1, 2, 3, 4, 5, 6, 7, 8  };
        public int[] Shin { get; } = {1, 2, 3, 4, 5, 6, 7, 8 };
        public int[] Foot { get; } = {1, 2, 3, 4, 5, 6, 7, 8 };
        public int[] Robe { get; } = {0, };

    }
}
