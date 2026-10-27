using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Point Blank Burst I-II: one energy weapon fires at every enemy ship near the pilot's ship, and the pilot
    /// gains Evasion. Ported from the Pistol's Point Blank Burst, with its 5 m burst scaled to space
    /// (<see cref="SpaceRadii.NearShip"/>).
    /// </summary>
    public class PointBlankBurstAbilityDefinition : IAbilityListDefinition
    {
        private const float BuffSeconds = 30f;

        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Rank(_builder.Create(FeatType.AcePointBlankBurst1, PerkType.AcePointBlankBurst).Name("Point Blank Burst I").Level(1), Spell.AcePointBlankBurst1, 16, 10, 6);
            Rank(_builder.Create(FeatType.AcePointBlankBurst2, PerkType.AcePointBlankBurst).Name("Point Blank Burst II").Level(2), Spell.AcePointBlankBurst2, 30, 15, 9);

            return _builder.Build();
        }

        private static void Rank(
            AbilityBuilder ability,
            Spell targetingSpell,
            int bonusDmg,
            int evasionPercent,
            int stamina)
        {
            PilotingAbilityConfiguration.ConfigureShipBurst(
                ability.HasRecastDelay(RecastGroup.AcePointBlankBurst, 30f),
                stamina,
                targetingSpell,
                SpaceRadii.NearShip,
                SpaceWeaponFilter.Energy,
                (activator, _, _, _) =>
                {
                    var results = SpaceAbilities.FireWeaponArea(
                        activator,
                        GetLocation(activator),
                        SpaceRadii.NearShip,
                        int.MaxValue,
                        SpaceWeaponFilter.Energy,
                        bonusDmg);
                    if (!results.Any(x => x.Fired))
                    {
                        PilotingAbilityConfiguration.ReportFailure(activator, results);
                        return;
                    }

                    StatusEffect.ApplyStatusEffect(
                        activator,
                        activator,
                        new PointBlankBurstStatusEffect(evasionPercent),
                        BuffSeconds);
                });
        }
    }
}
