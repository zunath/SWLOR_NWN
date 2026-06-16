using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Throwing
{
    public class BombardierStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            builder
                .Create(FeatType.BombardierStance1, PerkType.BombardierStance)
                .Name("Bombardier Stance")
                .Level(1)
                .SkillType(SkillType.Throwing)
                .HasRecastDelay(RecastGroup.BombardierStance, 180f)
                .UsesAnimation(Animation.ThrowGrenade);
            ConfigureToggle(builder, typeof(BombardierStanceStatusEffect));

            return builder.Build();
        }
    }
}
