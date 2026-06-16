using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class SniperStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureToggle(
                builder
                    .Create(FeatType.SniperStance1, PerkType.SniperStance)
                    .Name("Sniper Stance")
                    .Level(1)
                    .SkillType(SkillType.Rifle)
                    .HasRecastDelay(RecastGroup.SniperStance, 180f)
                    .UsesAnimation(Animation.PointPistol),
                typeof(SniperStanceStatusEffect));

            return builder.Build();
        }
    }
}
