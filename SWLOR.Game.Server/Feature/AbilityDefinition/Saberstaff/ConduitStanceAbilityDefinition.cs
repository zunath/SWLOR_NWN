using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class ConduitStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.ConduitStance1, PerkType.ConduitStance)
                    .Name("Conduit Stance")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ConduitStance, 180f),
                typeof(ConduitStanceStatusEffect));

            return builder.Build();
        }
    }
}
