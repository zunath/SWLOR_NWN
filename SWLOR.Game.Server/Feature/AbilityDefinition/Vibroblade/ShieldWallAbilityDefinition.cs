using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class ShieldWallAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const string ReplacementAnimationName = "Shield_Wall";

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigurePartyStatus(
                builder
                    .Create(FeatType.ShieldWall1, PerkType.ShieldWall)
                    .Name("Shield Wall")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.ShieldWall, 45f)
                    .UsesAnimationOverwrite(ReplacementAnimationName),
                typeof(ShieldWallStatusEffect),
                60f,
                10,
                true,
                6f);

            return builder.Build();
        }
    }
}
