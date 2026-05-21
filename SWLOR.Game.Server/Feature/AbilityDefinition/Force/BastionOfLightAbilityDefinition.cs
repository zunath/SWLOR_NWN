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
    public sealed class BastionOfLightAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BastionOfLight1(builder);

            return builder.Build();
        }

        private static void BastionOfLight1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BastionOfLight1, PerkType.BastionOfLight)
                .Name("Bastion of Light")
                .Level(1)
                .HasActivationDelay(1.5f)
                .HasRecastDelay(RecastGroup.BastionOfLight, 120f)
                .SkillType(SkillType.Force)
                .IsAreaAbility()
                .HasImpactAction(BastionOfLight1ImpactAction)
                .HasTargetingSphere(
                    Spell.BastionOfLight1,
                    5f,
                    AbilityTargetingFlags.HelpsAllies | AbilityTargetingFlags.OriginOnSelf)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementFP(8);
        }

        private static void BastionOfLight1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            foreach (var friendly in SWLOR.Game.Server.Feature.AbilityDefinition.AbilityTargeting.GetFriendlyTargets(activator, target, true))
            {
                SWLOR.Game.Server.Feature.AbilityDefinition.AbilityEffectScaling.ApplyTemporaryHPPercent(activator, friendly, 10, 20f);
                StatusEffect.ApplyStatusEffect(activator, friendly, typeof(BastionOfLight1StatusEffect), 20f);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), friendly);
            }
            LightGuardianPowerSupport.ApplyDeflectivePresence(activator);
        }
    }
}
