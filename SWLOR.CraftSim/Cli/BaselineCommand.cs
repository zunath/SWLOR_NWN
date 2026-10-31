using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Report;
using SWLOR.CraftSim.Scenario;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>The <c>baseline</c> command: plays today's rules and writes the legacy baseline report.</summary>
    public static class BaselineCommand
    {
        public const int MaximumRuns = 100_000;
        public const int MaximumGatheringRuns = 10_000_000;

        private static readonly string[] ValueOptions = { "--section", "--runs", "--seed", "--gathering-runs", "--skills", "--out", "--json", "--repo" };

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

            var section = options.TryGetValue("--section", out var sections) ? sections[0].ToLowerInvariant() : "all";
            if (section is not ("crafting" or "gathering" or "all"))
                throw new ArgumentException("--section must be crafting, gathering or all.");

            var runs = CommandLineOptions.GetInt(options, "--runs", 200, 1, MaximumRuns);
            var seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue);
            var gatheringRuns = CommandLineOptions.GetInt(options, "--gathering-runs", 20_000, 1, MaximumGatheringRuns);
            var skills = ParseSkills(options);
            var command = "SWLOR.CraftSim baseline " + string.Join(" ", args);

            var reports = new List<string>();
            var json = new Dictionary<string, object>();

            if (section is "crafting" or "all")
            {
                var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
                var recipes = RecipeCatalog.Load();
                var gear = CraftingGearCatalog.Build(GearItemReader.ReadAll(RepositoryPaths.ModuleItems(root)), recipes);
                var results = CraftingBaselineResults.Run(recipes, gear, new LegacyCraftBaselineOptions
                {
                    Runs = runs,
                    Seed = seed,
                    Policies = new ILegacyCraftPolicy[] { new DominantRotationPolicy(), new RolloutPolicy() },
                    Skills = skills
                });
                reports.Add(CraftingReport.Build(results, recipes, command));
                json["crafting"] = results;
            }

            if (section is "gathering" or "all")
            {
                var results = GatheringBaseline.Run(new GatheringBaselineOptions { Trials = gatheringRuns, Seed = seed });
                reports.Add(GatheringReport.Build(results, AttributeSweep.Default, command));
                json["gathering"] = results;
            }

            var report = string.Join(Environment.NewLine + "---" + Environment.NewLine + Environment.NewLine, reports);
            if (options.TryGetValue("--json", out var jsonPath))
            {
                OutputFile.WriteAllText(jsonPath[0], JsonConvert.SerializeObject(json, Formatting.Indented, new StringEnumConverter()));
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

        private static IReadOnlyList<SkillType> ParseSkills(IReadOnlyDictionary<string, List<string>> options)
        {
            if (!options.TryGetValue("--skills", out var values))
                return LegacyCraftBaselineOptions.DefaultSkills;

            var skills = new List<SkillType>();
            foreach (var name in values[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var skill = LegacyCraftBaselineOptions.DefaultSkills.Cast<SkillType?>()
                    .FirstOrDefault(s => string.Equals(s.ToString(), name, StringComparison.OrdinalIgnoreCase));
                if (skill == null)
                {
                    throw new ArgumentException(
                        $"--skills names a crafting skill ({string.Join(", ", LegacyCraftBaselineOptions.DefaultSkills)}), not '{name}'.");
                }

                if (!skills.Contains(skill.Value))
                    skills.Add(skill.Value);
            }

            return skills;
        }
    }
}
