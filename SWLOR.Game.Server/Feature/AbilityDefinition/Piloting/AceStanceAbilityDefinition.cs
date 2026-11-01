using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Ace Stance: +15% Evasion and +10% Movement Speed for -10% Attack. It is the Pistol's Skirmisher Stance,
    /// so it is the same status, one stance at a time, and it switches off by using it again.
    /// </summary>
    public class AceStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            var stance = builder.Create(FeatType.AceStance, PerkType.AceStance)
                .Name("Ace Stance")
                .Level(1)
                .SkillType(SkillType.Piloting)
                .CanBeUsedInSpace()
                .HasRecastDelay(RecastGroup.AceStance, 30f);
            ConfigureToggle(stance, typeof(SkirmisherStanceStatusEffect));

            // A space ability has no activation delay (the toggle setup gives a ground stance two seconds).
            stance.HasActivationDelay(0f);

            return builder.Build();
        }
    }
}
