namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public class EwokRacialAppearanceDefinition: RacialAppearanceBaseDefinition
    {
        public override float MaximumScale => 0.9f;
        public override float MinimumScale => 0.7f;
        public override int[] MaleHeads { get; } = { 111, 112, 113, 114 };
        public override int[] FemaleHeads { get; } = { 111, 112, 113, 114 };

        public override int[] Torsos { get; } = { 208, 209 };
        public override int[] Pelvis { get; } = { 208, 209 };
        public override int[] RightBicep { get; } = { 208 };
        public override int[] RightForearm { get; } = { 208 };
        public override int[] RightHand { get; } = { 208 };
        public override int[] RightThigh { get; } = { 208 };
        public override int[] RightShin { get; } = { 208 };
        public override int[] RightFoot { get; } = { 208 };
        public override int[] LeftBicep { get; } = { 208 };
        public override int[] LeftForearm { get; } = { 208 };
        public override int[] LeftHand { get; } = { 208 };
        public override int[] LeftThigh { get; } = { 208 };
        public override int[] LeftShin { get; } = { 208 };
        public override int[] LeftFoot { get; } = { 208 };
    }
}
