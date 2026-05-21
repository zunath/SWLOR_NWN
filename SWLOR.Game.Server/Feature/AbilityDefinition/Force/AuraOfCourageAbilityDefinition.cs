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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class AuraOfCourageAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            AuraOfCourage1(builder);

            return builder.Build();
        }

        private static void AuraOfCourage1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.AuraOfCourage1, PerkType.AuraOfCourage)
                .Name("Courageous Resolve")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.AuraOfCourage, 60f)
                .SkillType(SkillType.Force)
                .IsAreaAbility()
                .HasImpactAction(AuraOfCourage1ImpactAction)
                .HasTargetingSphere(
                    Spell.CourageousResolve1,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(5);
        }

        private static void AuraOfCourage1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true))
            {
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(AuraOfCourage1StatusEffect), 30f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Holy_Aid), friendly);
            }
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }


    }
}
