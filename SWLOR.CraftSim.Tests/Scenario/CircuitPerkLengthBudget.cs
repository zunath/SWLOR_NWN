using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    /// <summary>
    /// How many actions the adaptive player needs with the draft magnitudes and with the shipped ones, which explains gate 4's verdict for
    /// the perked scenarios (<c>design/testing/crafting-revamp/engineering-perk-gates.md</c>). Explicit: it is a long run, never part of a
    /// normal test run. The report goes to <c>%TEMP%/claude-crafting-revamp-perks-engineering/length-budget.txt</c>.
    /// </summary>
    [TestFixture, Explicit]
    public class CircuitPerkLengthBudget
    {
        private sealed record Values(
            int Cw1, int Cw2, int Cap, int Heat1, int Heat2, int OcLoad, int OcBoost, int OverLoad, int OverCp, int Shock,
            int Ins1, int Ins2, int Fc1, int Fc2, int Signal, int PatchRestore, int PatchCp, int SafeGrace, int SafeBoost)
        {
            /// <summary>The magnitudes of the approved draft.</summary>
            public static readonly Values Draft = new(1, 3, 10, 1, 2, 1, 3, 1, 1, 33, 1, 2, 10, 25, 10, 10, 2, 2, -3);

            /// <summary>The magnitudes in CircuitPerkRules.</summary>
            public static readonly Values Shipped = new(
                CircuitPerkRules.ConductiveWiringLoadBoostRank1, CircuitPerkRules.ConductiveWiringLoadBoostRank2, CircuitPerkRules.CapacitorBankDischargePercent,
                CircuitPerkRules.HeatSinksLineRank1, CircuitPerkRules.HeatSinksLineRank2, CircuitPerkRules.OverclockExtraLoad, CircuitPerkRules.OverclockLoadBoostPercent,
                CircuitPerkRules.OverdriveBoostExtraLoad, CircuitPerkRules.OverdriveBoostCpReduction, CircuitPerkRules.ShockAbsorbersReductionPercent,
                CircuitPerkRules.InsulatedTracesReductionRank1, CircuitPerkRules.InsulatedTracesReductionRank2, CircuitPerkRules.FineCalibrationPercentRank1,
                CircuitPerkRules.FineCalibrationPercentRank2, CircuitPerkRules.SignalLockChancePercent, CircuitPerkRules.PrecisionPatchExtraRestore,
                CircuitPerkRules.PrecisionPatchCpReduction, CircuitPerkRules.SafeModeOverloadGrace, CircuitPerkRules.SafeModeLoadBoostPercent);
        }

        private static CircuitPerkStats Stats(int rank, Values v, bool overclocker, bool technician, CircuitStyle style)
        {
            var s = new CircuitPerkStats();
            if (overclocker)
            {
                s = s with
                {
                    LoadBoostPercent = rank >= 45 ? v.Cw2 : rank >= 5 ? v.Cw1 : 0,
                    DischargePowerPercent = rank >= 10 ? v.Cap : 0,
                    OverloadLine = rank >= 35 ? v.Heat2 : rank >= 15 ? v.Heat1 : 0,
                    CascadeDischarge = rank >= 25,
                    BoostLoad = rank >= 30 ? v.OverLoad : 0,
                    BoostCpReduction = rank >= 30 ? v.OverCp : 0,
                    OverloadIntegrityReductionPercent = rank >= 40 ? v.Shock : 0,
                    Surge = rank >= 50
                };
            }

            if (technician)
            {
                s = s with
                {
                    ActionIntegrityReduction = rank >= 40 ? v.Ins2 : rank >= 5 ? v.Ins1 : 0,
                    TunePowerPercent = rank >= 45 ? v.Fc2 : rank >= 10 ? v.Fc1 : 0,
                    ForecastTurns = rank >= 15 ? 1 : 0,
                    MicroTune = rank >= 25,
                    SignalChancePercent = rank >= 30 ? v.Signal : 0,
                    PatchRestore = rank >= 35 ? v.PatchRestore : 0,
                    PatchCpReduction = rank >= 35 ? v.PatchCp : 0,
                    Failsafe = rank >= 50
                };
            }

            if (rank >= CircuitRules.StyleRank && style == CircuitStyle.Overclock)
                s = s with { ExtraLoadPerBuildAction = v.OcLoad, LoadBoostPercent = s.LoadBoostPercent + v.OcBoost };
            if (rank >= CircuitRules.StyleRank && style == CircuitStyle.SafeMode)
                s = s with { OverloadGrace = v.SafeGrace, LoadBoostPercent = s.LoadBoostPercent + v.SafeBoost };

            return s;
        }

        [Test]
        public void MeanActionsAtBestGear()
        {
            var builds = new (string Name, bool Overclocker, bool Technician, CircuitStyle Style)[]
            {
                ("No perks", false, false, CircuitStyle.None),
                ("Overclock alone", false, false, CircuitStyle.Overclock),
                ("Safe Mode alone", false, false, CircuitStyle.SafeMode),
                ("Overclocker, no style", true, false, CircuitStyle.None),
                ("Overclocker + Overclock", true, false, CircuitStyle.Overclock),
                ("Technician, no style", false, true, CircuitStyle.None),
                ("Technician + Safe Mode", false, true, CircuitStyle.SafeMode),
                ("Both lines + Overclock", true, true, CircuitStyle.Overclock),
                ("Both lines + Safe Mode", true, true, CircuitStyle.SafeMode),
            };

            var lines = new List<string> { "| Band | Build | Draft magnitudes | Shipped magnitudes |", "| --- | --- | --- | --- |" };
            var gear = SharedData.Gear.Levels(SkillType.Engineering).Last();
            foreach (var (levelLow, levelHigh) in new[] { (21, 30), (31, 40), (41, 53) })
            {
                var recipes = SharedData.Recipes.ForSkill(SkillType.Engineering)
                    .Where(recipe => recipe.Level >= levelLow && recipe.Level <= levelHigh && recipe.Detail?.MinigameParameters is CircuitParameters)
                    .OrderBy(recipe => recipe.Level).ThenBy(recipe => recipe.Resref).ToList();
                var sample = ScenarioCohort.Sample(recipes, 6);
                foreach (var (name, overclocker, technician, style) in builds)
                {
                    var draft = Actions(sample, gear, rank => Stats(rank, Values.Draft, overclocker, technician, style), 16);
                    var shipped = Actions(sample, gear, rank => Stats(rank, Values.Shipped, overclocker, technician, style), 16);
                    lines.Add($"| {levelLow}-{levelHigh} | {name} | {draft:F1} | {shipped:F1} |");
                }
            }

            var path = Path.Combine(Path.GetTempPath(), "claude-crafting-revamp-perks-engineering", "length-budget.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, lines);
        }

        private static double Actions(IReadOnlyList<RecipeEntry> recipes, GearLevel gear, Func<int, CircuitPerkStats> statsAt, int runs)
        {
            double actions = 0;
            var total = 0;
            Parallel.ForEach(recipes, recipe =>
            {
                var rank = Math.Min(recipe.Level, ScenarioCohort.MaximumRank);
                var stats = statsAt(rank);
                var level = Craft.GetRecipeLevelDetail(recipe.Level);
                for (var run = 0; run < runs; run++)
                {
                    var setup = new CircuitSetup(
                        recipe.Detail.MinigameParameters as CircuitParameters, recipe.Level, level.Progress, level.Quality, level.Durability, rank,
                        gear.Craftsmanship, gear.Control, gear.CpBonus, 0, stats);
                    var random = SimMinigameRandom.ForRun(1, SimStreams.Adaptive, (int)recipe.Type, rank, 0, run);
                    var result = CraftPlayer.Play(new CircuitEngine(random, setup), new AdaptivePolicy(AdaptivePolicyOptions.Default, run + 1), 100);
                    lock (recipes)
                    {
                        actions += result.Actions;
                        total++;
                    }
                }
            });

            return actions / total;
        }
    }
}
