using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beastmaster
{
    public sealed class ExecutePreyAbilityDefinition : IAbilityListDefinition
    {
        private const float LowHPThreshold = 0.35f;
        private const int LowHPDamagePercentBonus = 50;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ExecutePrey1(builder);

            return builder.Build();
        }

        private static void ExecutePrey1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ExecutePrey1, PerkType.ExecutePrey)
                .Name("Execute Prey")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ExecutePrey, 60f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(ExecutePrey1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void ExecutePrey1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.BeastMastery,
                30,
                12,
                null,
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Physical,
                targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small,
                damagePercentAdjustment: creature => IsLowHP(creature) ? LowHPDamagePercentBonus : 0);
        }

        private static bool IsLowHP(uint target)
        {
            return GetIsObjectValid(target) &&
                   GetMaxHitPoints(target) > 0 &&
                   GetCurrentHitPoints(target) <= GetMaxHitPoints(target) * LowHPThreshold;
        }

        private static void HealPercent(uint activator, uint target, SkillType skill, int percent)
        {
            var ability = skill switch
            {
                SkillType.Leadership => AbilityType.Social,
                SkillType.Devices => AbilityType.Perception,
                SkillType.BeastMastery => AbilityType.Might,
                _ => AbilityType.Willpower
            };
            var baseAmount = PercentOf(GetMaxHitPoints(target), percent);
            var amount = SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ScaleDirectEffect(baseAmount, GetAbilityScore(activator, ability));
            amount = Stat.ApplyHealingReceivedAdjustment(target, amount);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);
        }

        private static int PercentOf(int value, int percent)
        {
            return Math.Max(1, value * percent / 100);
        }
    }
}
