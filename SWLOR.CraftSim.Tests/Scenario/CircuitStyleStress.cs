using NUnit.Framework;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Rng;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Scenario.Circuit;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Scenario
{
    /// <summary>
    /// Compares the two styles where at-level quality does not saturate: crafts under the recipe's rank, without gear and with a heavy
    /// enhancement (<c>design/testing/crafting-revamp/engineering-perk-gates.md</c>). Explicit: a long run, never part of a normal test run.
    /// </summary>
    [TestFixture, Explicit]
    public class CircuitStyleStress
    {
        [Test]
        public void Compare()
        {
            var gears = SharedData.Gear.Levels(SkillType.Engineering);
            var lines = new List<string>();
            var loadouts = new (string Name, CircuitLoadout Loadout)[]
            {
                ("OC no lines", new CircuitLoadout(CircuitStyle.Overclock, false, false)),
                ("SM no lines", new CircuitLoadout(CircuitStyle.SafeMode, false, false)),
                ("none", new CircuitLoadout(CircuitStyle.None, false, false)),
                ("OC both", new CircuitLoadout(CircuitStyle.Overclock, true, true)),
                ("SM both", new CircuitLoadout(CircuitStyle.SafeMode, true, true)),
            };

            foreach (var (levelLow, levelHigh) in new[] { (21, 30), (31, 40), (41, 53) })
            {
                var recipes = SharedData.Recipes.ForSkill(SkillType.Engineering)
                    .Where(recipe => recipe.Level >= levelLow && recipe.Level <= levelHigh && recipe.Detail?.MinigameParameters is CircuitParameters)
                    .OrderBy(recipe => recipe.Level).ThenBy(recipe => recipe.Resref).ToList();
                var sample = ScenarioCohort.Sample(recipes, 6);
                foreach (var (condition, rankBelow, penalty, gear) in new[]
                         {
                             ("at level, no gear", 0, 0, gears.First()),
                             ("3 ranks under, no gear", 3, 0, gears.First()),
                             ("at level, no gear, heavy enhancement", 0, 40, gears.First()),
                             ("3 ranks under, mid gear, heavy enhancement", 3, 40, gears[1]),
                         })
                {
                    foreach (var (name, loadout) in loadouts)
                    {
                        var (actions, success, quality, full, n) = Play(sample, gear, rankBelow, penalty, loadout, 16);
                        lines.Add($"band {levelLow}-{levelHigh} | {condition,-42} | {name,-12} | actions {actions:F1} make {success:P0} quality {quality:F1} full {full:P0} (n {n})");
                    }
                }
            }

            var path = Path.Combine(Path.GetTempPath(), "claude-crafting-revamp-perks-engineering", "stress.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, lines);
        }

        private static (double Actions, double Success, double Quality, double Full, int N) Play(
            IReadOnlyList<RecipeEntry> recipes, GearLevel gear, int rankBelow, int penalty, CircuitLoadout loadout, int runs)
        {
            double actions = 0, score = 0;
            var successes = 0;
            var full = 0;
            var total = 0;
            Parallel.ForEach(recipes, recipe =>
            {
                var rank = Math.Max(0, Math.Min(recipe.Level, 50) - rankBelow);
                var stats = loadout.StatsAt(rank);
                for (var run = 0; run < runs; run++)
                {
                    var level = Craft.GetRecipeLevelDetail(recipe.Level);
                    var setup = new CircuitSetup(
                        recipe.Detail.MinigameParameters as CircuitParameters, recipe.Level, level.Progress, level.Quality, level.Durability, rank,
                        gear.Craftsmanship, gear.Control, gear.CpBonus, penalty, stats);
                    var random = SimMinigameRandom.ForRun(1, SimStreams.Adaptive, (int)recipe.Type, rank, 0, run);
                    var engine = new CircuitEngine(random, setup);
                    var result = CraftPlayer.Play(engine, new AdaptivePolicy(AdaptivePolicyOptions.Default, run + 1), 100);
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
