using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Devices
{
    public class FlashbangDefinitionAbilityDefinition : ExplosiveBaseAbilityDefinition
    {

        public FlashbangDefinitionAbilityDefinition(IRandomService random, IItemService itemService, IPerkService perkService, IStatService statService, ICombatService combatService, ICombatPointService combatPointService, IEnmityService enmityService, IStatusEffectService statusEffectService) 
            : base(random, itemService, perkService, statService, combatService, combatPointService, enmityService, statusEffectService)
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

            _combatPointService.AddCombatPoint(activator, target, SkillType.Devices, 3);
            _enmityService.ModifyEnmity(activator, target, 250);
        }

        private void FlashbangGrenade1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.FlashbangGrenade1, PerkType.FlashbangGrenade)
                .Name("Flashbang Grenade I")
                .Level(1)
                .HasRecastDelay(RecastGroup.FlashbangGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(1)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
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
                .HasRecastDelay(RecastGroup.FlashbangGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(2)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
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
                .HasRecastDelay(RecastGroup.FlashbangGrenade, 24f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .UsesAnimation(Animation.ThrowGrenade)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasMaxRange(15f)
                .HasCustomValidation(ExplosiveValidation)
                .HasImpactAction((activator, _, _, location) =>
                {
                    ExplosiveImpact(activator, location, EffectVisualEffect(VisualEffect.Vfx_Fnf_Mystical_Explosion), "explosion1", RadiusSize.Large, (target) =>
                    {
                        Impact(activator, target, 6);
                    });
                });
        }
    }
}
