using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class EspionagePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.Stealth,
                    MaxLevel = 4,
                    Prices = new[] { 2, 3, 3, 4 },
                },
                new()
                {
                    Perk = PerkType.BackAttack,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.BackAttackTrait },
                },
                new()
                {
                    Perk = PerkType.Slicing,
                    MaxLevel = 5,
                    Prices = new[] { 2, 3, 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.SlicingTrait },
                },
                new()
                {
                    Perk = PerkType.TacticalEscape,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.TacticalEscape1, FeatType.TacticalEscape2 },
                },
                new()
                {
                    Perk = PerkType.ShadowStep,
                    MaxLevel = 2,
                    Prices = new[] { 4, 4 },
                    GrantedFeats = new[] { FeatType.ShadowStep1, FeatType.ShadowStep2 },
                },
                new()
                {
                    Perk = PerkType.SilentStride,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SilentStrideTrait },
                },
                new()
                {
                    Perk = PerkType.GhostProtocol,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.GhostProtocol },
                },
                new()
                {
                    Perk = PerkType.Poisoncraft,
                    MaxLevel = 5,
                    Prices = new[] { 2, 3, 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.PoisoncraftTrait },
                },
                new()
                {
                    Perk = PerkType.Trapcraft,
                    MaxLevel = 4,
                    Prices = new[] { 2, 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.TrapcraftTrait },
                },
                new()
                {
                    Perk = PerkType.VenomExpertise,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.VenomExpertiseTrait },
                },
                new()
                {
                    Perk = PerkType.RazorTrap,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.RazorTrap1, FeatType.RazorTrap2 },
                },
                new()
                {
                    Perk = PerkType.ShockTrap,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ShockTrap },
                },
                new()
                {
                    Perk = PerkType.TrapManagement,
                    MaxLevel = 2,
                    Prices = new[] { 2, 4 },
                    GrantedFeats = new[] { FeatType.TrapManagementTrait },
                },
                new()
                {
                    Perk = PerkType.LastingCoatings,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.LastingCoatingsTrait },
                },
                new()
                {
                    Perk = PerkType.MasterSaboteur,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.MasterSaboteurTrait },
                },
                new()
                {
                    Perk = PerkType.FalseIdentities,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.FalseIdentitiesTrait },
                },
                new()
                {
                    Perk = PerkType.CoverStory,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.CoverStoryTrait },
                },
            };
        }
    }
}
