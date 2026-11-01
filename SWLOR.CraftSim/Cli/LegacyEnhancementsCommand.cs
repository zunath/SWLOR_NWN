using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Report;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// The <c>legacy-enhancements</c> command: plays the old minigame with one, two and three enhancements slotted and writes today's
    /// expected transferred enhancements per band, tier and slot count, the yardstick of balance gate 7.
    /// </summary>
    public static class LegacyEnhancementsCommand
    {
        public const int MaximumRuns = 100_000;

        private static readonly string[] ValueOptions = { "--runs", "--seed", "--recipes-per-band", "--skills", "--gear", "--out", "--json", "--repo" };

        public static int Run(string[] args, TextWriter output) => Run(args, output, null, null, null);

        /// <summary>The same command with the game data already loaded, so a test does not read every recipe again.</summary>
        internal static int Run(
            string[] args, TextWriter output, RecipeCatalog recipes, CraftingGearCatalog gear, EnhancementCatalog enhancements)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions);
            var outputs = new[] { "--json", "--out" }
                .Where(options.ContainsKey)
                .Select(option => (option, options[option][0]))
                .ToList();
            var collision = CommandLineOptions.FindOutputCollision(outputs, new List<(string, string)>());
            if (collision != null)
                throw new ArgumentException(collision);

            var runs = CommandLineOptions.GetInt(options, "--runs", 200, 1, MaximumRuns);
            var seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue);
            var recipesPerBand = CommandLineOptions.GetInt(options, "--recipes-per-band", 6, 1, 10_000);
            var skills = ParseSkills(options);
            var everyGear = ParseGear(options);

            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            recipes ??= RecipeCatalog.Load();
            gear ??= CraftingGearCatalog.Build(GearItemReader.ReadAll(RepositoryPaths.ModuleItems(root)), recipes);
            enhancements ??= EnhancementCatalog.Load(root);

            var results = LegacyEnhancementSweep.Run(recipes, gear, enhancements, new LegacyEnhancementOptions
            {
                Runs = runs,
                Seed = seed,
                RecipesPerBand = recipesPerBand,
                Skills = skills,
                SweepEveryGearLevel = everyGear
            });

            var command = "SWLOR.CraftSim legacy-enhancements " + string.Join(" ", args);
            var report = LegacyEnhancementReport.Build(results, recipes, enhancements, command);

            if (options.TryGetValue("--json", out var jsonPath))
            {
                OutputFile.WriteAllText(jsonPath[0], JsonConvert.SerializeObject(Summary(results), Formatting.Indented, new StringEnumConverter()));
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

        /// <summary>The samples without the recipe objects, so the JSON stays readable.</summary>
        private static object Summary(LegacyEnhancementResults results) => new
        {
            seed = results.Options.Seed,
            runs = results.Options.Runs,
            samples = results.Samples.Select(s => new
            {
                skill = s.Skill,
                band = s.Band,
                gateCohort = s.IsGateCohort,
                recipe = s.Recipe.Resref,
                recipeLevel = s.Recipe.Level,
                enhancement = s.Item.Resref,
                enhancementLevel = s.Item.Level,
                progressPenaltyEach = s.Item.ProgressPenalty,
                slots = s.Slots,
                rank = s.Rank,
                gear = s.Gear,
                policy = s.Policy,
                madeShare = s.Cell.SuccessRate,
                meanQualityOfItemsMade = s.Cell.MeanQualityOfSuccesses,
                meanTransferChancePercent = s.Cell.MeanTransferChancePercent,
                expectedTransfersPerAttempt = s.Cell.ExpectedTransfersPerAttempt
            })
        };

        private static bool ParseGear(IReadOnlyDictionary<string, List<string>> options)
        {
            if (!options.TryGetValue("--gear", out var values))
                return true;

            return values[0].ToLowerInvariant() switch
            {
                "all" => true,
                "best" => false,
                _ => throw new ArgumentException("--gear must be all or best.")
            };
        }

        private static IReadOnlyList<SkillType> ParseSkills(IReadOnlyDictionary<string, List<string>> options)
        {
            if (!options.TryGetValue("--skills", out var values))
                return LegacyEnhancementOptions.DefaultSkills;

            var skills = new List<SkillType>();
            foreach (var name in values[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var skill = LegacyEnhancementOptions.DefaultSkills.Cast<SkillType?>()
                    .FirstOrDefault(s => string.Equals(s.ToString(), name, StringComparison.OrdinalIgnoreCase));
                if (skill == null)
                {
                    throw new ArgumentException(
                        $"--skills names a crafting skill with enhancement slots ({string.Join(", ", LegacyEnhancementOptions.DefaultSkills)}), not '{name}'.");
                }

                if (!skills.Contains(skill.Value))
                    skills.Add(skill.Value);
            }

            if (skills.Count == 0)
                throw new ArgumentException("--skills needs at least one skill.");

            return skills;
        }
    }
}
