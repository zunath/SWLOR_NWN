using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class CrossCutAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureMultiHit(
                builder
                    .Create(FeatType.CrossCut1, PerkType.CrossCut)
                    .Name("Cross Cut I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.CrossCut, 60f)
                    .UsesAnimation(Animation.CrossCut),
                SkillType.TwinBlade,
                8,
                2,
                3,
                8,
                typeof(DisorientedStatusEffect));
            ConfigureMultiHit(
                builder
                    .Create(FeatType.CrossCut2, PerkType.CrossCut)
                    .Name("Cross Cut II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.CrossCut, 60f)
                    .UsesAnimation(Animation.CrossCut),
                SkillType.TwinBlade,
                17,
                2,
                5,
                10,
                typeof(DisorientedStatusEffect));
            ConfigureMultiHit(
                builder
                    .Create(FeatType.CrossCut3, PerkType.CrossCut)
                    .Name("Cross Cut III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.CrossCut, 60f)
                    .UsesAnimation(Animation.CrossCut),
                SkillType.TwinBlade,
                25,
                2,
                8,
                12,
                typeof(DisorientedStatusEffect));
            ConfigureMultiHit(
                builder
                    .Create(FeatType.CrossCut4, PerkType.CrossCut)
                    .Name("Cross Cut IV")
                    .Level(4)
                    .HasRecastDelay(RecastGroup.CrossCut, 60f)
                    .UsesAnimation(Animation.CrossCut),
                SkillType.TwinBlade,
                34,
                2,
                12,
                12,
                typeof(DisorientedStatusEffect),
                typeof(HamstringStatusEffect));

            return builder.Build();
        }
    }
}
