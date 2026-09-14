using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class PistolPerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.DisarmingShot,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.DisarmingShot1, FeatType.DisarmingShot2, FeatType.DisarmingShot3, FeatType.DisarmingShot4 },
                },
                new()
                {
                    Perk = PerkType.MobileFootwork,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.MobileFootworkTrait },
                },
                new()
                {
                    Perk = PerkType.SnapRoll,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.SnapRollTrait },
                },
                new()
                {
                    Perk = PerkType.InterruptingShot,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.InterruptingShot1, FeatType.InterruptingShot2, FeatType.InterruptingShot3 },
                },
                new()
                {
                    Perk = PerkType.KitingInstinct,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.KitingInstinctTrait },
                },
                new()
                {
                    Perk = PerkType.PointBlankBurst,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.PointBlankBurst1, FeatType.PointBlankBurst2 },
                },
                new()
                {
                    Perk = PerkType.SkirmisherStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SkirmisherStance1 },
                },
                new()
                {
                    Perk = PerkType.DuelistsDistance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.DuelistsDistanceTrait },
                },
                new()
                {
                    Perk = PerkType.EvasiveReload,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.EvasiveReloadTrait },
                },
                new()
                {
                    Perk = PerkType.LastWord,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.LastWord1 },
                },
                new()
                {
                    Perk = PerkType.QuickDraw,
                    MaxLevel = 4,
                    Prices = new[] { 2, 2, 3, 5 },
                    GrantedFeats = new[] { FeatType.QuickDraw1, FeatType.QuickDraw2, FeatType.QuickDraw3, FeatType.QuickDraw4 },
                },
                new()
                {
                    Perk = PerkType.LuckyChamber,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.LuckyChamberTrait },
                },
                new()
                {
                    Perk = PerkType.RapidShot,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.RapidShotTrait },
                },
                new()
                {
                    Perk = PerkType.DoubleShot,
                    MaxLevel = 3,
                    Prices = new[] { 2, 4, 4 },
                    GrantedFeats = new[] { FeatType.DoubleShot1, FeatType.DoubleShot2, FeatType.DoubleShot3 },
                },
                new()
                {
                    Perk = PerkType.DeadeyeReload,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.DeadeyeReloadTrait },
                },
                new()
                {
                    Perk = PerkType.FanTheHammer,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.FanTheHammer1, FeatType.FanTheHammer2 },
                },
                new()
                {
                    Perk = PerkType.GamblerStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.GamblerStance1 },
                },
                new()
                {
                    Perk = PerkType.HighNoon,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.HighNoonTrait },
                },
                new()
                {
                    Perk = PerkType.ReloadTempo,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.ReloadTempoTrait },
                },
                new()
                {
                    Perk = PerkType.DeadMansHand,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.DeadMansHand1 },
                },
            };
        }
    }
}
