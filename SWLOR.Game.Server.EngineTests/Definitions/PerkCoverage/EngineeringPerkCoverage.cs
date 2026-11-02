using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class EngineeringPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.ConductiveWiring,
                    MaxLevel = 2,
                    Prices = new[] { 1, 3 },
                    Notes = "Overclocker, Engineering 5 and 45. Circuit stat only; no feat.",
                },
                new()
                {
                    Perk = PerkType.CapacitorBank,
                    MaxLevel = 1,
                    Prices = new[] { 1 },
                    Notes = "Overclocker, Engineering 10.",
                },
                new()
                {
                    Perk = PerkType.HeatSinks,
                    MaxLevel = 2,
                    Prices = new[] { 2, 3 },
                    Notes = "Overclocker, Engineering 15 and 35.",
                },
                new()
                {
                    Perk = PerkType.OverclockStyle,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Overclocker style, Engineering 20. Picked in the craft window; carried by OverclockStatusEffect.",
                },
                new()
                {
                    Perk = PerkType.CascadeDischarge,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Overclocker, Engineering 25. Adds a Circuit action.",
                },
                new()
                {
                    Perk = PerkType.OverdriveBoost,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Overclocker, Engineering 30.",
                },
                new()
                {
                    Perk = PerkType.ShockAbsorbers,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Overclocker, Engineering 40.",
                },
                new()
                {
                    Perk = PerkType.CircuitSurge,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    Notes = "Overclocker capstone, Engineering 50. Automatic trait.",
                },
                new()
                {
                    Perk = PerkType.InsulatedTraces,
                    MaxLevel = 2,
                    Prices = new[] { 1, 3 },
                    Notes = "Technician, Engineering 5 and 40.",
                },
                new()
                {
                    Perk = PerkType.FineCalibration,
                    MaxLevel = 2,
                    Prices = new[] { 1, 3 },
                    Notes = "Technician, Engineering 10 and 45.",
                },
                new()
                {
                    Perk = PerkType.CircuitDiagnostics,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Technician, Engineering 15.",
                },
                new()
                {
                    Perk = PerkType.SafeModeStyle,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    Notes = "Technician style, Engineering 20. Picked in the craft window; carried by SafeModeStatusEffect.",
                },
                new()
                {
                    Perk = PerkType.MicroTune,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Technician, Engineering 25. Adds a Circuit action.",
                },
                new()
                {
                    Perk = PerkType.SignalLock,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Technician, Engineering 30.",
                },
                new()
                {
                    Perk = PerkType.PrecisionPatch,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    Notes = "Technician, Engineering 35.",
                },
                new()
                {
                    Perk = PerkType.CircuitFailsafe,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    Notes = "Technician capstone, Engineering 50. Automatic trait.",
                },
            };
        }
    }
}
