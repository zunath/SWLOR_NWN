using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class CripplingDefenseAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureAreaStatus(
                builder
                    .Create(FeatType.CripplingDefense1, PerkType.CripplingDefense)
                    .Name("Crippling Defense")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.CripplingDefense, 1800f),
                typeof(CripplingDefenseStatusEffect),
                15f,
                35,
                true,
                restoreStamina: 25);

            return builder.Build();
        }
    }
}
