using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class DamageTakenReactions
    {
        [NWNEventHandler(ScriptName.OnCreatureDamagedAfter)]
        public static void ApplyDamageTakenEffects()
        {
            var defender = OBJECT_SELF;
            var attacker = GetLastDamager(defender);
            var damage = GetTotalDamageDealt();

            DamageTakenReactions.ApplyDamageTakenEffects(defender, attacker, damage);
        }

        public static void ApplyDamageTakenEffects(uint defender, uint attacker, int damage)
        {
            if (!GetIsObjectValid(defender) || GetIsDead(defender) || damage <= 0)
                return;

            var attackPercent = Stat.GetStatAdjustment(defender, StatType.DamageTakenAttackPercentAdjustment);
            var attackDuration = Stat.GetStatAdjustment(defender, StatType.DamageTakenAttackDurationSeconds);
            if (attackPercent != 0 && attackDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.AttackPercentAdjustment,
                    attackPercent,
                    attackDuration,
                    StatType.DamageTakenAttackPercentAdjustment);
            }

            DamageTakenReactions.ApplyLowHPDamageTakenEffects(defender, damage);
            DamageTakenReactions.ApplyDamageTakenNextSkillAbilityDamage(defender);
            DamageTakenReactions.ApplyReversalCutReady(defender);
            CombatActivity.TrackRecentDamageTaken(defender);
            DamageModifierPipeline.ApplyRecentDamageTargetHitEffects(defender, attacker);
        }

        internal static void ApplyDamageTakenNextSkillAbilityDamage(uint defender)
        {
            var skillType = AbilityImpactEffects.GetSkillTypeFromStat(Stat.GetStatAdjustment(
                defender,
                StatType.DamageTakenNextSkillAbilitySkillType));
            var damageBonus = Stat.GetStatAdjustment(defender, StatType.DamageTakenNextSkillAbilityDamageBonus);
            var window = Stat.GetStatAdjustment(defender, StatType.DamageTakenNextSkillAbilityWindowSeconds);
            if (damageBonus <= 0 || window <= 0)
                return;

            AbilityImpactEffects.GrantNextSkillAbilityBonuses(defender, skillType, damageBonus, 0, window);
        }

        public static void ApplyLowHPDamageTakenEffects(uint defender, int damage)
        {
            if (!GetIsObjectValid(defender) || GetIsDead(defender) || damage <= 0)
                return;

            LowHPReactions.ApplyLowHPPhysicalDefenseEffect(defender, damage);
            LowHPReactions.ApplyLowHPEvasionEffect(defender, damage);
            LowHPReactions.ApplyLowHPNextAbilityNoStaminaCostEffect(defender, damage);
            LowHPReactions.ApplyLowHPTemporaryHPEffect(defender, damage);
            LowHPReactions.ApplyLowHPNoSaveTemporaryHPEffect(defender, damage);
            LowHPReactions.ApplyLowHPGuardEffect(defender, damage);
        }

        internal static void ApplyReversalCutReady(uint defender)
        {
            if (Stat.GetStatAdjustment(defender, StatType.TwinBladeDuelistReversalCut) <= 0)
                return;

            var window = Stat.GetStatAdjustment(defender, StatType.TwinBladeDuelistReversalCutWindowSeconds);
            if (window <= 0)
                return;

            var damageBonus = Stat.GetStatAdjustmentExcludingTemporaryModifiers(
                defender,
                StatType.TwinBladeDuelistReversalCutDamageBonus);
            if (damageBonus > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.TwinBladeDuelistReversalCutDamageBonus,
                    damageBonus,
                    window,
                    StatType.TwinBladeDuelistReversalCut);
            }

            var dazedDuration = Stat.GetStatAdjustmentExcludingTemporaryModifiers(
                defender,
                StatType.TwinBladeDuelistReversalCutDazedDurationSeconds);
            if (dazedDuration > 0)
            {
                TemporaryStatModifier.Replace(
                    defender,
                    StatType.TwinBladeDuelistReversalCutDazedDurationSeconds,
                    dazedDuration,
                    window,
                    StatType.TwinBladeDuelistReversalCut);
            }
        }
    }
}
