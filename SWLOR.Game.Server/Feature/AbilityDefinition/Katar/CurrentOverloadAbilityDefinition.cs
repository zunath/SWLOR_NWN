using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class CurrentOverloadAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            CurrentOverload1(builder);

            return builder.Build();
        }

        private static void CurrentOverload1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.CurrentOverload1, PerkType.CurrentOverload)
                .Name("Current Overload")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.CurrentOverload, 90f)
                .RequiresTarget()
                .HasImpactAction(CurrentOverload1ImpactAction)
                .SkillType(SkillType.Katar)
                .IsSingleTargetAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void CurrentOverload1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var consumedStatus = StatusEffect.HasStatusEffect(target, typeof(PoisonStatusEffect))
                ? typeof(PoisonStatusEffect)
                : StatusEffect.HasStatusEffect(target, typeof(DisorientedStatusEffect))
                    ? typeof(DisorientedStatusEffect)
                    : null;
            var damage = consumedStatus == null ? 35 : 60;
            var statusEffect = consumedStatus == null
                ? null
                : typeof(StunnedStatusEffect);
            var duration = statusEffect == null ? 0 : 3;

            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Katar,
                damage,
                duration,
                statusEffect,
                false,
                damageType: CombatDamageType.Electrical,
                afterSuccessfulHit: hitTarget =>
                {
                    if (consumedStatus != null)
                        StatusEffect.RemoveStatusEffect(hitTarget, consumedStatus, false);
                });
        }
    }
}
