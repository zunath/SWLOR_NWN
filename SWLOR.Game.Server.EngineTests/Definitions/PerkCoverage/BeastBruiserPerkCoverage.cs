using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class BeastBruiserPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.PoisonBreath,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.PoisonBreath1, FeatType.PoisonBreath2, FeatType.PoisonBreath3 },
                },
                new()
                {
                    Perk = PerkType.IceBreath,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.IceBreath1, FeatType.IceBreath2, FeatType.IceBreath3 },
                },
                new()
                {
                    Perk = PerkType.CrushingSlam,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.CrushingSlam1, FeatType.CrushingSlam2, FeatType.CrushingSlam3 },
                },
                new()
                {
                    Perk = PerkType.EnduranceLink,
                    MaxLevel = 3,
                    Prices = new[] { 3, 3, 4 },
                    GrantedFeats = new[] { FeatType.EnduranceLinkTrait },
                },
                new()
                {
                    Perk = PerkType.VenomousHide,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.VenomousHideTrait },
                },
                new()
                {
                    Perk = PerkType.Rampage,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.Rampage1, FeatType.Rampage2 },
                },
                new()
                {
                    Perk = PerkType.PrimalOverrun,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.PrimalOverrun1 },
                },
            };
        }
    }
}
