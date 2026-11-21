using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Bulwark Stance: +30% enmity, +20% Defense and +100% threat from damage, for -20% Attack. It is the
    /// Vibroblade Defensive Stance, so it is the same status, one stance at a time, and it switches off by using
    /// it again.
    /// </summary>
    public class BulwarkStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            var stance = builder.Create(FeatType.BulwarkShipStance, PerkType.BulwarkShipStance)
                .Name("Bulwark Stance")
                .Level(1)
                .SkillType(SkillType.Piloting)
                .CanBeUsedInSpace()
                .HasRecastDelay(RecastGroup.BulwarkShipStance, 30f);
            ConfigureToggle(stance, typeof(DefensiveStanceStatusEffect));

            // A space ability has no activation delay (the toggle setup gives a ground stance two seconds).
            stance.HasActivationDelay(0f);

            return builder.Build();
        }
    }
}
