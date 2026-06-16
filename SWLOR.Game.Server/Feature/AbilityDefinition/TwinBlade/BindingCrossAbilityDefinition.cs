using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class BindingCrossAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BindingCross1(builder);
            BindingCross2(builder);

            return builder.Build();
        }

        private static void BindingCross1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BindingCross1, PerkType.BindingCross)
                .Name("Binding Cross I")
                .Level(1)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .HasRecastDelay(RecastGroup.BindingCross, 60f)
                .RequiresTarget()
                .HasImpactAction(BindingCross1ImpactAction)
                .IsSingleTargetAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(8);
        }

        private static void BindingCross2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.BindingCross2, PerkType.BindingCross)
                .Name("Binding Cross II")
                .Level(2)
                .SkillType(SkillType.TwinBlade)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CrossCut)
                .HasRecastDelay(RecastGroup.BindingCross, 60f)
                .RequiresTarget()
                .HasImpactAction(BindingCross2ImpactAction)
                .IsSingleTargetAbility()
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void BindingCross1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyBindingCross(activator, target, targetLocation, 10, 12, 0);
        }

        private static void BindingCross2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyBindingCross(activator, target, targetLocation, 18, 20, 10);
        }

        private static void ApplyBindingCross(
            uint activator,
            uint target,
            Location targetLocation,
            int baseDamage,
            int hamstringDuration,
            int exposedDuration)
        {
            var damage = 0;
            for (var hit = 0; hit < 2; hit++)
            {
                damage += Ability.ApplyCombatImpact(
                    activator,
                    target,
                    targetLocation,
                    SkillType.TwinBlade,
                    baseDamage,
                    hamstringDuration,
                    typeof(HamstringStatusEffect),
                    false);
            }

            if (damage > 0 && exposedDuration > 0)
            {
                StatusEffect.ApplyStatusEffect(
                    activator,
                    target,
                    typeof(ExposedStatusEffect),
                    exposedDuration,
                    CombatDamageType.Physical);
            }
        }
    }
}
