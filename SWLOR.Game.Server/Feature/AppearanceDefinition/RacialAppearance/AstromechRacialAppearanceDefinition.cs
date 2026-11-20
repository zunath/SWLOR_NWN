namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    /// <summary>
    /// An astromech is a dome (head), a barrel (torso) and two legs (thighs), one of each per chassis design.
    /// It has no neck, pelvis, arms, shins or feet.
    /// </summary>
    public class AstromechRacialAppearanceDefinition : RacialAppearanceBaseDefinition
    {
        // The dome is seated on the barrel, so it keeps its size.
        public override float MaximumHeadScale => 1f;
        public override float MinimumHeadScale => 1f;

        public override int[] MaleHeads { get; } = { 1, 2, 3, 4, 5, 6, 7, 8 };
        public override int[] FemaleHeads { get; } = { 1, 2, 3, 4, 5, 6, 7, 8 };
        public override int[] Torsos { get; } = { 1, 2, 3, 4, 5, 6, 7, 8 };
        public override int[] RightThigh { get; } = { 1, 2, 3, 4, 5, 6, 7, 8 };
        public override int[] LeftThigh { get; } = { 1, 2, 3, 4, 5, 6, 7, 8 };

        public override int[] Pelvis { get; } = { 0 };
        public override int[] RightBicep { get; } = { 0 };
        public override int[] RightForearm { get; } = { 0 };
        public override int[] RightHand { get; } = { 0 };
        public override int[] RightShin { get; } = { 0 };
        public override int[] RightFoot { get; } = { 0 };
        public override int[] LeftBicep { get; } = { 0 };
        public override int[] LeftForearm { get; } = { 0 };
        public override int[] LeftHand { get; } = { 0 };
        public override int[] LeftShin { get; } = { 0 };
        public override int[] LeftFoot { get; } = { 0 };
    }
}
