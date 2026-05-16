using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class ForceLensAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigurePartyStatus(
                builder
                    .Create(FeatType.ForceLens1, PerkType.ForceLens)
                    .Name("Force Lens")
                    .Level(1)
                    .SkillType(SkillType.Saberstaff)
                    .IsAreaAbility()
                    .HasRecastDelay(RecastGroup.ForceLens, 120f),
                typeof(ForceLensStatusEffect),
                45f,
                8,
                true);

            return builder.Build();
        }
    }
}
