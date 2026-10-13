using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Scenario;

namespace SWLOR.CombatSim.Engine
{
    public sealed class PendingCast
    {
        public AbilityUseSpec Use { get; init; }
        public AbilityInfo Ability { get; init; }
        public AbilityEffect Effect { get; init; }
        public Combatant Target { get; init; }
        public double ImpactAt { get; init; }
        public bool InterruptedByDamage { get; init; }
        public int FpCost { get; init; }
        public int StmCost { get; init; }
    }
}
