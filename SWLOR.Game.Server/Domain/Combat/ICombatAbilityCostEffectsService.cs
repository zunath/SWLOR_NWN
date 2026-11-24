using SWLOR.Game.Server.Domain.Character;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The cost riders of an ability activation: what an ability costs or primes (the recast reduction, the next skill bonuses,
    /// the master hit chance, the Force FP cost riders and the riders of an HP-cost ability).
    /// </summary>
    public interface ICombatAbilityCostEffectsService
    {
        /// <param name="hitPointsSpent">HP actually paid. Nothing triggers when no HP was paid.</param>
        /// <param name="hitPointCost">Full HP cost before the payment was clipped at 1 HP; 0 when it was paid in full.</param>
        void ApplyHitPointSpendAbilityEffects(uint activator, int hitPointsSpent, int hitPointCost = 0);

        void ApplyAbilityUsedRecastReduction(uint activator, AbilityDetail ability);

        void ApplyAbilityUsedNextSkillAutoAttackDamageBonus(uint activator, AbilityDetail ability);

        void ApplyAbilityUsedNextSkillFPCostAdjustment(uint activator, AbilityDetail ability);

        void ApplyAbilityUsedMasterAbilityHitChance(uint activator);

        void ApplyForceFPCostActivatedEffects(uint activator, AbilityDetail ability);
    }
}
