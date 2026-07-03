using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service.CombatService
{
    public static class AbilityImpactAreaDamageEffects
    {
        internal static void ApplyFoggyMindResourceDrain(
            uint activator,
            uint target,
            AbilityDetail ability)
        {
            if (ability == null ||
                !ability.IsHostileAbility ||
                !GetIsObjectValid(target) ||
                !StatusEffect.HasStatusEffect(target, typeof(FoggyMindStatusEffect)))
            {
                return;
            }

            var fpDrain = Stat.GetStatAdjustment(activator, StatType.AbilityResourceDrainFoggyMindFP);
            if (fpDrain > 0)
                Stat.ReduceFP(target, fpDrain);

            var staminaDrain = Stat.GetStatAdjustment(activator, StatType.AbilityResourceDrainFoggyMindStamina);
            if (staminaDrain > 0)
                Stat.ReduceStamina(target, staminaDrain);
        }

        internal static void ApplyAreaAbilityFragmentation(
            uint activator,
            uint target,
            AbilityDetail ability,
            SkillType skillType,
            CombatDamageType damageType)
        {
            if (ability == null || !ability.IsAreaAbility || !GetIsObjectValid(target))
                return;

            var damage = Stat.GetStatAdjustment(activator, StatType.AreaAbilityFragmentationDamage);
            var duration = Stat.GetStatAdjustment(activator, StatType.AreaAbilityFragmentationDurationSeconds);
            var pulse = Stat.GetStatAdjustment(activator, StatType.AreaAbilityFragmentationPulseSeconds);
            if (damage <= 0 || duration <= 0 || pulse <= 0)
                return;

            StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new FragmentationStatusEffect(damage, pulse),
                duration,
                damageType);
        }

        internal static void ApplyRicochetDamage(
            uint activator,
            uint target,
            CombatDamageType damageType,
            StatType damageStatType,
            StatType maximumTargetsStatType,
            StatType cooldownStatType = StatType.Invalid)
        {
            var bonus = Stat.GetStatAdjustment(activator, damageStatType);
            var maximumTargets = Stat.GetStatAdjustment(activator, maximumTargetsStatType);
            if (bonus <= 0 || maximumTargets <= 0)
                return;

            if (cooldownStatType != StatType.Invalid)
            {
                var cooldown = Stat.GetStatAdjustment(activator, cooldownStatType);
                if (!CombatStatTriggers.TryUseStatTrigger(activator, damageStatType, cooldown))
                    return;
            }

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), 5f, maximumTargets, OBJECT_INVALID))
            {
                if (nearby == target)
                    continue;

                TriggeredCombatEffects.ApplyTriggeredDamage(activator, nearby, bonus, damageType);
            }
        }

        internal static void ApplyClusterStormDamage(
            uint activator,
            uint target,
            CombatDamageType damageType)
        {
            var bonus = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierClusterStormDamageBonus);
            var maximumTargets = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierClusterStormMaximumTargets);
            if (bonus <= 0 || maximumTargets <= 0)
                return;

            foreach (var nearby in AbilityTargeting.GetHostileTargetsNearLocation(activator, GetLocation(target), 5f, maximumTargets, OBJECT_INVALID))
            {
                if (nearby == target)
                    continue;

                TriggeredCombatEffects.ApplyTriggeredDamage(activator, nearby, bonus, damageType);
            }
        }

        internal static void ApplySaturationToss(uint activator, uint target)
        {
            if (Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationToss) <= 0)
                return;

            var duration = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossDurationSeconds);
            var damage = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossDamage);
            var pulse = Stat.GetStatAdjustment(activator, StatType.ThrowingBombardierSaturationTossPulseSeconds);
            if (duration <= 0 || damage <= 0 || pulse <= 0)
                return;

            var applied = StatusEffect.ApplyStatusEffect(
                activator,
                target,
                new SaturationTossStatusEffect(damage, pulse),
                duration,
                CombatDamageType.Fire);
            if (!applied)
                return;

            ApplyEffectAtLocation(
                DurationType.Temporary,
                EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Fire),
                GetLocation(target),
                duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S), target);
        }

    }
}
