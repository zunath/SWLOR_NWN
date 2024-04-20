namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public class KelDorRacialAppearanceDefinition : RacialAppearanceBaseDefinition
    {
        public override float MaximumScale { get; } = 1.2f;
        public override int[] MaleHeads { get; } = { 223,224,225,226,227,228,229,233,234 };
        public override int[] FemaleHeads { get; } = { 223,224,225,226,227,228,229,230,231 };
        public override int[] RightHand { get; } = { 45 };
        public override int[] LeftHand { get; } = { 45 };
    }
}
