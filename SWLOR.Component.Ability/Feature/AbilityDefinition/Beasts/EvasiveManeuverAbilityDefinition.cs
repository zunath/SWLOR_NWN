using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class EvasiveManeuverAbilityDefinition : IAbilityListDefinition
    {
        private readonly IStatusEffectService _statusEffectService;

        public EvasiveManeuverAbilityDefinition(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            EvasiveManeuver1(builder);
            EvasiveManeuver2(builder);
            EvasiveManeuver3(builder);
            EvasiveManeuver4(builder);
            EvasiveManeuver5(builder);

            return builder.Build();
        }

        private void Impact(uint activator, StatusEffectType statusEffect)
        {
            var master = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Agility, master) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Agility, activator) / 2;
            var totalStat = beastmasterStat + beastStat;

            var duration = 5 * 60f + totalStat * 10;
            _statusEffectService.Apply(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Odd), activator);
        }

        private void EvasiveManeuver1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.EvasiveManeuver1, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver I")
                .Level(1)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.EvasiveManeuver1);
                });
        }
        private void EvasiveManeuver2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.EvasiveManeuver2, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver II")
                .Level(2)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.EvasiveManeuver2);
                });
        }
        private void EvasiveManeuver3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.EvasiveManeuver3, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver III")
                .Level(3)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.EvasiveManeuver3);
                });
        }
        private void EvasiveManeuver4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.EvasiveManeuver4, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.EvasiveManeuver4);
                });
        }
        private void EvasiveManeuver5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.EvasiveManeuver5, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver V")
                .Level(5)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.EvasiveManeuver5);
                });
        }

    }
}
