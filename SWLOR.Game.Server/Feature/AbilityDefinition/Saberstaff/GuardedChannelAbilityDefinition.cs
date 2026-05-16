using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class GuardedChannelAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder
                    .Create(FeatType.GuardedChannel1, PerkType.GuardedChannel)
                    .Name("Guarded Channel I")
                    .Level(1)
                    .SkillType(SkillType.Saberstaff)
                    .HasRecastDelay(RecastGroup.GuardedChannel, 60f),
                () => new GuardedChannelStatusEffect(20, 20),
                10f,
                6);
            ConfigureSelfStatus(
                builder
                    .Create(FeatType.GuardedChannel2, PerkType.GuardedChannel)
                    .Name("Guarded Channel II")
                    .Level(2)
                    .SkillType(SkillType.Saberstaff)
                    .HasRecastDelay(RecastGroup.GuardedChannel, 60f),
                () => new GuardedChannelStatusEffect(30, 30),
                12f,
                8);
            ConfigureSelfStatus(
                builder
                    .Create(FeatType.GuardedChannel3, PerkType.GuardedChannel)
                    .Name("Guarded Channel III")
                    .Level(3)
                    .SkillType(SkillType.Saberstaff)
                    .HasRecastDelay(RecastGroup.GuardedChannel, 120f),
                () => new GuardedChannelStatusEffect(40, 35),
                15f,
                12);

            return builder.Build();
        }
    }
}
