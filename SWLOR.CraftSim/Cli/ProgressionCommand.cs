using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Progression;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>The <c>progression</c> command: models a new crafter's hours to rank 50 and writes the progression-pace report.</summary>
    public static class ProgressionCommand
    {
        public const int MaximumRuns = 100_000;
        public const string DefaultReport = "design/testing/crafting-revamp/progression-pace-baseline.md";

        private static readonly string[] ValueOptions =
        {
            "--runs", "--seed", "--skills", "--out", "--json", "--repo", "--node-seconds", "--overhead-seconds", "--might"
        };

        public static int Run(string[] args, TextWriter output)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions);
            var outputs = new[] { "--json", "--out" }
                .Where(options.ContainsKey)
                .Select(option => (option, options[option][0]))
                .ToList();
            var collision = CommandLineOptions.FindOutputCollision(outputs, new List<(string, string)>());
            if (collision != null)
                throw new ArgumentException(collision);

            var runs = CommandLineOptions.GetInt(options, "--runs", 500, 1, MaximumRuns);
            var seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue);
            var assumptions = new ProgressionAssumptions
            {
                NodeTravelSeconds = CommandLineOptions.GetPositiveNumber(options, "--node-seconds", new ProgressionAssumptions().NodeTravelSeconds),
                OverheadSecondsPerCraft = CommandLineOptions.GetPositiveNumber(options, "--overhead-seconds", new ProgressionAssumptions().OverheadSecondsPerCraft),
                MightModifier = CommandLineOptions.GetInt(options, "--might", 0, -1, 5)
            };

            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var recipes = RecipeCatalog.Load();
            var gear = CraftingGearCatalog.Build(GearItemReader.ReadAll(RepositoryPaths.ModuleItems(root)), recipes);
            var guilds = GuildTaskCatalog.Build(recipes, root);
            var model = ProgressionModel.Run(
                recipes, gear, guilds, new ProgressionOptions { Runs = runs, Seed = seed, Skills = ParseSkills(options) });

            var command = "SWLOR.CraftSim progression " + string.Join(" ", args);
            var report = ProgressionReport.Build(model, assumptions, command);

            if (options.TryGetValue("--json", out var jsonPath))
            {
                OutputFile.WriteAllText(jsonPath[0], JsonConvert.SerializeObject(Results(model, assumptions), Formatting.Indented, new StringEnumConverter()));
                output.WriteLine($"Wrote {jsonPath[0]}");
            }

            if (options.TryGetValue("--out", out var outPath))
            {
                OutputFile.WriteAllText(outPath[0], report);
                output.WriteLine($"Wrote {outPath[0]}");
            }
            else
            {
                output.WriteLine(report);
            }

            return 0;
        }

        private static object Results(ProgressionModel model, ProgressionAssumptions assumptions) => new
        {
            model.Options.Seed,
            model.Options.Runs,
            Assumptions = assumptions,
            Crafting = model.Skills.SelectMany(skill => model.GearNames(skill).Select(gear => model.Path(skill, gear, assumptions))).ToList(),
            Gathering = new[] { GatheringStrategy.HarvestAndRefine, GatheringStrategy.Scavenge, GatheringStrategy.Best }
                .Select(strategy => model.Gathering(strategy, assumptions)).ToList(),
            Research = model.Skills.Contains(SkillType.Fabrication)
                ? ResearchCohort.Rows(model, CraftingGearCatalog.MidGearName, assumptions)
                : new List<ResearchRow>(),
            CatchTasks = GuildCohorts.CatchTasks(model.Guilds, assumptions)
        };

        private static IReadOnlyList<SkillType> ParseSkills(IReadOnlyDictionary<string, List<string>> options)
        {
            if (!options.TryGetValue("--skills", out var values))
                return CraftingGearCatalog.GearSkills;

            var skills = new List<SkillType>();
            foreach (var name in values[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var skill = CraftingGearCatalog.GearSkills.Cast<SkillType?>()
                    .FirstOrDefault(s => string.Equals(s.ToString(), name, StringComparison.OrdinalIgnoreCase));
                if (skill == null)
                {
                    throw new ArgumentException(
                        $"--skills names a crafting skill ({string.Join(", ", CraftingGearCatalog.GearSkills)}), not '{name}'.");
                }

                if (!skills.Contains(skill.Value))
                    skills.Add(skill.Value);
            }

            return skills;
        }
    }
}
