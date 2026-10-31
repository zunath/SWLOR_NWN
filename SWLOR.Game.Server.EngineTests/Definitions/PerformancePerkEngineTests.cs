using System.Linq;
using System.Threading.Tasks;
using SWLOR.Game.Server.EngineTests.Framework;
using SWLOR.Game.Server.Service.PerformanceService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.EngineTests.Definitions
{
    /// <summary>
    /// Behavior of the Performance perk lines on a native player fixture: the feat and the performer StatType values each
    /// bought rank grants, which routine or set each perk unlocks, and what the capstones allow. The expected values are
    /// written out from the perk definition (not read back from it) so an unintended change fails here.
    /// </summary>
    public static class PerformancePerkEngineTests
    {
        private sealed class LevelExpectation
        {
            public LevelExpectation(FeatType feat, (StatType Stat, int Value)[] stats)
            {
                Feat = feat;
                Stats = stats;
            }

            public FeatType Feat { get; }
            public (StatType Stat, int Value)[] Stats { get; }
        }

        private static LevelExpectation Level(FeatType feat, params (StatType Stat, int Value)[] stats)
        {
            return new LevelExpectation(feat, stats);
        }

        private static readonly StatType[] PerformanceStats =
        {
            StatType.PerformanceDanceXPBonus,
            StatType.PerformanceDanceDebtClearPercent,
            StatType.PerformanceMusicXPBonus,
            StatType.PerformanceMusicDebtClearPercent,
            StatType.PerformanceBankingBonusPercent,
            StatType.PerformanceHarmonyBankingBonusPercent,
            StatType.PerformanceInspiredCapBonusMinutes,
            StatType.PerformanceRangeBonusMeters,
            StatType.PerformanceLightFeetSpeedPercent,
            StatType.PerformanceSteadyHandsBonus,
            StatType.PerformanceAnglersLullabyBonus,
            StatType.PerformanceRoutineAttributeBonus,
            StatType.PerformanceSetAttributeBonus
        };

        // Dance and Play Instrument are the two perks whose rank feat replaces the lower rank's feat.
        private static readonly PerkType[] RankReplacingPerks = { PerkType.Dance, PerkType.PlayInstrument };

        private static readonly (PerkType Perk, LevelExpectation[] Levels)[] Expectations =
        {
            // Dancing
            (PerkType.Dance, new[]
            {
                Level(FeatType.Dance1, (StatType.PerformanceDanceXPBonus, 6), (StatType.PerformanceDanceDebtClearPercent, 1)),
                Level(FeatType.Dance2, (StatType.PerformanceDanceXPBonus, 12), (StatType.PerformanceDanceDebtClearPercent, 2)),
                Level(FeatType.Dance3, (StatType.PerformanceDanceXPBonus, 18), (StatType.PerformanceDanceDebtClearPercent, 3)),
                Level(FeatType.Dance4, (StatType.PerformanceDanceXPBonus, 24), (StatType.PerformanceDanceDebtClearPercent, 4)),
            }),
            (PerkType.LightFeet, new[]
            {
                Level(FeatType.LightFeetTrait, (StatType.PerformanceLightFeetSpeedPercent, 10)),
                Level(FeatType.LightFeet2Trait, (StatType.PerformanceLightFeetSpeedPercent, 15)),
            }),
            (PerkType.CrowdPleaser, new[]
            {
                Level(FeatType.CrowdPleaserTrait, (StatType.PerformanceBankingBonusPercent, 10)),
                Level(FeatType.CrowdPleaser2Trait, (StatType.PerformanceBankingBonusPercent, 20)),
            }),
            (PerkType.PowerStep, new[] { Level(FeatType.PowerStepTrait) }),
            (PerkType.Encore, new[]
            {
                Level(FeatType.EncoreTrait, (StatType.PerformanceInspiredCapBonusMinutes, 20)),
                Level(FeatType.Encore2Trait, (StatType.PerformanceInspiredCapBonusMinutes, 40)),
                Level(FeatType.Encore3Trait, (StatType.PerformanceInspiredCapBonusMinutes, 60)),
            }),
            (PerkType.Quickstep, new[] { Level(FeatType.QuickstepTrait) }),
            (PerkType.VigorReel, new[] { Level(FeatType.VigorReelTrait) }),
            (PerkType.PartnerDance, new[] { Level(FeatType.PartnerDanceTrait) }),
            (PerkType.StandingOvation, new[] { Level(FeatType.StandingOvationTrait) }),
            (PerkType.PeakForm, new[] { Level(FeatType.PeakFormTrait, (StatType.PerformanceRoutineAttributeBonus, 1)) }),
            (PerkType.Showstopper, new[] { Level(FeatType.ShowstopperTrait) }),

            // Instruments
            (PerkType.PlayInstrument, new[]
            {
                Level(FeatType.PlayInstrument1, (StatType.PerformanceMusicXPBonus, 6), (StatType.PerformanceMusicDebtClearPercent, 1)),
                Level(FeatType.PlayInstrument2, (StatType.PerformanceMusicXPBonus, 12), (StatType.PerformanceMusicDebtClearPercent, 2)),
                Level(FeatType.PlayInstrument3, (StatType.PerformanceMusicXPBonus, 18), (StatType.PerformanceMusicDebtClearPercent, 3)),
                Level(FeatType.PlayInstrument4, (StatType.PerformanceMusicXPBonus, 24), (StatType.PerformanceMusicDebtClearPercent, 4)),
            }),
            (PerkType.SteadyHands, new[]
            {
                Level(FeatType.SteadyHandsTrait, (StatType.PerformanceSteadyHandsBonus, 2)),
                Level(FeatType.SteadyHands2Trait, (StatType.PerformanceSteadyHandsBonus, 4)),
            }),
            (PerkType.Harmony, new[]
            {
                Level(FeatType.HarmonyTrait, (StatType.PerformanceHarmonyBankingBonusPercent, 10)),
                Level(FeatType.Harmony2Trait, (StatType.PerformanceHarmonyBankingBonusPercent, 20)),
            }),
            (PerkType.KeenEar, new[] { Level(FeatType.KeenEarTrait) }),
            (PerkType.Resonance, new[]
            {
                Level(FeatType.ResonanceTrait, (StatType.PerformanceRangeBonusMeters, 3)),
                Level(FeatType.Resonance2Trait, (StatType.PerformanceRangeBonusMeters, 6)),
            }),
            (PerkType.AnglersLullaby, new[] { Level(FeatType.AnglersLullabyTrait, (StatType.PerformanceAnglersLullabyBonus, 10)) }),
            (PerkType.StillWaters, new[] { Level(FeatType.StillWatersTrait) }),
            (PerkType.Duet, new[] { Level(FeatType.DuetTrait) }),
            (PerkType.Requiem, new[] { Level(FeatType.RequiemTrait) }),
            (PerkType.SilverTongue, new[] { Level(FeatType.SilverTongueTrait) }),
            (PerkType.PerfectPitch, new[] { Level(FeatType.PerfectPitchTrait, (StatType.PerformanceSetAttributeBonus, 1)) }),
            (PerkType.Virtuoso, new[] { Level(FeatType.VirtuosoTrait) }),
        };

        // Each routine or set and the one perk that unlocks it; Cantina Shuffle and Cantina Standards come with the toggle perks.
        private static readonly (PerformanceRoutineType Routine, PerkType Perk)[] Routines =
        {
            (PerformanceRoutineType.CantinaShuffle, PerkType.Dance),
            (PerformanceRoutineType.LightFeet, PerkType.LightFeet),
            (PerformanceRoutineType.PowerStep, PerkType.PowerStep),
            (PerformanceRoutineType.Quickstep, PerkType.Quickstep),
            (PerformanceRoutineType.VigorReel, PerkType.VigorReel),
        };

        private static readonly (PerformanceSetType Set, PerkType Perk)[] Sets =
        {
            (PerformanceSetType.CantinaStandards, PerkType.PlayInstrument),
            (PerformanceSetType.SteadyHands, PerkType.SteadyHands),
            (PerformanceSetType.KeenEar, PerkType.KeenEar),
            (PerformanceSetType.AnglersLullaby, PerkType.AnglersLullaby),
            (PerformanceSetType.StillWaters, PerkType.StillWaters),
            (PerformanceSetType.SilverTongue, PerkType.SilverTongue),
        };

        [EngineTest("Performance perk ranks grant their feat and performer stat values", Category = "PerformancePerks", TimeoutSeconds = 120f)]
        public static async Task RanksGrantFeatsAndStats(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            try
            {
                ctx.AssertEqual(23, Expectations.Length, "every Performance perk is expected");
                var ranksVerified = 0;
                foreach (var (perk, levels) in Expectations)
                {
                    for (var rank = 1; rank <= levels.Length; rank++)
                    {
                        var current = levels[rank - 1];
                        fixture.Update(record => record.Perks[perk] = rank);
                        Perk.SyncGrantedFeats(player, perk, rank, true);

                        ctx.AssertEqual(rank, Perk.GetPerkLevel(player, perk), $"{perk} rank {rank} is the player's perk level");
                        ctx.Assert(GetHasFeat(current.Feat, player), $"{perk} rank {rank} grants {current.Feat}");
                        for (var higher = rank + 1; higher <= levels.Length; higher++)
                            ctx.Assert(!GetHasFeat(levels[higher - 1].Feat, player), $"{perk} rank {rank} does not yet grant {levels[higher - 1].Feat}");
                        if (rank > 1 && RankReplacingPerks.Contains(perk))
                            ctx.Assert(!GetHasFeat(levels[rank - 2].Feat, player), $"{perk} rank {rank} replaces {levels[rank - 2].Feat}");

                        // Ranks replace each other: the value is this rank's, never a running total.
                        foreach (var stat in PerformanceStats)
                        {
                            var expected = current.Stats.Where(entry => entry.Stat == stat).Sum(entry => entry.Value);
                            ctx.AssertEqual(expected, Stat.GetStatAdjustment(player, stat), $"{perk} rank {rank} {stat}");
                        }

                        ranksVerified++;
                    }

                    fixture.Update(record => record.Perks.Remove(perk));
                    foreach (var stat in PerformanceStats)
                        ctx.AssertEqual(0, Stat.GetStatAdjustment(player, stat), $"{perk} removed leaves no {stat}");
                }

                ctx.SetResultDetail($"{Expectations.Length} perks, {ranksVerified} ranks: feat grant, rank replacement and {PerformanceStats.Length} performer stats checked per rank.");
            }
            finally
            {
                // Queued last so it runs after Dispose clears the player flag; otherwise the
                // fixture stays at the spawn point through the rest of the sweep.
                AssignCommand(player, () => DestroyObject(player));
            }
        }

        [EngineTest("Performance routine and set perks unlock only their own routine or set", Category = "PerformancePerks", TimeoutSeconds = 60f)]
        public static async Task PerksUnlockTheirOwnRoutineOrSet(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            try
            {
                foreach (var (routine, _) in Routines)
                    ctx.Assert(!Performance.IsRoutineUnlocked(player, routine), $"{routine} is locked with no perks");
                foreach (var (set, _) in Sets)
                    ctx.Assert(!Performance.IsSetUnlocked(player, set), $"{set} is locked with no perks");

                foreach (var unlockPerk in Routines.Select(entry => entry.Perk).Concat(Sets.Select(entry => entry.Perk)))
                {
                    fixture.Update(record => record.Perks[unlockPerk] = 1);
                    foreach (var (routine, perk) in Routines)
                        ctx.AssertEqual(perk == unlockPerk, Performance.IsRoutineUnlocked(player, routine), $"{unlockPerk} unlocks {routine}");
                    foreach (var (set, perk) in Sets)
                        ctx.AssertEqual(perk == unlockPerk, Performance.IsSetUnlocked(player, set), $"{unlockPerk} unlocks {set}");

                    fixture.Update(record => record.Perks.Remove(unlockPerk));
                }
            }
            finally
            {
                AssignCommand(player, () => DestroyObject(player));
            }
        }

        [EngineTest("Performance routine and set choices follow unlocks and the capstones", Category = "PerformancePerks", TimeoutSeconds = 60f)]
        public static async Task ChoicesFollowUnlocksAndCapstones(EngineTestContext ctx)
        {
            using var fixture = await PlayerAbilityFixture.CreateAsync(ctx);
            var player = fixture.Creature;
            try
            {
                var preference = Performance.GetPreference(player);

                // A locked routine or set cannot be chosen; buying its perk allows it.
                ctx.Assert(!Performance.SetRoutine(player, PerformanceRoutineType.PowerStep), "Power Step cannot be chosen before its perk");
                ctx.Assert(!Performance.SetSet(player, PerformanceSetType.KeenEar), "Keen Ear cannot be chosen before its perk");
                fixture.Update(record =>
                {
                    record.Perks[PerkType.PowerStep] = 1;
                    record.Perks[PerkType.LightFeet] = 1;
                    record.Perks[PerkType.KeenEar] = 1;
                    record.Perks[PerkType.SteadyHands] = 1;
                });
                ctx.Assert(Performance.SetRoutine(player, PerformanceRoutineType.PowerStep), "Power Step can be chosen after its perk");
                ctx.AssertEqual(PerformanceRoutineType.PowerStep, preference.Routine, "the chosen routine is remembered");
                ctx.Assert(Performance.SetSet(player, PerformanceSetType.KeenEar), "Keen Ear can be chosen after its perk");
                ctx.AssertEqual(PerformanceSetType.KeenEar, preference.Set, "the chosen set is remembered");

                // Blending a second routine or weaving a second set needs the capstone as well as the unlock.
                ctx.Assert(!Performance.SetSecondRoutine(player, PerformanceRoutineType.LightFeet), "a second routine needs Showstopper");
                ctx.Assert(!Performance.SetSecondSet(player, PerformanceSetType.SteadyHands), "a second set needs Virtuoso");
                fixture.Update(record =>
                {
                    record.Perks[PerkType.Showstopper] = 1;
                    record.Perks[PerkType.Virtuoso] = 1;
                });
                ctx.Assert(Performance.SetSecondRoutine(player, PerformanceRoutineType.LightFeet), "Showstopper allows a second unlocked routine");
                ctx.AssertEqual(PerformanceRoutineType.LightFeet, preference.SecondRoutine, "the second routine is remembered");
                ctx.Assert(Performance.SetSecondSet(player, PerformanceSetType.SteadyHands), "Virtuoso allows a second unlocked set");
                ctx.AssertEqual(PerformanceSetType.SteadyHands, preference.SecondSet, "the second set is remembered");
                ctx.Assert(!Performance.SetSecondRoutine(player, PerformanceRoutineType.Quickstep), "a second routine must be unlocked");
                ctx.Assert(!Performance.SetSecondSet(player, PerformanceSetType.StillWaters), "a second set must be unlocked");
            }
            finally
            {
                AssignCommand(player, () => DestroyObject(player));
            }
        }
    }
}
