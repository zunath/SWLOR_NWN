using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class PilotingCommandPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.CommandEmergencyRepair,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.CommandEmergencyRepair1, FeatType.CommandEmergencyRepair2, FeatType.CommandEmergencyRepair3, FeatType.CommandEmergencyRepair4 },
                },
                new()
                {
                    Perk = PerkType.CommandMarkTarget,
                    MaxLevel = 2,
                    Prices = new[] { 2, 4 },
                    GrantedFeats = new[] { FeatType.CommandMarkTargetTrait },
                },
                new()
                {
                    Perk = PerkType.CommandRepairEfficiency,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.CommandRepairEfficiencyTrait },
                },
                new()
                {
                    Perk = PerkType.CommandFocusFire,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.CommandFocusFire1, FeatType.CommandFocusFire2, FeatType.CommandFocusFire3 },
                },
                new()
                {
                    Perk = PerkType.CommandCrewMorale,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.CommandCrewMoraleTrait },
                },
                new()
                {
                    Perk = PerkType.CommandBreakFormation,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.CommandBreakFormation1, FeatType.CommandBreakFormation2 },
                },
                new()
                {
                    Perk = PerkType.CommandFormation,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.CommandFormation },
                },
                new()
                {
                    Perk = PerkType.CommandPresence,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.CommandPresenceTrait },
                },
                new()
                {
                    Perk = PerkType.CommandFleetAction,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.CommandFleetAction },
                },
            };
        }
    }
}
