using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class ForceLightConsularPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.ThrowRock,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.ThrowRock1, FeatType.ThrowRock2, FeatType.ThrowRock3 },
                },
                new()
                {
                    Perk = PerkType.ForceBurst,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ForceBurst1 },
                },
                new()
                {
                    Perk = PerkType.Benevolence,
                    MaxLevel = 3,
                    Prices = new[] { 2, 3, 4 },
                    GrantedFeats = new[] { FeatType.Benevolence1, FeatType.Benevolence2, FeatType.Benevolence3 },
                },
                new()
                {
                    Perk = PerkType.ForceJudgment,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.ForceJudgment1, FeatType.ForceJudgment2, FeatType.ForceJudgment3 },
                },
                new()
                {
                    Perk = PerkType.RadiantLance,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.RadiantLance1, FeatType.RadiantLance2, FeatType.RadiantLance3 },
                },
                new()
                {
                    Perk = PerkType.Renewal,
                    MaxLevel = 3,
                    Prices = new[] { 3, 4, 4 },
                    GrantedFeats = new[] { FeatType.Renewal1, FeatType.Renewal2, FeatType.Renewal3 },
                },
                new()
                {
                    Perk = PerkType.SereneFocus,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.SereneFocusTrait },
                },
                new()
                {
                    Perk = PerkType.MindTrick,
                    MaxLevel = 2,
                    Prices = new[] { 3, 4 },
                    GrantedFeats = new[] { FeatType.MindTrick1, FeatType.MindTrick2 },
                },
                new()
                {
                    Perk = PerkType.ForceMend,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ForceMendTrait },
                },
                new()
                {
                    Perk = PerkType.ForceSanctuary,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.ForceSanctuary1 },
                },
                new()
                {
                    Perk = PerkType.HarmonicRestoration,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.HarmonicRestorationTrait },
                },
            };
        }
    }
}
