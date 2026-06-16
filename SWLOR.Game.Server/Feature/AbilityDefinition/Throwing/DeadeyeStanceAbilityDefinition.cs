using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class DeadeyeStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.DeadeyeStance1, PerkType.DeadeyeStance)
                .Name("Deadeye Stance")
                .Level(1)
                .SkillType(SkillType.Throwing)
                .HasRecastDelay(RecastGroup.DeadeyeStance, 180f)
                .UsesAnimation(Animation.PointForward);
            ConfigureToggle(builder, typeof(DeadeyeStanceStatusEffect));

            return builder.Build();
        }
    }
}
