using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class DecoyAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureAreaStatus(
                builder
                    .Create(FeatType.Decoy1, PerkType.Decoy)
                    .Name("Decoy")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Decoy, 30f),
                typeof(DecoyStatusEffect),
                12f,
                12,
                true,
                activationDelay: 1f);

            return builder.Build();
        }
    }
}
