namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public class WookieeRacialAppearanceDefinition: RacialAppearanceBaseDefinition
    {
        public override float MaximumScale => 1.5f;

        public override int[] MaleHeads { get; } = { 117, 119, 192, 193 };
        public override int[] FemaleHeads { get; } = { 110, 185, 186, 190, 192, 193, 195 };
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
