using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class MarkedForDeathAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureTargetStatus(
                builder
                    .Create(FeatType.MarkedForDeath1, PerkType.MarkedForDeath)
                    .Name("Marked for Death")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.MarkedForDeath, 90f),
                typeof(MarkedForDeathStatusEffect),
                20f,
                6);

            return builder.Build();
        }
    }
}
