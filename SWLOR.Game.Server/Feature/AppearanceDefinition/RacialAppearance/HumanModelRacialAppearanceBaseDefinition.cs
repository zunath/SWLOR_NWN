namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public abstract class HumanModelRacialAppearanceBaseDefinition : RacialAppearanceBaseDefinition
    {
        public override int[] RightBicep { get; } = { 1, 2, 251 };
        public override int[] LeftBicep { get; } = { 1, 2, 251 };
    }
}
