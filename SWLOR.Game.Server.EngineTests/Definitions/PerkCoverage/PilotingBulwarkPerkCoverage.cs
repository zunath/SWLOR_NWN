using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class PilotingBulwarkPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.BulwarkShieldRam,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.BulwarkShieldRam1, FeatType.BulwarkShieldRam2, FeatType.BulwarkShieldRam3, FeatType.BulwarkShieldRam4 },
                },
                new()
                {
                    Perk = PerkType.BulwarkAngerStrike,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.BulwarkAngerStrikeTrait },
                },
                new()
                {
                    Perk = PerkType.BulwarkBulwark,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.BulwarkBulwarkTrait },
                },
                new()
                {
                    Perk = PerkType.BulwarkDefiance,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.BulwarkDefiance1, FeatType.BulwarkDefiance2, FeatType.BulwarkDefiance3 },
                },
                new()
                {
                    Perk = PerkType.BulwarkFortification,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.BulwarkFortificationTrait },
                },
                new()
                {
                    Perk = PerkType.BulwarkHail,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.BulwarkHail1, FeatType.BulwarkHail2 },
                },
                new()
                {
                    Perk = PerkType.BulwarkShipStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.BulwarkShipStance },
                },
                new()
                {
                    Perk = PerkType.BulwarkCriticalWard,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.BulwarkCriticalWardTrait },
                },
                new()
                {
                    Perk = PerkType.BulwarkUnbreakable,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.BulwarkUnbreakableTrait },
                },
                new()
                {
                    Perk = PerkType.BulwarkInvincible,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.BulwarkInvincible },
                },
            };
        }
    }
}
