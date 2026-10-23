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
    /// Compares Hot Forging with Cold Working where at-level quality does not saturate: crafts under the recipe's rank, without gear and
    /// with a heavy enhancement (<c>design/testing/crafting-revamp/smithery-perk-gates.md</c>), for a crafter with no line perks and for
    /// one with both lines in full. Explicit: a long run, never part of a normal test run. The report goes to
    /// <c>%TEMP%/claude-crafting-revamp-perks-smithery/stress.txt</c>.
    /// </summary>
    [TestFixture, Explicit]
    public class ForgeStyleStress
    {
        [Test]
        public void Compare()
        {
            var gears = SharedData.Gear.Levels(SkillType.Smithery);
            var options = new ForgeScenario().AdaptiveOptions;
            var lines = new List<string>();
            var loadouts = new (string Name, ForgeLoadout Loadout)[]
            {
                ("Hot, no lines", new ForgeLoadout(ForgeStyle.HotForging, false, false)),
                ("Cold, no lines", new ForgeLoadout(ForgeStyle.ColdWorking, false, false)),
                ("none", new ForgeLoadout(ForgeStyle.None, false, false)),
                ("Hot, both", new ForgeLoadout(ForgeStyle.HotForging, true, true)),
                ("Cold, both", new ForgeLoadout(ForgeStyle.ColdWorking, true, true)),
            };

            foreach (var (levelLow, levelHigh) in new[] { (21, 30), (31, 40), (41, 53) })
            {
                var recipes = SharedData.Recipes.ForSkill(SkillType.Smithery)
                    .Where(recipe => recipe.Level >= levelLow && recipe.Level <= levelHigh && recipe.Detail?.MinigameParameters is ForgeParameters)
                    .OrderBy(recipe => recipe.Level).ThenBy(recipe => (int)recipe.Type).ToList();
                var sample = ScenarioCohort.Sample(recipes, 6);
                foreach (var (condition, rankBelow, penalty, gear) in new[]
                         {
                             ("at level, no gear", 0, 0, gears.First()),
                             ("3 ranks under, no gear", 3, 0, gears.First()),
                             ("at level, no gear, heavy enhancement", 0, 40, gears.First()),
                             ("3 ranks under, mid gear, heavy enhancement", 3, 40, gears[1]),
                         })
                {
                    // A style opens at rank 20, so a recipe the crafter would play below it is left out of a style comparison.
                    var eligible = sample.Where(recipe => Math.Min(recipe.Level, 50) - rankBelow >= ForgePerkRules.StyleRank).ToList();
                    if (eligible.Count == 0)
                        continue;

                    foreach (var (name, loadout) in loadouts)
                    {
                        var (actions, success, quality, full, n) = Play(eligible, gear, rankBelow, penalty, loadout, options, 16);
                        lines.Add($"band {levelLow}-{levelHigh} | {condition,-42} | {name,-14} | actions {actions:F1} make {success:P0} quality {quality:F1} full {full:P0} (n {n})");
                    }
                }
            }

            var path = Path.Combine(Path.GetTempPath(), "claude-crafting-revamp-perks-smithery", "stress.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, lines);
        }

        private static (double Actions, double Success, double Quality, double Full, int N) Play(
            IReadOnlyList<RecipeEntry> recipes, GearLevel gear, int rankBelow, int penalty, ForgeLoadout loadout, AdaptivePolicyOptions options, int runs)
        {
            double actions = 0, score = 0;
            var successes = 0;
            var full = 0;
            var total = 0;
            Parallel.ForEach(recipes, recipe =>
            {
                var rank = Math.Max(0, Math.Min(recipe.Level, 50) - rankBelow);
                var stats = loadout.StatsAt(rank);
                var shape = penalty > 0 ? ForgeRules.EnhancementShapePercent(penalty, recipe.Level) : 0;
                for (var run = 0; run < runs; run++)
                {
                    var setup = new ForgeSetup(
                        recipe.Detail.MinigameParameters as ForgeParameters, recipe.Level, rank, gear.Craftsmanship, gear.Control, gear.CpBonus, shape, stats);
                    var random = SimMinigameRandom.ForRun(1, SimStreams.Adaptive, (int)recipe.Type, rank, 0, run);
                    var result = CraftPlayer.Play(new ForgeEngine(setup, random), new AdaptivePolicy(options, run + 1), 100);
                    lock (recipes)
                    {
                        actions += result.Actions;
                        total++;
                        score += result.Score * 100;
                        if (result.Succeeded)
                            successes++;
                        if (result.ReachedMaxQuality)
                            full++;
                    }
                }
            });

            return (actions / total, successes / (double)total, score / total, full / (double)total, total);
        }
    }
}
