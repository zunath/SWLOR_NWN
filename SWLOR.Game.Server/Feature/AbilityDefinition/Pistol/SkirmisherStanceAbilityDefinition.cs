using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class SkirmisherStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.SkirmisherStance1, PerkType.SkirmisherStance)
                .Name("Skirmisher Stance")
                .Level(1)
                .SkillType(SkillType.Pistol)
                .HasRecastDelay(RecastGroup.SkirmisherStance, 180f);
            ConfigureToggle(builder, typeof(SkirmisherStanceStatusEffect));

            return builder.Build();
        }
    }
}
