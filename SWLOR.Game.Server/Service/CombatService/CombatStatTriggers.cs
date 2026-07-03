using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class CombatStatTriggers
    {
        internal static int GetStatusSourceStatAdjustment(uint creature, uint source, StatType statType)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(source))
                return 0;

            var adjustment = 0;
            foreach (var effect in StatusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                if (effect.Source != source)
                    continue;

                if (effect.StatGroup.Stats.TryGetValue(statType, out var value))
                    adjustment += value;
            }

            return adjustment;
        }

        internal static int GetStatusSourcePartyStatAdjustment(uint creature, uint attacker, StatType statType)
        {
            if (!GetIsObjectValid(creature) || !GetIsObjectValid(attacker))
                return 0;

            var adjustment = 0;
            foreach (var effect in StatusEffect.GetCreatureStatusEffects(creature).GetAllEffects())
            {
                if (!GetIsObjectValid(effect.Source) ||
                    (effect.Source != attacker && !Party.IsInParty(effect.Source, attacker)))
                    continue;

                if (effect.StatGroup.Stats.TryGetValue(statType, out var value))
                    adjustment += value;
            }

            return adjustment;
        }

        internal static bool TryUseStatTrigger(uint creature, StatType statType, int cooldownSeconds)
        {
            return CombatStatTriggers.TryUseStatTrigger(creature, statType, TimeSpan.FromSeconds(cooldownSeconds));
        }

        internal static bool TryUseStatTrigger(uint creature, StatType statType, TimeSpan cooldown)
        {
            return CombatState.TryUseStatTrigger(creature, statType, cooldown);
        }

        internal static void RemoveStatTriggerCooldowns(uint creature)
        {
            CombatState.ClearCreature(creature);
        }
    }
}
