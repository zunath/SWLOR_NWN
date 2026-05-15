using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ClarityAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Clarity1(builder);
            Clarity2(builder);

            return builder.Build();
        }

        private static void Clarity1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Clarity1, PerkType.Clarity)
                .Name("Clarity I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Clarity, 45f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(Clarity1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(4);
        }

        private static void Clarity2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Clarity2, PerkType.Clarity)
                .Name("Clarity II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.Clarity, 45f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(Clarity2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(6);
        }

        private static void Clarity1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                var stamina = AbilityEffectScaling.ApplyActiveForceAffinityMagnitude(
                    activator,
                    PercentOf(Stat.GetMaxStamina(friendly), 10));
                Stat.RestoreStamina(friendly, stamina);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Clarity1StatusEffect), 15f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), friendly);
            }
        }

        private static void Clarity2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, false))
            {
                var stamina = AbilityEffectScaling.ApplyActiveForceAffinityMagnitude(
                    activator,
                    PercentOf(Stat.GetMaxStamina(friendly), 18));
                Stat.RestoreStamina(friendly, stamina);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(Clarity2StatusEffect), 15f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), friendly);
            }
        }


        private static int PercentOf(int value, int percent)
        {
            return Math.Max(1, value * percent / 100);
        }
    }
}
