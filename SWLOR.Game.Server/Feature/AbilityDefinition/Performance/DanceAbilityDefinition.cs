using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Performance
{
    public class DanceAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Dance(builder, FeatType.Dance1, "Dance I", 1);
            Dance(builder, FeatType.Dance2, "Dance II", 2);
            Dance(builder, FeatType.Dance3, "Dance III", 3);
            Dance(builder, FeatType.Dance4, "Dance IV", 4);

            return builder.Build();
        }

        private static void Dance(AbilityBuilder builder, FeatType featType, string name, int level)
        {
            builder
                .Create(featType, PerkType.Dance)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DanceHandsUp)
                .PreservesNativeAnimationChoreography()
                .UsesNativeAnimationPreview(Animation.DanceHandsUp, 3f)
                .SkillType(SkillType.Performance)
                .HasImpactAction(DanceImpactAction)
                .IsCastedAbility();
        }

        private static void DanceImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Service.Performance.ToggleDance(activator, level);
        }
    }
}
