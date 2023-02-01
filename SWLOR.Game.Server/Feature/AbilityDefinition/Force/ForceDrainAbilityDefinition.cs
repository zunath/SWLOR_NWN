//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceDrainAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceDrain1(builder);
            ForceDrain2(builder);
            ForceDrain3(builder);
            ForceDrain4(builder);
            ForceDrain5(builder);

            return builder.Build();
        }

        private static void ForceDrain1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain1, PerkType.ForceDrain)
                .Name("Force Drain I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(2)
                .IsConcentrationAbility(StatusEffectType.ForceDrain1)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceDrain2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain2, PerkType.ForceDrain)
                .Name("Force Drain II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(3)
                .IsConcentrationAbility(StatusEffectType.ForceDrain2)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceDrain3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain3, PerkType.ForceDrain)
                .Name("Force Drain III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .IsConcentrationAbility(StatusEffectType.ForceDrain3)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceDrain4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain4, PerkType.ForceDrain)
                .Name("Force Drain IV")
                .Level(4)
                .HasRecastDelay(RecastGroup.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(5)
                .IsConcentrationAbility(StatusEffectType.ForceDrain4)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceDrain5(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceDrain5, PerkType.ForceDrain)
                .Name("Force Drain V")
                .HasRecastDelay(RecastGroup.ForceDrain, 12f)
                .HasActivationDelay(2f)
                .HasMaxRange(15.0f)
                .RequirementFP(6)
                .IsConcentrationAbility(StatusEffectType.ForceDrain5)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}