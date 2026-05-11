using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class GunfighterStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.GunfighterStance1, PerkType.GunfighterStance)
                .Name("Gunfighter Stance")
                .Level(1)
                .HasRecastDelay(RecastGroup.GunfighterStance, 180f);
            ConfigureToggle(builder, typeof(GunfighterStanceStatusEffect));

            return builder.Build();
        }
    }
}
