using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class DefensiveStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.DefensiveStance1, PerkType.DefensiveStance)
                    .Name("Defensive Stance")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.DefensiveStance, 30f)
                    .UsesAnimation(Animation.ShieldWall),
                typeof(DefensiveStanceStatusEffect),
                () => new DefensiveStanceStatusEffect());

            return builder.Build();
        }
    }
}
