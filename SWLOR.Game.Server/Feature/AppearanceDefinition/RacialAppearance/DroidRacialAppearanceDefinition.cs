namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public class DroidRacialAppearanceDefinition: RacialAppearanceBaseDefinition
    {
        public override float MaximumScale => 1.3f;
        public override int[] MaleHeads { get; } = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 };
        public override int[] FemaleHeads { get; } = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 };

        public override int[] Torsos { get; } = { 1 };
        public override int[] Pelvis { get; } = { 1, };
        public override int[] RightBicep { get; } = { 1, };
        public override int[] RightForearm { get; } = { 1, };
        public override int[] RightHand { get; } = { 1, };
        public override int[] RightThigh { get; } = { 1, };
        public override int[] RightShin { get; } = { 1, };
        public override int[] RightFoot { get; } = { 1, };
        public override int[] LeftBicep { get; } = { 1, };
        public override int[] LeftForearm { get; } = { 1, };
        public override int[] LeftHand { get; } = { 1, };
        public override int[] LeftThigh { get; } = { 1, };
        public override int[] LeftShin { get; } = { 1, };
        public override int[] LeftFoot { get; } = { 1, };
    }
}
