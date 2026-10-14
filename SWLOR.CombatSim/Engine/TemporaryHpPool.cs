namespace SWLOR.CombatSim.Engine
{
    public sealed class TemporaryHpPool
    {
        public string Key { get; init; }
        public double Amount { get; set; }
        public double ExpiresAt { get; init; }
    }
}
