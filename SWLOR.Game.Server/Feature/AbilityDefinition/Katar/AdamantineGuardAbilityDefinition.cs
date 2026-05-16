using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Katar
{
    public class AdamantineGuardAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder
                    .Create(FeatType.AdamantineGuard1, PerkType.AdamantineGuard)
                    .Name("Adamantine Guard")
                    .Level(1)
                    .SkillType(SkillType.Katar)
                    .HasRecastDelay(RecastGroup.Capstone, 1800f),
                typeof(AdamantineGuardStatusEffect),
                20f,
                25,
                activationDelay: 1f);

            return builder.Build();
        }
    }
}
