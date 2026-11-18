using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The take-the-max rule of the Leadership damage-reduction family, over one creature's tracked effects. No state, no engine
    /// and no services: the status effect service calls it after an effect is added and after one is removed.
    /// </summary>
    public static class LeadershipDamageReductionRules
    {
        /// <summary>
        /// Enforces take-the-max behavior across the Leadership damage-reduction family
        /// independently for each damage channel. A physical/Force-only effect can supersede
        /// Hold the Line in those channels without disabling Hold the Line's protection against
        /// elemental and other damage. Weaker members remain applied so their unrelated stats
        /// continue to function, and every channel is recomputed after application or removal.
        /// </summary>
        public static void ReconcileLeadershipDamageReduction(CreatureStatusEffect creatureEffects)
        {
            var family = creatureEffects.GetAllEffects()
                .Where(effect => effect is ILeadershipDamageReductionStatusEffect)
                .ToList();

            if (family.Count == 0)
                return;

            var winnerByStat = family
                .SelectMany(effect =>
                    ((ILeadershipDamageReductionStatusEffect)effect).LeadershipDamageReductionStats.Keys)
                .Distinct()
                .ToDictionary(
                    statType => statType,
                    statType => family
                        .Where(effect =>
                            ((ILeadershipDamageReductionStatusEffect)effect)
                            .LeadershipDamageReductionStats.ContainsKey(statType))
                        .OrderBy(effect =>
                            ((ILeadershipDamageReductionStatusEffect)effect)
                            .LeadershipDamageReductionStats[statType])
                        .First());

            foreach (var effect in family)
            {
                var leadershipDamageReductionEffect = (ILeadershipDamageReductionStatusEffect)effect;
                ApplyLeadershipDamageReductionContribution(
                    creatureEffects,
                    effect,
                    leadershipDamageReductionEffect,
                    winnerByStat);
            }
        }

        private static void ApplyLeadershipDamageReductionContribution(
            CreatureStatusEffect creatureEffects,
            IStatusEffect effect,
            ILeadershipDamageReductionStatusEffect leadershipDamageReductionEffect,
            IReadOnlyDictionary<StatType, IStatusEffect> winnerByStat)
        {
            var isAlreadyCorrect = leadershipDamageReductionEffect.LeadershipDamageReductionStats.All(pair =>
                effect.StatGroup.Stats.TryGetValue(pair.Key, out var current) &&
                current == (ReferenceEquals(winnerByStat[pair.Key], effect) ? pair.Value : 0));

            if (isAlreadyCorrect)
                return;

            creatureEffects.Remove(effect);

            foreach (var (statType, nominalValue) in leadershipDamageReductionEffect.LeadershipDamageReductionStats)
            {
                effect.StatGroup.Stats[statType] = ReferenceEquals(winnerByStat[statType], effect)
                    ? nominalValue
                    : 0;
            }

            creatureEffects.Add(effect);
        }
    }
}
