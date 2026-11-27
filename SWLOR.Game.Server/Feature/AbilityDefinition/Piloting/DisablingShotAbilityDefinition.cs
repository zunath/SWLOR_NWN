using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Disabling Shot I-IV: one energy weapon shot at the target that inflicts Disarming Shot. Ported from the
    /// Pistol's Disarming Shot (damage, STM, cooldown, Attack penalty and duration are the ground's).
    /// </summary>
    public class DisablingShotAbilityDefinition : IAbilityListDefinition
    {
        private const float DisarmingShotSeconds = 30f;

        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.AceDisablingShot1, PerkType.AceDisablingShot).Name("Disabling Shot I").Level(1), 8, 10, 3);
            Rank(_builder.Create(FeatType.AceDisablingShot2, PerkType.AceDisablingShot).Name("Disabling Shot II").Level(2), 18, 12, 5);
            Rank(_builder.Create(FeatType.AceDisablingShot3, PerkType.AceDisablingShot).Name("Disabling Shot III").Level(3), 28, 15, 8);
            Rank(_builder.Create(FeatType.AceDisablingShot4, PerkType.AceDisablingShot).Name("Disabling Shot IV").Level(4), 38, 18, 12);

            return _builder.Build();
        }

        private static void Rank(AbilityBuilder ability, int bonusDmg, int attackPenaltyPercent, int stamina)
        {
            PilotingAbilityConfiguration.ConfigureTargetedShot(
                ability.HasRecastDelay(RecastGroup.AceDisablingShot, 18f),
                stamina,
                SpaceWeaponFilter.Energy,
                (activator, target, _, _) =>
                {
                    var result = SpaceAbilities.FireWeapon(activator, target, SpaceWeaponFilter.Energy, bonusDmg);
                    if (!result.Fired)
                    {
                        PilotingAbilityConfiguration.ReportFailure(activator, new[] { result });
                        return;
                    }

                    if (result.IsHit)
                    {
                        Space.ApplyStatusToShip(
                            activator,
                            target,
                            new DisarmingShotStatusEffect(attackPenaltyPercent),
                            DisarmingShotSeconds);
                    }
                });
        }
    }
}
