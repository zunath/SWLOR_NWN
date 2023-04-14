using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class EvasiveManeuverAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            EvasiveManeuver1();
            EvasiveManeuver2();
            EvasiveManeuver3();
            EvasiveManeuver4();
            EvasiveManeuver5();

            return _builder.Build();
        }

        private void Impact(uint activator, StatusEffectType statusEffect)
        {
            var master = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Agility, master) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Agility, activator) / 2;
            var totalStat = beastmasterStat + beastStat;

            var duration = 5 * 60f + totalStat * 10;
            StatusEffect.Apply(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Odd), activator);
        }

        private void EvasiveManeuver1()
        {
            _builder.Create(FeatType.EvasiveManeuver1, PerkType.EvasiveManeuver)
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
        private void EvasiveManeuver2()
        {
            _builder.Create(FeatType.EvasiveManeuver2, PerkType.EvasiveManeuver)
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
        private void EvasiveManeuver3()
        {
            _builder.Create(FeatType.EvasiveManeuver3, PerkType.EvasiveManeuver)
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
        private void EvasiveManeuver4()
        {
            _builder.Create(FeatType.EvasiveManeuver4, PerkType.EvasiveManeuver)
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
        private void EvasiveManeuver5()
        {
            _builder.Create(FeatType.EvasiveManeuver5, PerkType.EvasiveManeuver)
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
