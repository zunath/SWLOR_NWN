using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.SkillService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class InnervateAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Innervate1();
            Innervate2();
            Innervate3();
            Innervate4();
            Innervate5();

            return _builder.Build();
        }

        private void Impact(uint activator, uint target, int baseAmount)
        {
            var beastmaster = GetMaster(activator);

            var beastmasterWillBonus = GetAbilityModifier(AbilityType.Willpower, beastmaster) * 4;
            var beastWillBonus = GetAbilityModifier(AbilityType.Willpower, activator) * 4;
            var amount = baseAmount + beastWillBonus + beastmasterWillBonus + Random.D10(1);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);

            Enmity.ModifyEnmityOnAll(activator, 200 + amount);
            CombatPoint.AddCombatPointToAllTagged(beastmaster, SkillType.BeastMastery, 3);
        }

        private void Innervate1()
        {
            _builder.Create(FeatType.Innervate1, PerkType.Innervate)
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

        private void Innervate2()
        {
            _builder.Create(FeatType.Innervate2, PerkType.Innervate)
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

        private void Innervate3()
        {
            _builder.Create(FeatType.Innervate3, PerkType.Innervate)
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

        private void Innervate4()
        {
            _builder.Create(FeatType.Innervate4, PerkType.Innervate)
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

        private void Innervate5()
        {
            _builder.Create(FeatType.Innervate5, PerkType.Innervate)
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