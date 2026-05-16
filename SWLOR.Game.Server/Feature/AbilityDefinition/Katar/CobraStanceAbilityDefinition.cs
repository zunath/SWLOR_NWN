using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class CobraStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.CobraStance1, PerkType.CobraStance)
                    .Name("Cobra Stance")
                    .Level(1)
                    .SkillType(SkillType.Katar)
                    .HasRecastDelay(RecastGroup.CobraStance, 180f),
                typeof(CobraStanceStatusEffect));

            return builder.Build();
        }
    }
}
