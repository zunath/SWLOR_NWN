using SWLOR.CombatSim.Effects;

namespace SWLOR.CombatSim.Engine
{
    public sealed class HealOverTimeInstance
    {
        public Combatant Source { get; init; }
        public AbilityEffect Effect { get; init; }
        public float AffinityMultiplier { get; init; }
        public int TicksLeft { get; set; }
        public double NextTickAt { get; set; }
    }
}
