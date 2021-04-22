//using Random = SWLOR.Game.Server.Service.Random;

using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.AbilityService;

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
                .HasActivationDelay(2.0f)
                .RequirementFP(4)
                .IsConcentrationAbility(StatusEffectType.ForceStun1)
                .DisplaysVisualEffectWhenActivating(); 
        }

        private static void ForceStun2(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun2, PerkType.ForceStun)
                .Name("Force Stun II")
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasActivationDelay(2.0f)
                .RequirementFP(6)
                .IsConcentrationAbility(StatusEffectType.ForceStun2)
                .DisplaysVisualEffectWhenActivating();
        }

        private static void ForceStun3(AbilityBuilder builder)
        {
            builder.Create(FeatType.ForceStun3, PerkType.ForceStun)
                .Name("Force Stun III")
                .HasRecastDelay(RecastGroup.ForceStun, 60f * 5f)
                .HasActivationDelay(2.0f)
                .RequirementFP(8)
                .IsConcentrationAbility(StatusEffectType.ForceStun3)
                .DisplaysVisualEffectWhenActivating();
        }
    }
}