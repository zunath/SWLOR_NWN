namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public class TrandoshanRacialAppearanceDefinition: RacialAppearanceBaseDefinition
    {
        public override int[] MaleHeads { get; } = { 2, 101, 111, 123, 124, 125, 143, 162 };
        public override int[] FemaleHeads { get; } = { 24, 126, 128, 135, 150, 157 };

        public override int[] Torsos { get; } = { 201 };
        public override int[] Pelvis { get; } = { 201 };
        public override int[] RightBicep { get; } = { 201 };
        public override int[] RightForearm { get; } = { 201 };
        public override int[] RightHand { get; } = { 201 };
        public override int[] RightThigh { get; } = { 201 };
        public override int[] RightShin { get; } = { 201 };
        public override int[] LeftBicep { get; } = { 201 };
        public override int[] LeftForearm { get; } = { 201 };
        public override int[] LeftHand { get; } = { 201 };
        public override int[] LeftThigh { get; } = { 201 };
        public override int[] LeftShin { get; } = { 201 };
    }
}
