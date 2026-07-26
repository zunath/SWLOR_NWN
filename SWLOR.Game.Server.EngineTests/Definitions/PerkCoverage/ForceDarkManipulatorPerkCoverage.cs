using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class ForceDarkManipulatorPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.CreepingTerror,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.CreepingTerror1, FeatType.CreepingTerror2, FeatType.CreepingTerror3 },
                },
                new()
                {
                    Perk = PerkType.WeakenResolve,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.WeakenResolve1, FeatType.WeakenResolve2 },
                },
                new()
                {
                    Perk = PerkType.NightmareField,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.NightmareField1 },
                },
                new()
                {
                    Perk = PerkType.ForceChoke,
                    MaxLevel = 4,
                    Prices = new[] { 2, 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.ForceChoke1, FeatType.ForceChoke2, FeatType.ForceChoke3, FeatType.ForceChoke4 },
                },
                new()
                {
                    Perk = PerkType.CollapseWill,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.CollapseWillTrait },
                },
                new()
                {
                    Perk = PerkType.EclipseOfResolve,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.EclipseOfResolve1 },
                },
            };
        }
    }
}
