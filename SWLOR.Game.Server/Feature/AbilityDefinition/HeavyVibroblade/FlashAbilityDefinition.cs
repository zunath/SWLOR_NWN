using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class FlashAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Flash(builder);

            return builder.Build();
        }

        private static void Flash(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Flash1, PerkType.Flash)
                .Name("Flash")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.Flash, 45f)
                .HasTargetingSphere(
                    Spell.Flash1,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        SkillType.HeavyVibroblade,
                        0,
                        30,
                        typeof(FlashStatusEffect),
                        CombatImpactAreaShape.Sphere,
                        0.25f,
                        5f,
                        centerOnActivator: true,
                        statusEffectFactory: () => new FlashStatusEffect(20),
                        targetVisualEffect: VisualEffect.Vfx_Imp_Dazed_S,
                        areaVisualEffect: VisualEffect.Vfx_Fnf_Sound_Burst,
                        damagePercentAdjustment: _ => -100,
                        enmityBonus: 650,
                        canCritical: false);
                })
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .IsHostileAbility()
                .IsAreaAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }
    }
}
