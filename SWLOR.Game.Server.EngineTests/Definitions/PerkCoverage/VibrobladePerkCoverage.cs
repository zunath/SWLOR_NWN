using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class VibrobladePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.BerserkerStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.BerserkerStance1 },
                },
                new()
                {
                    Perk = PerkType.BloodFrenzy,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.BloodFrenzyTrait },
                },
                new()
                {
                    Perk = PerkType.CoveringStrike,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.CoveringStrike1, FeatType.CoveringStrike2, FeatType.CoveringStrike3 },
                },
                new()
                {
                    Perk = PerkType.DefensiveStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.DefensiveStance1 },
                },
                new()
                {
                    Perk = PerkType.Executioner,
                    MaxLevel = 2,
                    Prices = new[] { 2, 2 },
                    GrantedFeats = new[] { FeatType.ExecutionerTrait },
                },
                new()
                {
                    Perk = PerkType.Fortification,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.FortificationTrait },
                },
                new()
                {
                    Perk = PerkType.Invincible,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.Invincible1 },
                },
                new()
                {
                    Perk = PerkType.Alacrity,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.AlacrityTrait },
                },
                new()
                {
                    Perk = PerkType.Bulwark,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.BulwarkTrait },
                },
                new()
                {
                    Perk = PerkType.RendingStrike,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.RendingStrike1, FeatType.RendingStrike2 },
                },
                new()
                {
                    Perk = PerkType.RiotBlade,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.RiotBlade1, FeatType.RiotBlade2, FeatType.RiotBlade3, FeatType.RiotBlade4 },
                },
                new()
                {
                    Perk = PerkType.Rundown,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.RundownTrait },
                },
                new()
                {
                    Perk = PerkType.FollowThrough,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.FollowThroughTrait },
                },
                new()
                {
                    Perk = PerkType.SavageCleave,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.SavageCleave1, FeatType.SavageCleave2, FeatType.SavageCleave3 },
                },
                new()
                {
                    Perk = PerkType.SavageReflexes,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SavageReflexesTrait },
                },
                new()
                {
                    Perk = PerkType.ShieldBash,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.ShieldBash1, FeatType.ShieldBash2, FeatType.ShieldBash3, FeatType.ShieldBash4 },
                },
                new()
                {
                    Perk = PerkType.ShieldTraining,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.ShieldTrainingTrait },
                },
                new()
                {
                    Perk = PerkType.ShieldWall,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.ShieldWall1, FeatType.ShieldWall2 },
                },
                new()
                {
                    Perk = PerkType.Unbreakable,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.UnbreakableTrait },
                },
            };
        }
    }
}
