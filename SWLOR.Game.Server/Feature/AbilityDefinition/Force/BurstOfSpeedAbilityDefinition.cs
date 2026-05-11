using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class BurstOfSpeedAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            BurstOfSpeed1(builder);
            BurstOfSpeed2(builder);

            return builder.Build();
        }

        private static string Validation(uint target, int tier)
        {
            if (tier == 1 && StatusEffect.HasStatusEffect<BurstOfSpeed2StatusEffect>(target))
            {
                return "Your target is already enhanced by a more powerful effect.";
            }

            return string.Empty;
        }

        private static void Impact<T>(uint activator, uint target)
            where T : IStatusEffect
        {
            StatusEffect.ApplyStatusEffect<T>(activator, target, 600f);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Haste), target);

            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
            Enmity.ModifyEnmityOnAll(activator, 250);
        }

        private static void BurstOfSpeed1(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed1, PerkType.BurstOfSpeed)
                .Name("Burst of Speed I")
                .Level(1)
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(2)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation((activator, target, level, location) => Validation(target, 1))
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact<BurstOfSpeed1StatusEffect>(activator, target);
                });
        }
        private static void BurstOfSpeed2(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed2, PerkType.BurstOfSpeed)
                .Name("Burst of Speed II")
                .Level(2)
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(3)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasCustomValidation((activator, target, level, location) => Validation(target, 2))
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact<BurstOfSpeed2StatusEffect>(activator, target);
                });
        }
    }
}
