namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public class TrandoshanRacialAppearanceDefinition: RacialAppearanceBaseDefinition
    {
        public override float MaximumScale => 1.3f;
        public override int[] MaleHeads { get; } = { 2, 96, 97, 98, 101, 111, 123, 124, 125, 143, 147, 148, 162 };
        public override int[] FemaleHeads { get; } = { 24, 50, 51, 126, 128, 129, 131, 135, 150, 157 };

        public override int[] Torsos { get; } = { 201 };
        public override int[] Pelvis { get; } = { 201 };
        public override int[] RightBicep { get; } = { 201 };
        public override int[] RightForearm { get; } = { 201 };
        public override int[] RightHand { get; } = { 201 };
        public override int[] RightThigh { get; } = { 201 };
        public override int[] RightShin { get; } = { 201 };
        public override int[] RightFoot { get; } = { 201 };
        public override int[] LeftBicep { get; } = { 201 };
        public override int[] LeftForearm { get; } = { 201 };
        public override int[] LeftHand { get; } = { 201 };
        public override int[] LeftThigh { get; } = { 201 };
        public override int[] LeftShin { get; } = { 201 };
        public override int[] LeftFoot { get; } = { 201 };
    }
}
