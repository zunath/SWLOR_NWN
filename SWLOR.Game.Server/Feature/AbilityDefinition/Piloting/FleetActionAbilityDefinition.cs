using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Fleet Action: for 45 s, party ships within command range, the pilot's own included, deal more damage, hit
    /// more accurately, critically hit more often and restore STM. Ported from the Leadership Decisive Command (the
    /// numbers, STM and cooldown are the ground's), with its status. A capstone order, so Command Presence does not
    /// lengthen it.
    /// </summary>
    public class FleetActionAbilityDefinition : IAbilityListDefinition
    {
        private const float ActiveSeconds = 45f;
        private const int Stamina = 15;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PilotingAbilityConfiguration.ConfigureOrder(
                builder.Create(FeatType.CommandFleetAction, PerkType.CommandFleetAction)
                    .Name("Fleet Action")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Capstone, 90f),
                Stamina,
                Spell.CommandFleetAction,
                (activator, _, _, _) => SpaceCommand.ApplyOrder(activator, typeof(DecisiveCommand1StatusEffect), ActiveSeconds, true));

            return builder.Build();
        }
    }
}
