namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public class ZabrakRacialAppearanceDefinition: RacialAppearanceBaseDefinition
    {
        public override float MaximumScale => 1.3f;
        public override int[] MaleHeads { get; } = { 56, 57, 58, 59, 60, 61, 62, 103 };
        public override int[] FemaleHeads { get; } = { 38, 69, 70, 71, 72, 73, 120 };
    }
}
