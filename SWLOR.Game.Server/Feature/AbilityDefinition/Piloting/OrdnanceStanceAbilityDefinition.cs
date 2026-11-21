using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Ordnance Stance: area launcher abilities deal +10% damage and single-target launcher damage is reduced by
    /// 10%. It is the Throwing Ordnance Stance's status, one stance at a time, and it switches off by using it again.
    /// </summary>
    public class OrdnanceStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            var stance = builder.Create(FeatType.OrdnanceShipStance, PerkType.OrdnanceShipStance)
                .Name("Ordnance Stance")
                .Level(1)
                .SkillType(SkillType.Piloting)
                .CanBeUsedInSpace()
                .HasRecastDelay(RecastGroup.OrdnanceShipStance, 30f);
            ConfigureToggle(stance, typeof(OrdnanceStanceStatusEffect));

            // A space ability has no activation delay (the toggle setup gives a ground stance two seconds).
            stance.HasActivationDelay(0f);

            return builder.Build();
        }
    }
}
