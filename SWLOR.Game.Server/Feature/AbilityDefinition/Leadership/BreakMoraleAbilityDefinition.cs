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

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Leadership
{
    public sealed class BreakMoraleAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BreakMorale1(builder);
            BreakMorale2(builder);

            return builder.Build();
        }

        private static void BreakMorale1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BreakMorale1, PerkType.BreakMorale)
                .Name("Break Morale I")
                .Level(1)
                .HasActivationDelay(0.5f)
                .HasRecastDelay(RecastGroup.BreakMorale, 90f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(BreakMorale1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void BreakMorale2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BreakMorale2, PerkType.BreakMorale)
                .Name("Break Morale II")
                .Level(2)
                .HasActivationDelay(0.5f)
                .HasRecastDelay(RecastGroup.BreakMorale, 90f)
                .SkillType(SkillType.Leadership)
                .IsAreaAbility()
                .HasImpactAction(BreakMorale2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(9);
        }

        private static void BreakMorale1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = (int)Math.Ceiling(LeadershipAbilityEffects.ApplyVanguardCommandDurationBonus(activator, 12f));

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Leadership,
                0,
                duration,
                typeof(FlashStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                radius,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                statusEffectFactory: () => new FlashStatusEffect(ScaleSocialPenalty(activator, 8, 10)),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Sonic,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst);
        }

        private static void BreakMorale2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var radius = LeadershipAbilityEffects.GetLeadershipCommandRadius(activator);
            var duration = (int)Math.Ceiling(LeadershipAbilityEffects.ApplyVanguardCommandDurationBonus(activator, 12f));

            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Leadership,
                0,
                duration,
                typeof(WeakenedStatusEffect),
                CombatImpactAreaShape.Sphere,
                0f,
                radius,
                0f,
                Array.Empty<Type>(),
                centerOnActivator: true,
                statusEffectFactory: () => new FlashStatusEffect(ScaleSocialPenalty(activator, 12, 14)),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Sonic,
                areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst,
                additionalStatusEffectFactories: new Func<IStatusEffect>[]
                {
                    () => new WeakenedStatusEffect(ScaleSocialPenalty(activator, 8, 10)),
                });
        }

        private static int ScaleSocialPenalty(uint activator, int baseValue, int maximumValue)
        {
            return AbilityEffectScaling.ScaleValueBySourceSocial(activator, baseValue, maximumValue);
        }
    }
}
