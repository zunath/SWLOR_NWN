using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class VibroknifePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.PathogenStrike,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.PathogenStrike1, FeatType.PathogenStrike2, FeatType.PathogenStrike3, FeatType.PathogenStrike4 },
                },
                new()
                {
                    Perk = PerkType.Hypermetabolize,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.HypermetabolizeTrait },
                },
                new()
                {
                    Perk = PerkType.Debilitate,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.DebilitateTrait },
                },
                new()
                {
                    Perk = PerkType.VirulentBlade,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.VirulentBlade1, FeatType.VirulentBlade2, FeatType.VirulentBlade3 },
                },
                new()
                {
                    Perk = PerkType.Infection,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.InfectionTrait },
                },
                new()
                {
                    Perk = PerkType.VolatileCompound,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.VolatileCompound1, FeatType.VolatileCompound2 },
                },
                new()
                {
                    Perk = PerkType.AssassinsStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.AssassinsStance1 },
                },
                new()
                {
                    Perk = PerkType.VenomTempo,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.VenomTempoTrait },
                },
                new()
                {
                    Perk = PerkType.Propagation,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.PropagationTrait },
                },
                new()
                {
                    Perk = PerkType.ViralCascade,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.ViralCascade1 },
                },
                new()
                {
                    Perk = PerkType.VeiledStrike,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.VeiledStrike1, FeatType.VeiledStrike2, FeatType.VeiledStrike3, FeatType.VeiledStrike4 },
                },
                new()
                {
                    Perk = PerkType.ButchersTempo,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.ButchersTempoTrait },
                },
                new()
                {
                    Perk = PerkType.FirstStrike,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.FirstStrikeTrait },
                },
                new()
                {
                    Perk = PerkType.CripplingSlice,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.CripplingSlice1, FeatType.CripplingSlice2, FeatType.CripplingSlice3 },
                },
                new()
                {
                    Perk = PerkType.VenaticRecovery,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.VenaticRecoveryTrait },
                },
                new()
                {
                    Perk = PerkType.Backstab,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.Backstab1, FeatType.Backstab2 },
                },
                new()
                {
                    Perk = PerkType.ShadowflowStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ShadowflowStance1 },
                },
                new()
                {
                    Perk = PerkType.CheapShot,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.CheapShotTrait },
                },
                new()
                {
                    Perk = PerkType.Hobbled,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.HobbledTrait },
                },
                new()
                {
                    Perk = PerkType.EscapeArtist,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.EscapeArtist1 },
                },
            };
        }
    }
}
