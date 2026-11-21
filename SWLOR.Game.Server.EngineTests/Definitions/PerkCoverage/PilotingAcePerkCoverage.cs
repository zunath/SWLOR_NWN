using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class PilotingAcePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.AceDisablingShot,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.AceDisablingShot1, FeatType.AceDisablingShot2, FeatType.AceDisablingShot3, FeatType.AceDisablingShot4 },
                },
                new()
                {
                    Perk = PerkType.AceMobileFootwork,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.AceMobileFootworkTrait },
                },
                new()
                {
                    Perk = PerkType.AceSustainedFire,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.AceSustainedFireTrait },
                },
                new()
                {
                    Perk = PerkType.AceDoubleShot,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.AceDoubleShot1, FeatType.AceDoubleShot2, FeatType.AceDoubleShot3 },
                },
                new()
                {
                    Perk = PerkType.AceKitingInstinct,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.AceKitingInstinctTrait },
                },
                new()
                {
                    Perk = PerkType.AcePointBlankBurst,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.AcePointBlankBurst1, FeatType.AcePointBlankBurst2 },
                },
                new()
                {
                    Perk = PerkType.AceStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.AceStance },
                },
                new()
                {
                    Perk = PerkType.AceHighNoon,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.AceHighNoonTrait },
                },
                new()
                {
                    Perk = PerkType.AceEvasiveReload,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.AceEvasiveReloadTrait },
                },
                new()
                {
                    Perk = PerkType.AceInTheHole,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.AceInTheHole },
                },
            };
        }
    }
}
