using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class FirstAidCombatPharmacologyPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.AdrenalStim,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.AdrenalStim1, FeatType.AdrenalStim2, FeatType.AdrenalStim3 },
                },
                new()
                {
                    Perk = PerkType.Shielding,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.Shielding1, FeatType.Shielding2, FeatType.Shielding3 },
                },
                new()
                {
                    Perk = PerkType.Coagulant,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.CoagulantTrait },
                },
                new()
                {
                    Perk = PerkType.PainSuppressant,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.PainSuppressant1, FeatType.PainSuppressant2 },
                },
                new()
                {
                    Perk = PerkType.Antitoxin,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.Antitoxin1 },
                },
                new()
                {
                    Perk = PerkType.FieldPharmacist,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 3 },
                    GrantedFeats = new[] { FeatType.FieldPharmacistTrait },
                },
                new()
                {
                    Perk = PerkType.FocusStim,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.FocusStim1, FeatType.FocusStim2 },
                },
                new()
                {
                    Perk = PerkType.EmergencyCocktail,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.EmergencyCocktail1 },
                },
            };
        }
    }
}
