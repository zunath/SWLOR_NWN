using System.Collections.Generic;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage
{
    public class HeavyVibrobladePerkCoverage : IPerkCoverageSource
    {
        public List<PerkCoverageCase> BuildCases()
        {
            return new List<PerkCoverageCase>
            {
                new()
                {
                    Perk = PerkType.AbsoluteDefense,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.AbsoluteDefense1 },
                },
                new()
                {
                    Perk = PerkType.AngerStrike,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.AngerStrikeTrait },
                },
                new()
                {
                    Perk = PerkType.BastionStance,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.BastionStance1 },
                },
                new()
                {
                    Perk = PerkType.BlazingSpikes,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.BlazingSpikes1 },
                },
                new()
                {
                    Perk = PerkType.BloodWeapon,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.BloodWeaponTrait },
                },
                new()
                {
                    Perk = PerkType.Bloodlust,
                    MaxLevel = 1,
                    Prices = new[] { 5 },
                    GrantedFeats = new[] { FeatType.BloodlustTrait },
                },
                new()
                {
                    Perk = PerkType.CriticalWard,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.CriticalWardTrait },
                },
                new()
                {
                    Perk = PerkType.CrushingBlow,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.CrushingBlowTrait },
                },
                new()
                {
                    Perk = PerkType.DefensiveHarmony,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.DefensiveHarmonyTrait },
                },
                new()
                {
                    Perk = PerkType.Earthshatter,
                    MaxLevel = 2,
                    Prices = new[] { 3, 3 },
                    GrantedFeats = new[] { FeatType.Earthshatter1, FeatType.Earthshatter2 },
                },
                new()
                {
                    Perk = PerkType.EssenceHunter,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.EssenceHunterTrait },
                },
                new()
                {
                    Perk = PerkType.EssenceTap,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.EssenceTapTrait },
                },
                new()
                {
                    Perk = PerkType.Flash,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.Flash1 },
                },
                new()
                {
                    Perk = PerkType.FortressStrike,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 4 },
                    GrantedFeats = new[] { FeatType.FortressStrike1, FeatType.FortressStrike2, FeatType.FortressStrike3 },
                },
                new()
                {
                    Perk = PerkType.GuardiansReaping,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.GuardiansReapingTrait },
                },
                new()
                {
                    Perk = PerkType.GuardiansResolve,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.GuardiansResolveTrait },
                },
                new()
                {
                    Perk = PerkType.LastStand,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.LastStandTrait },
                },
                new()
                {
                    Perk = PerkType.LifeSiphon,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.LifeSiphonTrait },
                },
                new()
                {
                    Perk = PerkType.Rampart,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.Rampart1 },
                },
                new()
                {
                    Perk = PerkType.SacrificialBlade,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.SacrificialBlade1 },
                },
                new()
                {
                    Perk = PerkType.SoulAmplification,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SoulAmplificationTrait },
                },
                new()
                {
                    Perk = PerkType.SoulAscension,
                    MaxLevel = 1,
                    Prices = new[] { 6 },
                    GrantedFeats = new[] { FeatType.SoulAscensionTrait },
                },
                new()
                {
                    Perk = PerkType.SoulBarrier,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SoulBarrierTrait },
                },
                new()
                {
                    Perk = PerkType.SoulBurst,
                    MaxLevel = 1,
                    Prices = new[] { 3 },
                    GrantedFeats = new[] { FeatType.SoulBurst1 },
                },
                new()
                {
                    Perk = PerkType.SoulDevourer,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SoulDevourer1 },
                },
                new()
                {
                    Perk = PerkType.SoulReaping,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SoulReapingTrait },
                },
                new()
                {
                    Perk = PerkType.SoulSacrifice,
                    MaxLevel = 1,
                    Prices = new[] { 2 },
                    GrantedFeats = new[] { FeatType.SoulSacrificeTrait },
                },
                new()
                {
                    Perk = PerkType.SoulStorm,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.SoulStorm1 },
                },
                new()
                {
                    Perk = PerkType.SoulStrike,
                    MaxLevel = 3,
                    Prices = new[] { 2, 2, 3 },
                    GrantedFeats = new[] { FeatType.SoulStrike1, FeatType.SoulStrike2, FeatType.SoulStrike3 },
                },
                new()
                {
                    Perk = PerkType.UnbreakableWill,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.UnbreakableWillTrait },
                },
                new()
                {
                    Perk = PerkType.VampiricFury,
                    MaxLevel = 1,
                    Prices = new[] { 4 },
                    GrantedFeats = new[] { FeatType.VampiricFuryTrait },
                },
            };
        }
    }
}
