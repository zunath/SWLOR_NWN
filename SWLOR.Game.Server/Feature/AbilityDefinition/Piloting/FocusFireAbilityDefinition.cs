using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Focus Fire I-III: party ships within command range deal bonus damage (and, at III, gain Accuracy) for 30 s.
    /// Ported from the Leadership Press the Attack (the numbers, STM and cooldowns are the ground's), with its
    /// statuses, which ships read through their Damage Dealt and Accuracy stats. Command Presence lengthens it.
    /// </summary>
    public class FocusFireAbilityDefinition : IAbilityListDefinition
    {
        private const float OrderSeconds = 30f;

        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.CommandFocusFire1, PerkType.CommandFocusFire).Name("Focus Fire I").Level(1),
                Spell.CommandFocusFire1, typeof(PressTheAttack1StatusEffect), 4, 24f);
            Rank(_builder.Create(FeatType.CommandFocusFire2, PerkType.CommandFocusFire).Name("Focus Fire II").Level(2),
                Spell.CommandFocusFire2, typeof(PressTheAttack2StatusEffect), 6, 24f);
            Rank(_builder.Create(FeatType.CommandFocusFire3, PerkType.CommandFocusFire).Name("Focus Fire III").Level(3),
                Spell.CommandFocusFire3, typeof(PressTheAttack3StatusEffect), 9, 30f);

            return _builder.Build();
        }

        private static void Rank(AbilityBuilder ability, Spell targetingSpell, System.Type status, int stamina, float cooldown)
        {
            PilotingAbilityConfiguration.ConfigureOrder(
                ability.HasRecastDelay(RecastGroup.CommandFocusFire, cooldown),
                stamina,
                targetingSpell,
                (activator, _, _, _) => SpaceCommand.ApplyOrder(activator, status, OrderSeconds, false));
        }
    }
}
