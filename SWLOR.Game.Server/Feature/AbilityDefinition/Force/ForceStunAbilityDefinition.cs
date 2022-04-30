using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceStunAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();
            ForceStun1(builder);
            ForceStun2(builder);
            ForceStun3(builder);

            return builder.Build();
        }

        private static void ForceStun1(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun1, PerkType.ForceStun)
                .Name("Force Stun I")
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasMaxRange(15.0f)
                .RequirementFP(4)
                .UsesAnimation(Animation.LoopingConjure1)
                .IsConcentrationAbility(StatusEffectType.ForceStun1)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating(); 
        }

        private static void ForceStun2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun2, PerkType.ForceStun)
                .Name("Force Stun II")
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasMaxRange(15.0f)
                .RequirementFP(7)
                .UsesAnimation(Animation.LoopingConjure1)
                .IsConcentrationAbility(StatusEffectType.ForceStun2)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceStun3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun3, PerkType.ForceStun)
                .Name("Force Stun III")
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasMaxRange(15.0f)
                .RequirementFP(14)
                .UsesAnimation(Animation.LoopingConjure1)
                .IsConcentrationAbility(StatusEffectType.ForceStun3)
                .IsHostileAbility()
                .DisplaysVisualEffectWhenActivating();
        }
    }
}