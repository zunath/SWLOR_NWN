using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class BolsterAttackAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            BolsterAttack1();
            BolsterAttack2();
            BolsterAttack3();
            BolsterAttack4();
            BolsterAttack5();

            return _builder.Build();
        }

        private void Impact(uint activator, StatusEffectType statusEffect)
        {
            var master = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Might, master) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Might, activator) / 2;
            var totalStat = beastmasterStat + beastStat;

            var duration = 5 * 60f + totalStat * 10;
            StatusEffect.Apply(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Flame_S), activator);
        }

        private void BolsterAttack1()
        {
            _builder.Create(FeatType.BolsterAttack1, PerkType.BolsterAttack)
                .Name("Bolster Attack I")
                .Level(1)
                .HasRecastDelay(RecastGroup.BolsterAttack, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterAttack1);
                });
        }
        private void BolsterAttack2()
        {
            _builder.Create(FeatType.BolsterAttack2, PerkType.BolsterAttack)
                .Name("Bolster Attack II")
                .Level(2)
                .HasRecastDelay(RecastGroup.BolsterAttack, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterAttack2);
                });
        }
        private void BolsterAttack3()
        {
            _builder.Create(FeatType.BolsterAttack3, PerkType.BolsterAttack)
                .Name("Bolster Attack III")
                .Level(3)
                .HasRecastDelay(RecastGroup.BolsterAttack, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterAttack3);
                });
        }
        private void BolsterAttack4()
        {
            _builder.Create(FeatType.BolsterAttack4, PerkType.BolsterAttack)
                .Name("Bolster Attack IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.BolsterAttack, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterAttack4);
                });
        }
        private void BolsterAttack5()
        {
            _builder.Create(FeatType.BolsterAttack5, PerkType.BolsterAttack)
                .Name("Bolster Attack V")
                .Level(5)
                .HasRecastDelay(RecastGroup.BolsterAttack, 60f)
                .HasActivationDelay(2f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, StatusEffectType.BolsterAttack5);
                });
        }

    }
}
