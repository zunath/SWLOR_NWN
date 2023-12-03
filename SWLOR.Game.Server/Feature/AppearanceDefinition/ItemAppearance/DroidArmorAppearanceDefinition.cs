namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public class DroidArmorAppearanceDefinition: BaseArmorAppearanceDefinition
    {
        public override int[] Helmet { get; } = {0 };
        public override int[] Cloak { get; } = {0 };
        public override int[] Neck { get; } = {0, 1, 2, 7, 200, 201, 202, 203, 204, 205, 206, };
        public override int[] Torso { get; } = {1, 2, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214 };
        public override int[] Belt { get; } = { 0 };
        public override int[] Pelvis { get; } = {1, 2, 3, 4, 5, 6, 7, 8 };
                
        public override int[] Shoulder { get; } = {0, };
        public override int[] Bicep { get; } = {1, 2, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212 };
        public override int[] Forearm { get; } = {1, 2, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214 };
        public override int[] Hand { get; } = {1, 2, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211 };
                
        public override int[] Thigh { get; } = {1, 2, 3, 4, 5, 6, 7, 8  };
        public override int[] Shin { get; } = {1, 2, 3, 4, 5, 6, 7, 8 };
        public override int[] Foot { get; } = {1, 2, 3, 4, 5, 6, 7, 8 };
        public override int[] Robe { get; } = {0, };
    }
}
