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
                    .HasRecastDelay(RecastGroup.UnmovingCenter, 1800f),
                typeof(UnmovingCenterStatusEffect),
                20f,
                50,
                activator =>
                {
                    Ability.ApplyTemporaryImmunity(activator, 20f, ImmunityType.Knockdown);
                    Ability.ApplyTemporaryImmunity(activator, 20f, ImmunityType.Dazed);
                });

            return builder.Build();
        }
    }
}
