//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceHealAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceHeal1(builder);
            ForceHeal2(builder);
            ForceHeal3(builder);
            ForceHeal4(builder);
            ForceHeal5(builder);

            return builder.Build();
        }

        private static void ForceHeal1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal1, PerkType.ForceHeal)
                .Name("Force Heal 1")
                .HasRecastDelay(RecastGroup.ForceHeal, 60f)
                .HasActivationDelay(2.0f)
                .RequirementFP(2)
                .IsConcentrationAbility(StatusEffectType.ForceHeal1)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceHeal2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal2, PerkType.ForceHeal)
                .Name("Force Heal II")
                .HasRecastDelay(RecastGroup.ForceHeal, 60f)
                .HasActivationDelay(2.0f)
                .RequirementFP(3)
                .IsConcentrationAbility(StatusEffectType.ForceHeal2)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceHeal3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal3, PerkType.ForceHeal)
                .Name("Force Heal III")
                .HasRecastDelay(RecastGroup.ForceHeal, 60f)
                .HasActivationDelay(2.0f)
                .RequirementFP(4)
                .IsConcentrationAbility(StatusEffectType.ForceHeal3)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceHeal4(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal4, PerkType.ForceHeal)
                .Name("Force Heal IV")
                .HasRecastDelay(RecastGroup.ForceHeal, 60f)
                .HasActivationDelay(4.0f)
                .RequirementFP(5)
                .IsConcentrationAbility(StatusEffectType.ForceHeal4)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceHeal5(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceHeal5, PerkType.ForceHeal)
                .Name("Force Heal V")
                .HasRecastDelay(RecastGroup.ForceHeal, 60f)
                .HasActivationDelay(4.0f)
                .RequirementFP(6)
                .IsConcentrationAbility(StatusEffectType.ForceHeal5)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}