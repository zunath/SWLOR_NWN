using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class DevicesFieldEngineerPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.BlasterBeacon,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.BlasterBeacon1, FeatType.BlasterBeacon2, FeatType.BlasterBeacon3 },
                },
                new()
                {
                    Perk = PerkType.BeaconTargeting,
                    MaxLevel = 2,
                    Prices = new[] { 3, 5 },
                    GrantedFeats = new[] { FeatType.BeaconTargetingTrait },
                },
                new()
                {
                    Perk = PerkType.IncendiaryField,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 5 },
                    GrantedFeats = new[] { FeatType.IncendiaryField1, FeatType.IncendiaryField2, FeatType.IncendiaryField3 },
                },
                new()
                {
                    Perk = PerkType.SignalJammer,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SignalJammer1 },
                },
                new()
                {
                    Perk = PerkType.RemoteCharge,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.RemoteCharge1, FeatType.RemoteCharge2 },
                },
                new()
                {
                    Perk = PerkType.ShockBeacon,
                    MaxLevel = 2,
                    Prices = new[] { 4, 5 },
                    GrantedFeats = new[] { FeatType.ShockBeacon1, FeatType.ShockBeacon2 },
                },
                new()
                {
                    Perk = PerkType.DiagnosticSweep,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.DiagnosticSweepTrait },
                },
                new()
                {
                    Perk = PerkType.KillzoneBeacon,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.KillzoneBeacon1 },
                },
            };
        }
    }
}
