using SWLOR.Component.Ability.Contracts;
using SWLOR.NWN.API.NWScript.Constants;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Ability.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;

namespace SWLOR.Component.Ability.Definitions.Devices
{
    public class FlashbangDefinitionAbilityDefinition : ExplosiveBaseAbilityDefinition
    {

        public FlashbangDefinitionAbilityDefinition(
            IServiceProvider serviceProvider,
            IStatCalculationService statCalculation)
            : base(serviceProvider, statCalculation)
        {
        }

        public override Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            FlashbangGrenade1(builder);
            FlashbangGrenade2(builder);
            FlashbangGrenade3(builder);

            return builder.Build();
        }
        
        private void Impact(uint activator, uint target, int abReduce)
        {
            if (GetFactionEqual(activator, target))
                return;

            ApplyEffectToObject(DurationType.Temporary, EffectAccuracyDecrease(abReduce), target, 20f);

            CombatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
            EnmityService.ModifyEnmity(activator, target, 250);
        }

        private void FlashbangGrenade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.FlashbangGrenade1, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroupType.FlashbangGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(1)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 2);
                    });
                });
        }

        private void FlashbangGrenade2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.FlashbangGrenade2, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade II")
                .Level(2)
                .HasRecastDelay(RecastGroupType.FlashbangGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 4);
                    });
                });
        }

        private void FlashbangGrenade3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.FlashbangGrenade3, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade III")
                .Level(3)
                .HasRecastDelay(RecastGroupType.FlashbangGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(AnimationType.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffectType.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 6);
                    });
                });
        }
    }
}
