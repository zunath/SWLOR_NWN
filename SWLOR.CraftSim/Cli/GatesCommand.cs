using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Play;
using SWLOR.CraftSim.Scenario;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// The <c>gates</c> command: runs the crafting balance gates for one minigame scenario and writes the report to
    /// <c>design/testing/crafting-revamp/&lt;scenario&gt;-gates.md</c> unless <c>--out</c> says otherwise.
    /// </summary>
    public static class GatesCommand
    {
        public const int MaximumRuns = 100_000;
        public const int DefaultRuns = 100;

        private static readonly string[] ValueOptions =
        {
            "--scenario", "--runs", "--seed", "--recipes-per-band", "--gates", "--legacy-seconds-per-action", "--out", "--json", "--repo"
        };

        private static readonly string[] Flags = { "--all-recipes", "--list", "--strict", "--thorough" };

        public static int Run(string[] args, TextWriter output, ScenarioRegistry registry = null) =>
            Run(args, output, registry, null, null);

        /// <summary>The same command with the game data already loaded, so a test does not read every recipe again.</summary>
        internal static int Run(string[] args, TextWriter output, ScenarioRegistry registry, RecipeCatalog recipes, CraftingGearCatalog gear)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions, Flags);
            registry ??= ScenarioRegistry.Discover();

            if (options.ContainsKey("--list"))
            {
                foreach (var listed in registry.Scenarios)
                    output.WriteLine($"{listed.Name}: {listed.Title} ({listed.Skill}, {listed.Minigame})");

                if (registry.Scenarios.Count == 0)
                    output.WriteLine("No scenarios yet: a minigame adds one by implementing ICraftScenario in SWLOR.CraftSim.");

                return 0;
            }

            if (!options.TryGetValue("--scenario", out var names))
                throw new ArgumentException("--scenario needs the name of a scenario; --list shows them.");

            var scenario = registry.Find(names[0]);
            var runs = CommandLineOptions.GetInt(options, "--runs", DefaultRuns, 1, MaximumRuns);
            var seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue);
            if (options.ContainsKey("--all-recipes") && options.ContainsKey("--recipes-per-band"))
                throw new ArgumentException("--all-recipes and --recipes-per-band cannot be given together.");

            var thresholds = GateThresholds.Default with
            {
                RecipesPerBand = options.ContainsKey("--all-recipes")
                    ? null
                    : CommandLineOptions.GetInt(options, "--recipes-per-band", GateThresholds.Default.RecipesPerBand ?? 1, 1, 10_000),
                LegacySecondsPerAction = CommandLineOptions.GetPositiveNumber(
                    options, "--legacy-seconds-per-action", GateThresholds.Default.LegacySecondsPerAction),
                SolverOptions = options.ContainsKey("--thorough") ? FixedRotationSolverOptions.Thorough : null
            };

            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var outPath = options.TryGetValue("--out", out var outValues) ? outValues[0] : DefaultReportPath(root, scenario);

            var outputs = new List<(string, string)> { ("--out", outPath) };
            if (options.TryGetValue("--json", out var jsonValues))
                outputs.Add(("--json", jsonValues[0]));

            var collision = CommandLineOptions.FindOutputCollision(outputs, new List<(string, string)>());
            if (collision != null)
                throw new ArgumentException(collision);

            var suite = new CraftGateSuite(scenario, thresholds);
            var gates = options.TryGetValue("--gates", out var keys)
                ? suite.Select(keys[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                : suite.Gates;

            recipes ??= RecipeCatalog.Load();
            gear ??= CraftingGearCatalog.Build(GearItemReader.ReadAll(RepositoryPaths.ModuleItems(root)), recipes);
            var context = new GateContext(recipes, gear, runs, seed, AttributeSweep.Default);

            var results = new List<GateResult>();
            foreach (var gate in gates)
            {
                output.WriteLine($"Running {gate.Name}...");
                results.Add(gate.Evaluate(context));
            }

            var inputs = suite.Inputs(context).ToList();
            if (gates.Count < suite.Gates.Count)
                inputs.Add($"- Gates run: {string.Join(", ", gates.Select(g => g.Name))}. The others were not run, so this report cannot show that the minigame passes.");

            var command = "SWLOR.CraftSim gates " + string.Join(" ", args);
            var report = GateReport.Build(
                $"Crafting revamp: balance gates of the {scenario.Title} minigame", context, results, command, inputs, suite.Limitations());

            WriteFile(outPath, report);
            output.WriteLine($"Wrote {outPath}");

            if (jsonValues != null)
            {
                var json = new
                {
                    scenario = new { scenario.Name, scenario.Title, Skill = scenario.Skill.ToString(), Minigame = scenario.Minigame.ToString() },
                    seed,
                    runs,
                    command,
                    thresholds = thresholds.Describe().Select(t => new { t.Name, t.Value, t.Meaning }),
                    allGatesPassed = gates.Count == suite.Gates.Count && GateReport.AllPassed(results),
                    results
                };

                WriteFile(jsonValues[0], JsonConvert.SerializeObject(json, Formatting.Indented, new StringEnumConverter()));
                output.WriteLine($"Wrote {jsonValues[0]}");
            }

            foreach (var result in results)
                output.WriteLine($"{result.Gate}: {result.Verdict}. {result.Summary}");

            var blocked = results.Any(result => result.Verdict is GateVerdict.Failed or GateVerdict.NotRun);
            return options.ContainsKey("--strict") && blocked ? 2 : 0;
        }

        /// <summary>Where a scenario's report goes when <c>--out</c> is not given: the crafting revamp's evidence folder.</summary>
        internal static string DefaultReportPath(string root, ICraftScenario scenario) =>
            Path.Combine(root, "design", "testing", "crafting-revamp", $"{scenario.Name}-gates.md");

        private static void WriteFile(string path, string contents)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            OutputFile.WriteAllText(path, contents);
        }
    }
}
