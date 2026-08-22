using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class ForceUniversalPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.ForcePush,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.ForcePush1, FeatType.ForcePush2, FeatType.ForcePush3 },
                },
                new()
                {
                    Perk = PerkType.ThrowLightsaber,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 5 },
                    GrantedFeats = new[] { FeatType.ThrowLightsaber1, FeatType.ThrowLightsaber2, FeatType.ThrowLightsaber3 },
                },
                new()
                {
                    Perk = PerkType.ForceLeap,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.ForceLeap1, FeatType.ForceLeap2 },
                },
                new()
                {
                    Perk = PerkType.Precognition,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.PrecognitionTrait },
                },
                new()
                {
                    Perk = PerkType.ForceConvergence,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ForceConvergenceTrait },
                },
            };
        }
    }
}
