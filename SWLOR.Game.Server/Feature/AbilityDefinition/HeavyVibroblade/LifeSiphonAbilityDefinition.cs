using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class LifeSiphonAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            LifeSiphon(builder);

            return builder.Build();
        }

        private static void LifeSiphon(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.LifeSiphon1, PerkType.LifeSiphon)
                .Name("Life Siphon")
                .Level(1)
                .HasActivationDelay(0f)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, typeof(LifeSiphonStatusEffect)))
                .HasImpactAction((activator, target, level, targetLocation) => ApplySelfStatus(activator, typeof(LifeSiphonStatusEffect)))
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .BreaksStealth();
        }
    }
}
