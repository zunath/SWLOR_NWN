namespace SWLOR.CraftSim.Legacy.Gathering
{
    public sealed record ScavengeRow(
        int HardLook, int PerceptionModifier, double SecondAttemptChance, double ExpectedAttempts, double ExpectedSuccesses,
        double SimulatedAttempts, double SimulatedSuccesses);
}
