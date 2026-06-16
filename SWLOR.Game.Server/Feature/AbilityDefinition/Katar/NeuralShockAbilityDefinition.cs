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
    public class NeuralShockAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            NeuralShock1(builder);

            return builder.Build();
        }

        private static void NeuralShock1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.NeuralShock1, PerkType.NeuralShock)
                .Name("Neural Shock")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.NeuralShock, 60f)
                .RequiresTarget()
                .HasImpactAction(NeuralShock1ImpactAction)
                .SkillType(SkillType.Katar)
                .IsSingleTargetAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void NeuralShock1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var isDisoriented = StatusEffect.HasStatusEffect(target, typeof(DisorientedStatusEffect));
            var statusEffect = isDisoriented
                ? typeof(DazedStatusEffect)
                : null;
            var duration = statusEffect == null ? 0 : 3;

            Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.Katar, 20, duration, statusEffect, false, damageType: CombatDamageType.Electrical);
        }
    }
}
