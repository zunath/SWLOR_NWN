using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class SoulDevourerAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SoulDevourer(builder);

            return builder.Build();
        }

        private static void SoulDevourer(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SoulDevourer1, PerkType.SoulDevourer)
                .Name("Soul Devourer")
                .Level(1)
                .HasActivationDelay(2f)
                .HasRecastDelay(RecastGroup.SoulDevourer, 180f)
                .HasActivationAction((activator, target, level, targetLocation) => ToggleSelfStatus(activator, typeof(SoulDevourerStatusEffect)))
                .HasImpactAction((activator, target, level, targetLocation) => ApplySelfStatus(activator, typeof(SoulDevourerStatusEffect)))
                .IsCastedAbility()
                .BreaksStealth();
        }
    }
}
