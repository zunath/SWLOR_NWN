using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Staff
{
    public class LegSweepAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureWeapon(
                builder
                    .Create(FeatType.LegSweep1, PerkType.LegSweep)
                    .Name("Leg Sweep I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.LegSweep, 45f),
                SkillType.Staff,
                6,
                3,
                typeof(KnockdownStatusEffect),
                4);
            ConfigureWeapon(
                builder
                    .Create(FeatType.LegSweep2, PerkType.LegSweep)
                    .Name("Leg Sweep II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.LegSweep, 45f),
                SkillType.Staff,
                16,
                3,
                typeof(KnockdownStatusEffect),
                5);
            ConfigureWeapon(
                builder
                    .Create(FeatType.LegSweep3, PerkType.LegSweep)
                    .Name("Leg Sweep III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.LegSweep, 45f),
                SkillType.Staff,
                26,
                4,
                typeof(KnockdownStatusEffect),
                7);

            return builder.Build();
        }
    }
}
