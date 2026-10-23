using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Forge;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Forge;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    /// <summary>
    /// What each Smithery perk buys a rank-50 crafter at no and mid gear (<c>design/testing/crafting-revamp/smithery-perk-gates.md</c>): the
    /// adaptive player's mean actions, mean quality and expected enhancements transferred with the whole build (both lines and Hot Forging)
    /// and with that one purchase removed, at the rank-50 at-level bands of gate 7 (41-45, 46-50, 51-53) with the representative
    /// enhancement item in one, two and three slots. Explicit: a long run, never part of a normal test run. The report goes to
    /// <c>%TEMP%/claude-crafting-revamp-perks-smithery-tuning/perk-effects.md</c>.
    /// </summary>
    [TestFixture, Explicit]
    public class ForgePerkEffects
    {
        private const int Rank = 50;
        private const int Runs = 16;

        private sealed record Effect(double Actions, double Quality, double Transfers1, double Transfers2, double Transfers3);

        private static readonly (string Name, Func<ForgePerkStats, ForgePerkStats> Remove)[] Purchases =
        {
            ("Plasma Efficiency", s => s with { RampCpReduction = 0 }),
            ("Heat Retention I and II", s => s with { CoolingReduction = 0 }),
            ("Overdrive Press", s => s with { OverdrivePressPercent = 0 }),
            ("Hammer Rhythm", s => s with { PressStreakPercent = 0 }),
            ("Scorch Guard", s => s with { PressOverheatIntegrityReductionPercent = 0 }),
            ("Surge Plasma", s => s with { SurgePlasmaThermal = 0 }),
            ("Fast Rebond", s => s with { RebondThermalReduction = 0, RebondCpReduction = 0 }),
            ("Red Line", s => s with { RedLineCharges = 0 }),
            ("Steady Hands", s => s with { HoneIntegrityReduction = 0 }),
            ("Clean Containment I and II", s => s with { ContainmentChancePercent = 0 }),
            ("Micro-Hone", s => s with { MicroHonePercent = 0 }),
            ("Steady Quench", s => s with { QuenchEdgePercent = 0 }),
            ("Cool Hand", s => s with { HoneHotEdgePercent = 0 }),
            ("Temper Hold", s => s with { TemperHoldActions = 0 }),
            ("Containment Focus", s => s with { ContainmentHonePercent = 0 }),
            ("Perfect Temper", s => s with { PerfectTemperPercent = 0 }),
        };

        [Test]
        public void EachPurchasesEffectAtNoAndMidGear()
        {
            var gears = SharedData.Gear.Levels(SkillType.Smithery);
            var options = new ForgeScenario().AdaptiveOptions;
            var enhanceable = SharedData.Recipes.Recipes
                .Where(recipe => recipe.Skill == SkillType.Smithery && recipe.EnhancementSlots > 0 && recipe.EnhancementType != RecipeEnhancementType.None
                                 && recipe.Detail?.MinigameParameters is ForgeParameters)
                .ToList();
            var recipes = new List<(RecipeEntry Recipe, EnhancementItem Item)>();
            foreach (var (low, high) in new[] { (41, 45), (46, 50), (51, int.MaxValue) })
            {
                var inBand = enhanceable.Where(recipe => recipe.Level >= low && recipe.Level <= high)
                    .OrderBy(recipe => recipe.Level).ThenBy(recipe => (int)recipe.Type).ToList();
                recipes.AddRange(inBand.Select(recipe => (recipe, SharedData.Enhancements.ForRecipe(recipe.EnhancementType, recipe.Level))).Where(x => x.Item2 != null));
            }

            var full = new ForgeLoadout(ForgeStyle.HotForging, true, true).StatsAt(Rank);
            var noStyle = new ForgeLoadout(ForgeStyle.None, true, true).StatsAt(Rank);
            var cold = new ForgeLoadout(ForgeStyle.ColdWorking, true, true).StatsAt(Rank);
            var lines = new List<string>
            {
                $"Rank {Rank}, the at-level bands 41-45, 46-50 and 51-53, {recipes.Count} enhanceable recipes, {Runs} runs each. The full build is both lines and Hot Forging; a row removes one purchase from it.",
                string.Empty,
                "| Purchase | Gear | Actions | Quality | Transferred, 1 slot | Transferred, 2 slots | Transferred, 3 slots |",
                "| --- | --- | --- | --- | --- | --- | --- |"
            };

            foreach (var gearIndex in new[] { 0, 1 })
            {
                var gear = gears[gearIndex];
                var baseline = Measure(recipes, gear, full, options);
                var rows = new List<(string Name, Effect Value)>();
                foreach (var (name, remove) in Purchases)
                    rows.Add((name, Measure(recipes, gear, remove(full), options)));

                rows.Add(("Hot Forging (style)", Measure(recipes, gear, noStyle, options)));
                rows.Add(("Cold Working instead of Hot Forging", Measure(recipes, gear, cold, options)));
                rows.Add(("No perks at all", Measure(recipes, gear, ForgePerkStats.None, options)));

                lines.Add($"| **Full build** | {gear.Name} | {baseline.Actions:F1} | {baseline.Quality:F1} | {baseline.Transfers1:F2} | {baseline.Transfers2:F2} | {baseline.Transfers3:F2} |");
                foreach (var (name, value) in rows)
                {
                    lines.Add($"| Without {name} | {gear.Name} | {value.Actions:F1} ({value.Actions - baseline.Actions:+0.0;-0.0;0.0}) | {value.Quality:F1} ({value.Quality - baseline.Quality:+0.0;-0.0;0.0}) | " +
                              $"{value.Transfers1:F2} ({value.Transfers1 - baseline.Transfers1:+0.00;-0.00;0.00}) | {value.Transfers2:F2} ({value.Transfers2 - baseline.Transfers2:+0.00;-0.00;0.00}) | " +
                              $"{value.Transfers3:F2} ({value.Transfers3 - baseline.Transfers3:+0.00;-0.00;0.00}) |");
                }
            }

            var path = Path.Combine(Path.GetTempPath(), "claude-crafting-revamp-perks-smithery-tuning", "perk-effects.md");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, lines);
        }

        private static Effect Measure(IReadOnlyList<(RecipeEntry Recipe, EnhancementItem Item)> recipes, GearLevel gear, ForgePerkStats stats, AdaptivePolicyOptions options)
        {
            double actions = 0, quality = 0;
            var plain = 0;
            var transfers = new double[4];
            var counts = new int[4];
            Parallel.ForEach(recipes, entry =>
            {
                var recipe = entry.Recipe;
                for (var slots = 0; slots <= 3; slots++)
                {
                    var shape = slots == 0 ? 0 : ForgeRules.EnhancementShapePercent(slots * entry.Item.ProgressPenalty, recipe.Level);
                    var thresholds = slots == 0 ? Array.Empty<int>() : CraftOutcomeRules.GetEnhancementThresholds(slots).ToArray();
                    for (var run = 0; run < Runs; run++)
                    {
                        var setup = new ForgeSetup(
                            (ForgeParameters)recipe.Detail.MinigameParameters, recipe.Level, Rank, gear.Craftsmanship, gear.Control, gear.CpBonus, shape, stats);
                        var random = SimMinigameRandom.ForRun(1, SimStreams.Adaptive, (int)recipe.Type, Rank, slots, run);
                        var result = CraftPlayer.Play(new ForgeEngine(setup, random), new AdaptivePolicy(options, run + 1), 100);
                        var percent = result.Succeeded ? result.QualityFraction * 100 : 0;
                        var moved = result.Succeeded ? thresholds.Count(threshold => percent >= threshold) : 0;
                        lock (recipes)
                        {
                            if (slots == 0)
                            {
                                actions += result.Actions;
                                quality += result.Succeeded ? percent : 0;
                                plain++;
                            }
                            else
                            {
                                transfers[slots] += moved;
                                counts[slots]++;
                            }
                        }
                    }
                }
            });

            return new Effect(actions / plain, quality / plain, transfers[1] / counts[1], transfers[2] / counts[2], transfers[3] / counts[3]);
        }
    }
}
