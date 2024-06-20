namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public class EwokRacialAppearanceDefinition: RacialAppearanceBaseDefinition
    {
        public override float MaximumScale => 0.9f;
        public override float MinimumScale => 0.7f;
        public override int[] MaleHeads { get; } = { 111, 112, 113, 114 };
        public override int[] FemaleHeads { get; } = { 111, 112, 113, 114 };
    }
}
