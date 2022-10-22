using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceDeathAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ForceDeath1();
            ForceDeath2();
            ForceDeath3();

            return _builder.Build();
        }


        private void Impact(uint activator, uint target, int baseAmount)
        {
            var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);
            var amount = baseAmount + willBonus * 10;

            ApplyEffectToObject(DurationType.Instant, EffectHeal(amount), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_M), target);

            Enmity.ModifyEnmityOnAll(activator, 300 + amount);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private void ForceDeath1()
        {
            _builder.Create(FeatType.ForceDeath1, PerkType.ForceDeath)
                .Name("Force Death I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Benevolence, 6f)
                .HasActivationDelay(6f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 15);
                });
        }

        private void ForceDeath2()
        {
            _builder.Create(FeatType.ForceDeath2, PerkType.ForceDeath)
                .Name("Force Death II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Benevolence, 6f)
                .HasActivationDelay(6f)
                .RequirementFP(8)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 30);
                });
        }

        private void ForceDeath3()
        {
            _builder.Create(FeatType.ForceDeath3, PerkType.ForceDeath)
                .Name("Force Death III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Benevolence, 6f)
                .HasActivationDelay(6f)
                .RequirementFP(10)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 45);
                });
        }

    }
}