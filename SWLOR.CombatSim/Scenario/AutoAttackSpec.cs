namespace SWLOR.CombatSim.Scenario
{
    public sealed class AutoAttackSpec
    {
        public float AttacksPerSecond { get; set; }
        public DamageSpec Damage { get; set; } = new();
    }
}
