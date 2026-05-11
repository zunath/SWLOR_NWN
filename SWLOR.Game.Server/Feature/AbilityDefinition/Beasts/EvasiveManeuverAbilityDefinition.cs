using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

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

            return _builder.Build();
        }

        private void Impact(uint activator, Type statusEffect)
        {
            var master = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Agility, master) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Agility, activator) / 2;
            var totalStat = beastmasterStat + beastStat;

            var duration = 5 * 60f + totalStat * 10;
            StatusEffect.ApplyStatusEffect(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Fnf_Howl_Odd), activator);
        }

        private void EvasiveManeuver1()
        {
            _builder
                .Create(FeatType.EvasiveManeuver1, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver I")
                .Level(1)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, typeof(EvasiveManeuver1StatusEffect));
                });
        }
        private void EvasiveManeuver2()
        {
            _builder
                .Create(FeatType.EvasiveManeuver2, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver II")
                .Level(2)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, typeof(EvasiveManeuver2StatusEffect));
                });
        }
        private void EvasiveManeuver3()
        {
            _builder
                .Create(FeatType.EvasiveManeuver3, PerkType.EvasiveManeuver)
                .Name("Evasive Maneuver III")
                .Level(3)
                .HasRecastDelay(RecastGroup.EvasiveManeuver, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, typeof(EvasiveManeuver3StatusEffect));
                });
        }

    }
}
