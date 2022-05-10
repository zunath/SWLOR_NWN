using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

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

        private static void Impact(uint activator, uint target, StatusEffectType type)
        {
            StatusEffect.Apply(activator, target, type, 600f);
        }

        private static void BurstOfSpeed1(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed1, PerkType.BurstOfSpeed)
                .Name("Burst of Speed I")
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(2)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, StatusEffectType.BurstOfSpeed1);
                });
        }
        private static void BurstOfSpeed2(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed2, PerkType.BurstOfSpeed)
                .Name("Burst of Speed II")
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(3)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, StatusEffectType.BurstOfSpeed2);
                });
        }
    }
}