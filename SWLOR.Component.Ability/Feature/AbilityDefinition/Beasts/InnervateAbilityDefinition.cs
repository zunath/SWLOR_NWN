using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.Model;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Enums;
using SWLOR.Shared.Domain.Model;

namespace SWLOR.Component.Ability.Feature.AbilityDefinition.Beasts
{
    public class InnervateAbilityDefinition : IAbilityListDefinition
    {
        private readonly IRandomService _random;
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;

        public InnervateAbilityDefinition(
            IRandomService random, 
            ICombatPointService combatPointService, 
            IEnmityService enmityService)
        {
            _random = random;
            _combatPointService = combatPointService;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            Innervate1(builder);
            Innervate2(builder);
            Innervate3(builder);
            Innervate4(builder);
            Innervate5(builder);

            return builder.Build();
        }

        private void Impact(uint activator, uint target, int baseAmount)
        {
            var beastmaster = GetMaster(activator);

            var beastmasterWillBonus = GetAbilityModifier(AbilityType.Willpower, beastmaster) * 4;
            var beastWillBonus = GetAbilityModifier(AbilityType.Willpower, activator) * 4;
            var amount = baseAmount + beastWillBonus + beastmasterWillBonus + _random.D10(1);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);

            _enmityService.ModifyEnmityOnAll(activator, 200 + amount);
            _combatPointService.AddCombatPointToAllTagged(beastmaster, SkillType.BeastMastery, 3);
        }

        private void Innervate1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Innervate1, PerkType.Innervate)
                .Name("Innervate I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Innervate, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(3)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 30);
                });
        }

        private void Innervate2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Innervate2, PerkType.Innervate)
                .Name("Innervate II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Innervate, 30f)
                .HasActivationDelay(2f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 40);
                });
        }

        private void Innervate3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Innervate3, PerkType.Innervate)
                .Name("Innervate III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Innervate, 30f)
                .HasActivationDelay(3f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 60);
                });
        }

        private void Innervate4(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Innervate4, PerkType.Innervate)
                .Name("Innervate IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.Innervate, 30f)
                .HasActivationDelay(3f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 80);
                });
        }

        private void Innervate5(IAbilityBuilder builder)
        {
            builder.Create(FeatType.Innervate5, PerkType.Innervate)
                .Name("Innervate V")
                .Level(5)
                .HasRecastDelay(RecastGroup.Innervate, 30f)
                .HasActivationDelay(4f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 120);
                });
        }
    }
}
