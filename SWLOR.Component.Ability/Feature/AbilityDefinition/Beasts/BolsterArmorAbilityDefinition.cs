using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class BolsterArmorAbilityDefinition : IAbilityListDefinition
    {
        private readonly IStatusEffectService _statusEffectService;

        public BolsterArmorAbilityDefinition(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            BolsterArmor1(builder);
            BolsterArmor2(builder);
            BolsterArmor3(builder);
            BolsterArmor4(builder);
            BolsterArmor5(builder);

            return builder.Build();
        }

        private void Impact(uint activator, StatusEffectType statusEffect)
        {
            var master = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Vitality, master) / 2;
            var beastStat = GetAbilityModifier(AbilityType.Vitality, activator) / 2;
            var totalStat = beastmasterStat + beastStat;

            var duration = 5 * 60f + totalStat * 10;
            _statusEffectService.Apply(activator, activator, statusEffect, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), activator);
        }

        private void BolsterArmor1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterArmor1, PerkType.BolsterArmor)
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
        private void BolsterArmor2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterArmor2, PerkType.BolsterArmor)
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
        private void BolsterArmor3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterArmor3, PerkType.BolsterArmor)
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
        private void BolsterArmor4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterArmor4, PerkType.BolsterArmor)
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
        private void BolsterArmor5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.BolsterArmor5, PerkType.BolsterArmor)
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
