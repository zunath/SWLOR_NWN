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
    public sealed class ForceChokeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ForceChoke1(builder);

            return builder.Build();
        }

        private static void ForceChoke1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.ForceChoke1, PerkType.ForceChoke)
                .Name("Force Choke")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.ForceChoke, 60f)
                .SkillType(SkillType.Force)
                .UsesImpactAnimation(Animation.ForceChoke)
                .IsSingleTargetAbility()
                .HasMaxRange(15f)
                .RequiresTarget()
                .HasImpactAction(ForceChoke1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .TriggersDarkForceConversion()
                .BreaksStealth()
                .RequirementFP(7);
        }

        private static void ForceChoke1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Force,
                0,
                3,
                typeof(DazedStatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Force,
                targetVisualEffect: VisualEffect.Vfx_Imp_Pulse_Negative,
                afterSuccessfulHit: hitTarget => ApplyForceDamageOverTime(activator, hitTarget));
        }

        private static void ApplyForceDamageOverTime(uint activator, uint target)
        {
            StatusEffect.ApplyStatusEffect(activator, target, typeof(ForceChokeDamageStatusEffect), 12f, CombatDamageType.Force);
        }

    }
}
