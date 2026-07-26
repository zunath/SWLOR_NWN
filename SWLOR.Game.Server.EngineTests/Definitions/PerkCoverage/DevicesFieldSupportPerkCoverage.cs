using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class DevicesFieldSupportPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.DeflectorShield,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.DeflectorShield1, FeatType.DeflectorShield2, FeatType.DeflectorShield3 },
                },
                new()
                {
                    Perk = PerkType.WeaponJam,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.WeaponJam1 },
                },
                new()
                {
                    Perk = PerkType.PowerCell,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 5 },
                    GrantedFeats = new[] { FeatType.PowerCell1, FeatType.PowerCell2, FeatType.PowerCell3 },
                },
                new()
                {
                    Perk = PerkType.PowerSurge,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.PowerSurgeTrait },
                },
                new()
                {
                    Perk = PerkType.RayshieldScreen,
                    MaxLevel = 2,
                    Prices = new[] { 3, 5 },
                    GrantedFeats = new[] { FeatType.RayshieldScreenTrait },
                },
                new()
                {
                    Perk = PerkType.DampeningField,
                    MaxLevel = 2,
                    Prices = new[] { 4, 5 },
                    GrantedFeats = new[] { FeatType.DampeningFieldTrait },
                },
                new()
                {
                    Perk = PerkType.OverclockRoutine,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.OverclockRoutineTrait },
                },
                new()
                {
                    Perk = PerkType.GroupDeflector,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.GroupDeflector1 },
                },
                new()
                {
                    Perk = PerkType.EmergencyBunker,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.EmergencyBunker1 },
                },
            };
        }
    }
}
