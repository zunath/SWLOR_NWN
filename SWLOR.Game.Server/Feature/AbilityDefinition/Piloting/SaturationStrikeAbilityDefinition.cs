using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Saturation Strike: one launcher fires at every enemy ship near the pilot's ship, and for 45 seconds the
    /// pilot's area launcher abilities leave fragmentation zones. Ported from the Throwing Rain of Steel (damage,
    /// STM, cooldown and zone numbers are the ground's). The zones are the same fragmentation Shrapnel Warheads
    /// leave, set as temporary stats, so the two add as they do on the ground.
    /// </summary>
    public class SaturationStrikeAbilityDefinition : IAbilityListDefinition
    {
        private const int BonusDmg = 20;
        private const int Stamina = 15;
        private const int ZoneDamage = 8;
        private const int ZonePulseSeconds = 6;
        private const int ZoneDurationSeconds = 30;
        private const int ZoneSeconds = 45;

        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            PilotingAbilityConfiguration.ConfigureShipBurst(
                _builder.Create(FeatType.OrdnanceSaturationStrike, PerkType.OrdnanceSaturationStrike)
                    .Name("Saturation Strike")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Capstone, 90f),
                Stamina,
                Spell.OrdnanceSaturationStrike,
                SpaceRadii.NearShip,
                SpaceWeaponFilter.Launcher,
                (activator, _, _, _) =>
                {
                    var results = SpaceAbilities.FireWeaponArea(
                        activator,
                        GetLocation(activator),
                        SpaceRadii.NearShip,
                        int.MaxValue,
                        SpaceWeaponFilter.Launcher,
                        BonusDmg);
                    if (!results.Any(x => x.Fired))
                    {
                        PilotingAbilityConfiguration.ReportFailure(activator, results);
                        return;
                    }

                    // The zones start with the strike, so its own blast already leaves them.
                    SpaceOrdnance.StartFragmentationZones(
                        activator, ZoneDamage, ZonePulseSeconds, ZoneDurationSeconds, ZoneSeconds);
                    SpaceOrdnance.OnAreaAbilityResolved(activator, results);
                });

            return _builder.Build();
        }
    }
}
