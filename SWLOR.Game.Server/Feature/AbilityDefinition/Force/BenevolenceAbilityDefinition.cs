using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class BenevolenceAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

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
            if (target != activator && Stat.GetCurrentFP(activator) >= 16)
            {
                var willRestore = (willBonus / 2);
                Stat.ReduceFP(activator, 10);
                Stat.ReduceStamina(activator, willRestore);
                Stat.RestoreFP(target, willRestore);
                Stat.RestoreStamina(target, willRestore);
                ApplyEffectToObject(DurationType.Instant, EffectRegenerate(willRestore * 4, 24f), target);
                targetBonus = willBonus * 4;
            }
            var willHeal = baseAmount + targetBonus * 4 + Random.D10(targetBonus * 4);

            ApplyEffectToObject(DurationType.Instant, EffectHeal(willHeal), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);

            Enmity.ModifyEnmityOnAll(activator, 300 + (willHeal / 3));
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
