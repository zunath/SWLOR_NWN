﻿//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
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
            BurstOfSpeed3(builder);
            BurstOfSpeed4(builder);
            BurstOfSpeed5(builder);

            return builder.Build();
        }

        private static void BurstOfSpeed1(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed1, PerkType.BurstOfSpeed)
                .Name("Burst of Speed I")
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(2)
                .IsConcentrationAbility(StatusEffectType.BurstOfSpeed1)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private static void BurstOfSpeed2(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed2, PerkType.BurstOfSpeed)
                .Name("Burst of Speed II")
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(3)
                .IsConcentrationAbility(StatusEffectType.BurstOfSpeed2)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private static void BurstOfSpeed3(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed3, PerkType.BurstOfSpeed)
                .Name("Burst of Speed III")
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(4)
                .IsConcentrationAbility(StatusEffectType.BurstOfSpeed3)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private static void BurstOfSpeed4(AbilityBuilder builder)
        {
            builder.Create(FeatType.BurstOfSpeed4, PerkType.BurstOfSpeed)
                .Name("Burst of Speed IV")
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(5)
                .IsConcentrationAbility(StatusEffectType.BurstOfSpeed4)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
        private static void BurstOfSpeed5(AbilityBuilder builder)
        {
            _ = builder.Create(FeatType.BurstOfSpeed5, PerkType.BurstOfSpeed)
                .Name("Burst of Speed V")
                .HasRecastDelay(RecastGroup.BurstOfSpeed, 20f)
                .RequirementFP(6)
                .IsConcentrationAbility(StatusEffectType.BurstOfSpeed5)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}