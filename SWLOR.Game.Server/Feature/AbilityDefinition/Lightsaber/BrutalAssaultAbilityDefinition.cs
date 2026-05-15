using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Lightsaber
{
    public class BrutalAssaultAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigurePartyStatus(
                builder
                    .Create(FeatType.BrutalAssault1, PerkType.BrutalAssault)
                    .Name("Brutal Assault")
                    .Level(1)
                    .IsAreaAbility()
                    .HasRecastDelay(RecastGroup.BrutalAssault, 300f),
                typeof(BrutalAssaultStatusEffect),
                60f,
                7,
                false,
                2f);

            return builder.Build();
        }
    }
}
