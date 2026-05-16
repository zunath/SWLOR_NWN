using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class UnmovingCenterAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder
                    .Create(FeatType.UnmovingCenter1, PerkType.UnmovingCenter)
                    .Name("Unmoving Center")
                    .Level(1)
                    .SkillType(SkillType.Staff)
                    .HasRecastDelay(RecastGroup.Capstone, 1800f),
                typeof(UnmovingCenterStatusEffect),
                20f,
                25,
                activator =>
                {
                    Ability.ApplyTemporaryImmunity(activator, 20f, ImmunityType.Knockdown);
                    Ability.ApplyTemporaryImmunity(activator, 20f, ImmunityType.Dazed);
                },
                activationDelay: 1f);

            return builder.Build();
        }
    }
}
