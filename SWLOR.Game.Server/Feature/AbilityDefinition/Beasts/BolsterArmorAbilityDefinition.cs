using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class BolsterArmorAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            BolsterArmor1();
            BolsterArmor2();
            BolsterArmor3();
            BolsterArmor4();
            BolsterArmor5();

            return _builder.Build();
        }

        private void Impact(uint activator, StatusEffectType statusEffect)
        {
            var master = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Vitality, master) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Vitality, activator) / 2;
            var totalStat = beastmasterStat + beastStat;

            var duration = 5 * 60f + totalStat * 10;
            StatusEffect.Apply(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), activator);
        }

        private void BolsterArmor1()
        {
            _builder.Create(FeatType.BolsterArmor1, PerkType.BolsterArmor)
                .Name("Bolster Armor I")
                .Level(1)
                .HasRecastDelay(RecastGroup.BolsterArmor, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterArmor1);
                });
        }
        private void BolsterArmor2()
        {
            _builder.Create(FeatType.BolsterArmor2, PerkType.BolsterArmor)
                .Name("Bolster Armor II")
                .Level(2)
                .HasRecastDelay(RecastGroup.BolsterArmor, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterArmor2);
                });
        }
        private void BolsterArmor3()
        {
            _builder.Create(FeatType.BolsterArmor3, PerkType.BolsterArmor)
                .Name("Bolster Armor III")
                .Level(3)
                .HasRecastDelay(RecastGroup.BolsterArmor, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterArmor3);
                });
        }
        private void BolsterArmor4()
        {
            _builder.Create(FeatType.BolsterArmor4, PerkType.BolsterArmor)
                .Name("Bolster Armor IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.BolsterArmor, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterArmor4);
                });
        }
        private void BolsterArmor5()
        {
            _builder.Create(FeatType.BolsterArmor5, PerkType.BolsterArmor)
                .Name("Bolster Armor V")
                .Level(5)
                .HasRecastDelay(RecastGroup.BolsterArmor, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterArmor5);
                });
        }

    }
}
