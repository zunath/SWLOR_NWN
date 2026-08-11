using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class ForceDarkRavagerPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.ForceSpark,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.ForceSpark1, FeatType.ForceSpark2 },
                },
                new()
                {
                    Perk = PerkType.ForceLightning,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.ForceLightning1, FeatType.ForceLightning2, FeatType.ForceLightning3 },
                },
                new()
                {
                    Perk = PerkType.UnstablePressure,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.UnstablePressureTrait },
                },
                new()
                {
                    Perk = PerkType.ForceDrain,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.ForceDrain1, FeatType.ForceDrain2, FeatType.ForceDrain3 },
                },
                new()
                {
                    Perk = PerkType.FuryStance,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.FuryStance1, FeatType.FuryStance2 },
                },
                new()
                {
                    Perk = PerkType.DevouringStrike,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.DevouringStrikeTrait },
                },
                new()
                {
                    Perk = PerkType.CruelMomentum,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.CruelMomentumTrait },
                },
                new()
                {
                    Perk = PerkType.HungerOfTheDark,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.HungerOfTheDark1 },
                },
            };
        }
    }
}
