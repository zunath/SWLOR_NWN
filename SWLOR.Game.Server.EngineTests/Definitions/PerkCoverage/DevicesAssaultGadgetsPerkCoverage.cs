using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class DevicesAssaultGadgetsPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.Flamethrower,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 3 },
                    GrantedFeats = new[] { FeatType.Flamethrower1, FeatType.Flamethrower2, FeatType.Flamethrower3 },
                },
                new()
                {
                    Perk = PerkType.WristRocket,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 3 },
                    GrantedFeats = new[] { FeatType.WristRocket1, FeatType.WristRocket2, FeatType.WristRocket3 },
                },
                new()
                {
                    Perk = PerkType.SonicBurst,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 3 },
                    GrantedFeats = new[] { FeatType.SonicBurst1, FeatType.SonicBurst2, FeatType.SonicBurst3 },
                },
                new()
                {
                    Perk = PerkType.GadgetHarness,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.GadgetHarnessTrait },
                },
                new()
                {
                    Perk = PerkType.ArcProjector,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.ArcProjector1, FeatType.ArcProjector2, FeatType.ArcProjector3 },
                },
                new()
                {
                    Perk = PerkType.IonLance,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.IonLance1, FeatType.IonLance2, FeatType.IonLance3 },
                },
                new()
                {
                    Perk = PerkType.RailDart,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 3 },
                    GrantedFeats = new[] { FeatType.RailDart1, FeatType.RailDart2, FeatType.RailDart3 },
                },
                new()
                {
                    Perk = PerkType.TacticalUplink,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.TacticalUplinkTrait },
                },
                new()
                {
                    Perk = PerkType.CryoSprayer,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.CryoSprayer1 },
                },
                new()
                {
                    Perk = PerkType.OverloadBarrage,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.OverloadBarrage1 },
                },
            };
        }
    }
}
