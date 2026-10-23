using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Search;
using SWLOR.Game.Server.Service.CraftService.Minigame.Gathering;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// The <c>search</c> command: plays the Search board against today's scavenging and writes the parity report to
    /// <c>design/testing/crafting-revamp/scavenging-search-parity.md</c> unless <c>--out</c> says otherwise.
    /// </summary>
    public static class SearchCommand
    {
        public const int MaximumRuns = 200_000;
        public const int DefaultRuns = 4000;

        private static readonly string[] ValueOptions = { "--runs", "--seed", "--seconds-per-action", "--site-seconds", "--out", "--json", "--repo", "--loadout" };
        private static readonly string[] Flags = { "--strict" };

        public static int Run(string[] args, TextWriter output) => Run(args, output, SearchParityOptions.Default);

        /// <summary>The same command with the cases to play chosen, so a test does not play every rank.</summary>
        internal static int Run(string[] args, TextWriter output, SearchParityOptions defaults)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions, Flags);
            var runs = CommandLineOptions.GetInt(options, "--runs", DefaultRuns, 1, MaximumRuns);
            var seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue);
            var secondsPerAction = CommandLineOptions.GetPositiveNumber(options, "--seconds-per-action", defaults.SecondsPerAction);
            var siteSeconds = CommandLineOptions.GetPositiveNumber(options, "--site-seconds", defaults.SiteSeconds);
            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var loadout = GatheringLoadout.None;
            if (options.TryGetValue("--loadout", out var loadoutValues))
            {
                loadout = GatheringLoadout.Find(loadoutValues[0]) ??
                          throw new ArgumentException($"--loadout names no loadout. Choose one of: {string.Join(", ", GatheringLoadout.All.Select(entry => entry.Slug))}.");
                defaults = defaults with { Loadout = loadout };
            }

            var outPath = options.TryGetValue("--out", out var outValues)
                ? outValues[0]
                : Path.Combine(root, "design", "testing", "crafting-revamp", loadout == GatheringLoadout.None ? "scavenging-search-parity.md" : $"scavenging-{loadout.Slug}.md");

            var outputs = new List<(string, string)> { ("--out", outPath) };
            if (options.TryGetValue("--json", out var jsonValues))
                outputs.Add(("--json", jsonValues[0]));

            var collision = CommandLineOptions.FindOutputCollision(outputs, new List<(string, string)>());
            if (collision != null)
                throw new ArgumentException(collision);

            var parityOptions = defaults with { SecondsPerAction = secondsPerAction, SiteSeconds = siteSeconds };
            var thresholds = GateThresholds.Default;
            var parity = new SearchParity(runs, seed);
            var context = new GateContext(null, null, runs, seed, AttributeSweep.Default);

            output.WriteLine("Playing the boards...");
            var results = SearchReport.Run(context, parity, parityOptions, thresholds);
            var command = "SWLOR.CraftSim search " + string.Join(" ", args);
            var report = SearchReport.Build(context, parity, parityOptions, thresholds, results, command);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            OutputFile.WriteAllText(outPath, report);
            output.WriteLine($"Wrote {outPath}");

            if (jsonValues != null)
            {
                var json = new { seed, runs, command, secondsPerAction, siteSeconds, results };
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(jsonValues[0]))!);
                OutputFile.WriteAllText(jsonValues[0], JsonConvert.SerializeObject(json, Formatting.Indented, new StringEnumConverter()));
                output.WriteLine($"Wrote {jsonValues[0]}");
            }

            foreach (var result in results)
                output.WriteLine($"{result.Gate}: {result.Verdict}. {result.Summary}");

            var blocked = results.Any(result => result.Verdict is GateVerdict.Failed or GateVerdict.NotRun);
            return options.ContainsKey("--strict") && blocked ? 2 : 0;
        }
    }
}
