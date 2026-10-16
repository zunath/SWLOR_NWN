namespace SWLOR.CombatSim.Engine
{
    public sealed class DeathRecord
    {
        public string Name { get; init; }
        public int TeamIndex { get; init; }
        public bool IsNpc { get; init; }
        public double At { get; init; }
        public string KilledBy { get; init; }
    }
}
