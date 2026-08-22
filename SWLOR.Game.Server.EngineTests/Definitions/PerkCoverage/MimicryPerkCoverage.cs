using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class MimicryPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.CombatAnalyzer,
                    MaxLevel = 4,
                    Prices = new[] { 2, 3, 3, 3 },
                    GrantedFeats = new[] { FeatType.CombatAnalyzerTrait },
                },
                new()
                {
                    Perk = PerkType.AnalyzerMemory,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.AnalyzerMemoryTrait },
                },
                new()
                {
                    Perk = PerkType.PatternRecognition,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    GrantedFeats = new[] { FeatType.PatternRecognitionTrait },
                },
                new()
                {
                    Perk = PerkType.OverclockedAnalyzer,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.Overload },
                },
            };
        }
    }
}
