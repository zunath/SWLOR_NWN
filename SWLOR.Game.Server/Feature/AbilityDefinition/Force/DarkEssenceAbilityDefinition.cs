using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using System.Collections.Generic;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class DarkEssenceAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private const string DEssRegen = "DARK_ESSENCE";
        private const string DEssTempHP = "DARK_ESSENCE_TEMP";

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            DarkEssence1();
            DarkEssence2();
            DarkEssence3();

            return _builder.Build();
        }

        private void Impact(uint activator, uint target, int baseAmount)
        {
            var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);
            var duration = 24f + (willBonus * 6f);
            if (activator == target)
            {
                RemoveEffectByTag(target, DEssRegen);
                var willRegen = (baseAmount / 2) + willBonus;
                var effect = EffectRegenerate(willRegen, duration);
                var regen = TagEffect(effect, DEssRegen);

                ApplyEffectToObject(DurationType.Temporary, regen, target, duration);
            }

            RemoveEffectByTag(activator, DEssTempHP);
            var tempHP = EffectTemporaryHitpoints(baseAmount + (willBonus * 12) + Random.D10(willBonus));
            var tempHPEffect = TagEffect(tempHP, DEssTempHP);

            ApplyEffectToObject(DurationType.Temporary, tempHPEffect, target, duration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Head_Evil), target);

            Enmity.ModifyEnmityOnAll(activator, 150 + (baseAmount + willBonus / 4));
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
        }

        private void DarkEssence1()
        {
            _builder.Create(FeatType.DarkEssence1, PerkType.DarkEssence)
                .Name("Dark Essence I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Benevolence, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 30);
                });
        }

        private void DarkEssence2()
        {
            _builder.Create(FeatType.DarkEssence2, PerkType.DarkEssence)
                .Name("Dark Essence II")
                .Level(2)
                .HasRecastDelay(RecastGroup.DarkEssence, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(8)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 60);
                });
        }

        private void DarkEssence3()
        {
            _builder.Create(FeatType.DarkEssence3, PerkType.DarkEssence)
                .Name("Dark Essence III")
                .Level(3)
                .HasRecastDelay(RecastGroup.DarkEssence, 12f)
                .HasActivationDelay(2f)
                .RequirementFP(10)
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
