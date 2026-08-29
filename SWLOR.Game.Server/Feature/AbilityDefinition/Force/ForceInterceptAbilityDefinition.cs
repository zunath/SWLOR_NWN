using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class ForceInterceptAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceIntercept1(builder);

            return builder.Build();
        }

        private static void ForceIntercept1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceIntercept1, PerkType.ForceIntercept)
                .Name("Force Intercept")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.ForceLeap)
                .PlaysSoundOnImpact("ksfx_frc_speed")
                .HasRecastDelay(RecastGroup.ForceIntercept, 24f)
                .SkillType(SkillType.Force)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasCustomValidation((activator, target, _, _) =>
                    AbilityTargeting.ValidateFriendlyTarget(activator, target, false))
                .HasImpactAction(ForceIntercept1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void ForceIntercept1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var friendly = AbilityTargeting.ResolveFriendlyTarget(activator, target, false);
            if (!GetIsObjectValid(friendly))
                return;

            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Summon_Monster_1), activator);
            AssignCommand(activator, () => ActionJumpToObject(friendly));
            StatusEffect.ApplyStatusEffect(activator, friendly, typeof(ForceIntercept1StatusEffect), 30f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            LightGuardianPowerSupport.ApplyCourageousResolve(activator);
        }


    }
}
