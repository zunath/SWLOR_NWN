using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Saberstaff
{
    public class DoubleStrikeAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureMultiHit(
                builder
                    .Create(FeatType.DoubleStrike1, PerkType.DoubleStrike)
                    .Name("Double Strike I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.DoubleStrike, 60f)
                    .UsesAnimation(Animation.DoubleStrike),
                SkillType.Saberstaff,
                12,
                2,
                3);
            ConfigureMultiHit(
                builder
                    .Create(FeatType.DoubleStrike2, PerkType.DoubleStrike)
                    .Name("Double Strike II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.DoubleStrike, 60f)
                    .UsesAnimation(Animation.DoubleStrike),
                SkillType.Saberstaff,
                21,
                2,
                5);
            ConfigureMultiHit(
                builder
                    .Create(FeatType.DoubleStrike3, PerkType.DoubleStrike)
                    .Name("Double Strike III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.DoubleStrike, 60f)
                    .UsesAnimation(Animation.DoubleStrike),
                SkillType.Saberstaff,
                29,
                2,
                8);
            ConfigureMultiHit(
                builder
                    .Create(FeatType.DoubleStrike4, PerkType.DoubleStrike)
                    .Name("Double Strike IV")
                    .Level(4)
                    .HasRecastDelay(RecastGroup.DoubleStrike, 60f)
                    .UsesAnimation(Animation.DoubleStrike),
                SkillType.Saberstaff,
                38,
                2,
                12,
                bonusStatus: typeof(ForceErosionStatusEffect),
                bonusDamage: 15);

            return builder.Build();
        }
    }
}
