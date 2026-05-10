using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class BastionStanceAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            BastionStance(builder);

            return builder.Build();
        }

        private static void BastionStance(AbilityBuilder builder)
        {
            builder.Create(FeatType.BastionStance1, PerkType.BastionStance)
                .Name("Bastion Stance")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.BastionStance, 180f)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, typeof(BastionStanceStatusEffect)))
                .HasImpactAction((activator, target, level, targetLocation) => ApplySelfStatus(activator, typeof(BastionStanceStatusEffect)))
                .IsCastedAbility()
                .BreaksStealth();
        }
    }
}
