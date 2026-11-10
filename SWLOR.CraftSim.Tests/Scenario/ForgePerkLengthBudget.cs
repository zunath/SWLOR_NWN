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
    /// How the adaptive player plays each Smithery build: actions, quality, how often it makes the item and how often it uses each perk
    /// action, which explains gates 2 and 4 for the perked scenarios (<c>design/testing/crafting-revamp/smithery-perk-gates.md</c>).
    /// Explicit: it is a long run, never part of a normal test run. The report goes to
    /// <c>%TEMP%/claude-crafting-revamp-perks-smithery-tuning/length-budget.txt</c>.
    /// </summary>
    [TestFixture, Explicit]
    public class ForgePerkLengthBudget
    {
        private static readonly (string Name, ForgeLoadout Loadout)[] Builds =
        {
            ("No perks", null),
            ("Hot Forging alone", new ForgeLoadout(ForgeStyle.HotForging, false, false)),
            ("Cold Working alone", new ForgeLoadout(ForgeStyle.ColdWorking, false, false)),
            ("Forgemaster, no style", new ForgeLoadout(ForgeStyle.None, true, false)),
            ("Forgemaster + Hot Forging", new ForgeLoadout(ForgeStyle.HotForging, true, false)),
            ("Forgemaster + Cold Working", new ForgeLoadout(ForgeStyle.ColdWorking, true, false)),
            ("Artisan, no style", new ForgeLoadout(ForgeStyle.None, false, true)),
            ("Artisan + Cold Working", new ForgeLoadout(ForgeStyle.ColdWorking, false, true)),
            ("Artisan + Hot Forging", new ForgeLoadout(ForgeStyle.HotForging, false, true)),
            ("Both lines + Hot Forging", new ForgeLoadout(ForgeStyle.HotForging, true, true)),
            ("Both lines + Cold Working", new ForgeLoadout(ForgeStyle.ColdWorking, true, true)),
        };

        private sealed record Summary(
            double Actions, double Make, double Quality, double Full, double CpSpent, double IntegrityLeft, IReadOnlyDictionary<string, double> ActionShare, double PerfectTemper, int N);

        [Test]
        public void MeanActionsAtEachGear()
        {
            var lines = new List<string>();
            var gears = SharedData.Gear.Levels(SkillType.Smithery);
            var options = new ForgeScenario().AdaptiveOptions;
            foreach (var gearIndex in new[] { 0, 1, 2 })
            {
                lines.Add($"## gear {gearIndex}");
                lines.Add("| Band | Build | Mean actions | Make the item | Quality | Full quality | CP spent | Integrity left | Perk actions per craft | Perfect Temper quenches |");
                lines.Add("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
                foreach (var (levelLow, levelHigh) in new[] { (1, 10), (11, 20), (21, 30), (31, 40), (41, 53) })
                {
                    var recipes = SharedData.Recipes.ForSkill(SkillType.Smithery)
                        .Where(recipe => recipe.Level >= levelLow && recipe.Level <= levelHigh && recipe.Detail?.MinigameParameters is ForgeParameters)
                        .OrderBy(recipe => recipe.Level).ThenBy(recipe => (int)recipe.Type).ToList();
                    var sample = ScenarioCohort.Sample(recipes, 6);
                    foreach (var (name, loadout) in Builds)
                    {
                        var summary = Play(sample, gears[gearIndex], loadout, options, 16);
                        var perkActions = string.Join(", ", summary.ActionShare.Where(pair => pair.Value > 0).Select(pair => $"{pair.Key} {pair.Value:F1}"));
                        lines.Add($"| {levelLow}-{levelHigh} | {name} | {summary.Actions:F1} | {summary.Make:P0} | {summary.Quality:F1} | {summary.Full:P0} | {summary.CpSpent:F1} | {summary.IntegrityLeft:F0} | {perkActions} | {summary.PerfectTemper:P0} |");
                    }
                }
            }

            var path = Path.Combine(Path.GetTempPath(), "claude-crafting-revamp-perks-smithery-tuning", "length-budget.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllLines(path, lines);
        }

        private static Summary Play(IReadOnlyList<RecipeEntry> recipes, GearLevel gear, ForgeLoadout loadout, AdaptivePolicyOptions options, int runs)
        {
            double actions = 0, score = 0, perfect = 0, cp = 0, integrity = 0;
            var successes = 0;
            var full = 0;
            var total = 0;
            var shares = new Dictionary<string, double>
            {
                [ForgeActionIds.OverdrivePress] = 0, [ForgeActionIds.SurgePlasma] = 0, [ForgeActionIds.RedLinePress] = 0,
                [ForgeActionIds.MicroHone] = 0, [ForgeActionIds.TemperHold] = 0
            };
            Parallel.ForEach(recipes, recipe =>
            {
                var rank = Math.Min(recipe.Level, 50);
                var stats = loadout?.StatsAt(rank);
                for (var run = 0; run < runs; run++)
                {
                    var setup = new ForgeSetup(
                        recipe.Detail.MinigameParameters as ForgeParameters, recipe.Level, rank, gear.Craftsmanship, gear.Control, gear.CpBonus, 0, stats);
                    var random = SimMinigameRandom.ForRun(1, SimStreams.Adaptive, (int)recipe.Type, rank, 0, run);
                    var engine = new ForgeEngine(setup, random);
                    var policy = new AdaptivePolicy(options, run + 1);
                    var used = new Dictionary<string, int>();
                    var perfectQuench = false;
                    var turns = 0;
                    while (engine.Status == MinigameStatus.InProgress && turns < 100)
                    {
                        var choice = policy.Choose(engine);
                        if (choice == null)
                            break;

                        var result = engine.Perform(choice);
                        turns++;
                        used[choice] = used.GetValueOrDefault(choice) + 1;
                        if (result.Performed && result.Outcome?.Label == "Perfect Temper")
                            perfectQuench = true;
                    }

                    var succeeded = engine.Status == MinigameStatus.Succeeded;
                    var quality = succeeded ? engine.Meters.Quality.Fraction * 100 : 0;
                    lock (recipes)
                    {
                        actions += turns;
                        total++;
                        score += quality;
                        cp += engine.Meters.Cp.Max - engine.Meters.Cp.Value;
                        integrity += engine.Meters.Integrity.Value;
                        if (succeeded)
                            successes++;
                        if (succeeded && engine.Meters.Quality.IsFull)
                            full++;
                        if (perfectQuench)
                            perfect++;
                        foreach (var key in shares.Keys.ToList())
                            shares[key] += used.GetValueOrDefault(key);
                    }
                }
            });

            return new Summary(
                actions / total, successes / (double)total, score / total, full / (double)total, cp / total, integrity / total,
                shares.ToDictionary(pair => pair.Key, pair => pair.Value / total), perfect / total, total);
        }
    }
}
