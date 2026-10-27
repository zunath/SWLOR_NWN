using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Formation: an aura that gives party ships within command range less damage taken, switched on and off by
    /// using it. Ported from the Leadership Watchful Presence II (the numbers and cooldown are the ground's), with
    /// its status, which carries the space damage reduction a ship reads. It runs through <see cref="SpaceAuras"/>,
    /// which keeps it on the party ships in range, and it is not a stance, so it never blocks one.
    /// </summary>
    public class FormationAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            PilotingAbilityConfiguration.ConfigureOrder(
                builder.Create(FeatType.CommandFormation, PerkType.CommandFormation)
                    .Name("Formation")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.CommandFormation, 30f),
                0,
                Spell.CommandFormation,
                (activator, _, _, _) => Toggle(activator));

            return builder.Build();
        }

        private static void Toggle(uint activator)
        {
            var aura = typeof(WatchfulPresence2StatusEffect);
            if (SpaceAuras.HasAura(activator, aura))
            {
                SpaceAuras.RemoveAura(activator, aura);
                return;
            }

            if (SpaceAuras.ApplyAura(activator, aura))
                CombatPoint.AddSupportCombatPoint(activator, SpaceCommand.GetPartyShipsInRange(activator), SkillType.Piloting);
        }
    }
}
