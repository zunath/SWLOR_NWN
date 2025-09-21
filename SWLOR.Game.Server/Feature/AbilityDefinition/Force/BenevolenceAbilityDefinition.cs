using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class BenevolenceAbilityDefinition : IAbilityListDefinition
    {
        private readonly IRandomService _random;
        private readonly AbilityBuilder _builder = new();
        private readonly IStatService _statService;
        private const string BeneRegen = "FORCE_BENEVOLENCE";

        public BenevolenceAbilityDefinition(IRandomService random, IStatService statService)
        {
            _random = random;
            _statService = statService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Benevolence1();
            Benevolence2();
            Benevolence3();

            return _builder.Build();
        }

        private void Impact(uint activator, uint target, int baseAmount)
        {
            var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);
            var targetBonus = willBonus;
            if (target != activator && _statService.GetCurrentFP(activator) >= 16)
            {
                RemoveEffectByTag(target, BeneRegen);

                var willRestore = (willBonus / 2) * 4;
                var duration = 90f + (willBonus * 60f);
                var effect = EffectRegenerate(willRestore, 24f);
                _statService.ReduceFP(activator, 10);
                _statService.ReduceStamina(activator, willRestore);
                _statService.RestoreFP(target, willRestore);
                _statService.RestoreStamina(target, willRestore);
                targetBonus = willBonus * 4;

                effect = TagEffect(effect, BeneRegen);
                ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
            }
            var willHeal = baseAmount + (targetBonus * 4) + _random.D4(targetBonus);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(willHeal), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);

            Enmity.ModifyEnmityOnAll(activator, 150 + (willHeal / 4));
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private void Benevolence1()
        {
            _builder.Create(FeatType.Benevolence1, PerkType.Benevolence)
                .Name("Benevolence I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Benevolence, 8f)
                .HasActivationDelay(2f)
                .RequirementFP(2)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 30);
                });
        }

        private void Benevolence2()
        {
            _builder.Create(FeatType.Benevolence2, PerkType.Benevolence)
                .Name("Benevolence II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Benevolence, 8f)
                .HasActivationDelay(2f)
                .RequirementFP(4)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 60);
                });
        }

        private void Benevolence3()
        {
            _builder.Create(FeatType.Benevolence3, PerkType.Benevolence)
                .Name("Benevolence III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Benevolence, 8f)
                .HasActivationDelay(2f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 90);
                });
        }
    }
}
