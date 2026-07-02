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
    public sealed class HastenAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Hasten1(builder);
            Hasten2(builder);

            return builder.Build();
        }

        private static void Hasten1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Hasten1, PerkType.Hasten)
                .Name("Hasten I")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FollowMe)
                .HasRecastDelay(RecastGroup.Hasten, 60f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(Hasten1ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(5);
        }

        private static void Hasten2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Hasten2, PerkType.Hasten)
                .Name("Hasten II")
                .Level(2)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.FollowMe)
                .HasRecastDelay(RecastGroup.Hasten, 60f)
                .SkillType(SkillType.BeastMastery)
                .IsSingleTargetAbility()
                .HasImpactAction(Hasten2ImpactAction)
                .IsCastedAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void Hasten1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(Hasten1StatusEffect), 30f, VisualEffect.Vfx_Imp_Haste);
        }

        private static void Hasten2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplySelfStatus(activator, typeof(Hasten2StatusEffect), 30f, VisualEffect.Vfx_Imp_Haste);
        }

        private static void ApplySelfStatus(uint activator, Type statusEffect, float duration, VisualEffect visualEffect)
        {
            StatusEffect.ApplyStatusEffect(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(visualEffect), activator);
        }

    }
}
