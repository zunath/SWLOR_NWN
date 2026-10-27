using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Double Shot I-III: one energy weapon fires twice at the target, and a critical hit from either shot
    /// restores STM. Ported from the Pistol's Double Shot.
    /// </summary>
    public class DoubleShotAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.AceDoubleShot1, PerkType.AceDoubleShot).Name("Double Shot I").Level(1), 6, 4, 2);
            Rank(_builder.Create(FeatType.AceDoubleShot2, PerkType.AceDoubleShot).Name("Double Shot II").Level(2), 13, 8, 3);
            Rank(_builder.Create(FeatType.AceDoubleShot3, PerkType.AceDoubleShot).Name("Double Shot III").Level(3), 20, 10, 4);

            return _builder.Build();
        }

        private static void Rank(AbilityBuilder ability, int bonusDmg, int stamina, int criticalStaminaRestore)
        {
            PilotingAbilityConfiguration.ConfigureTargetedShot(
                ability.HasRecastDelay(RecastGroup.AceDoubleShot, 24f),
                stamina,
                SpaceWeaponFilter.Energy,
                (activator, target, _, _) =>
                {
                    var first = SpaceAbilities.FireWeapon(activator, target, SpaceWeaponFilter.Energy, bonusDmg);
                    if (!first.Fired)
                    {
                        PilotingAbilityConfiguration.ReportFailure(activator, new[] { first });
                        return;
                    }

                    var second = SpaceAbilities.FireWeapon(activator, target, SpaceWeaponFilter.Energy, bonusDmg);
                    if (!first.IsCritical && !second.IsCritical)
                        return;

                    var ability = Ability.GetActiveAbilityImpactSummary(activator)?.Ability;
                    var restored = Combat.RestoreAbilityHitStamina(
                        activator, ability, criticalStaminaRestore, sendFeedback: false);
                    if (restored > 0 && GetIsPC(activator))
                    {
                        PlayerFeedback.ShowDiagnosticFloatingText(
                            ColorToken.Combat($"Double Shot restored {restored} STM"),
                            activator,
                            false);
                    }
                });
        }
    }
}
