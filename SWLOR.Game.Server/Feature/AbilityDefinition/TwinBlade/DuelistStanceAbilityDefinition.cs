using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.TwinBlade
{
    public class DuelistStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.DuelistStance1, PerkType.DuelistStance)
                    .Name("Duelist Stance")
                    .Level(1)
                    .SkillType(SkillType.TwinBlade)
                    .HasRecastDelay(RecastGroup.DuelistStance, 180f)
                    .UsesAnimation(Animation.DualWieldingStance),
                typeof(DuelistStanceStatusEffect));

            return builder.Build();
        }
    }
}
