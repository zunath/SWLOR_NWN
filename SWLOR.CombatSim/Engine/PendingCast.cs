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
        public double ImpactAt { get; set; }

        /// <summary>Paid for and on recast, waiting out the ability's impact delay.</summary>
        public bool Activated { get; set; }
        public bool InterruptedByDamage { get; init; }
        public int FpCost { get; init; }
        public int StmCost { get; set; }
        public SWLOR.Game.Server.Service.CombatService.PairedRangedAbilityTraitsSnapshot WeaponTraits { get; init; }
        public IReadOnlyList<Combatant> WeaponTargets { get; init; }
        public IReadOnlyList<int> WeaponDamages { get; init; }
        public bool MovementStarted { get; set; }
    }
}
