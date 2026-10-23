using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Gates;
using SWLOR.CraftSim.Harvesting;
using SWLOR.CraftSim.Scenario;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// The <c>harvesting</c> command: plays Quick Harvest and Prospecting sessions against today's harvests and writes the parity report to
    /// <c>design/testing/crafting-revamp/harvesting-prospecting-parity.md</c> unless <c>--out</c> says otherwise.
    /// </summary>
    public static class HarvestingCommand
    {
        public const int MaximumRuns = 200_000;
        public const int DefaultRuns = 4000;

        private static readonly string[] ValueOptions = { "--runs", "--seed", "--out", "--json", "--repo" };
        private static readonly string[] Flags = { "--strict" };

        public static int Run(string[] args, TextWriter output) => Run(args, output, HarvestingParityOptions.Default);

        /// <summary>The same command with the cases to play chosen, so a test does not play every rank.</summary>
        internal static int Run(string[] args, TextWriter output, HarvestingParityOptions defaults)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions, Flags);
            var runs = CommandLineOptions.GetInt(options, "--runs", DefaultRuns, 1, MaximumRuns);
            var seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue);
            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var outPath = options.TryGetValue("--out", out var outValues)
                ? outValues[0]
                : Path.Combine(root, "design", "testing", "crafting-revamp", "harvesting-prospecting-parity.md");

            var outputs = new List<(string, string)> { ("--out", outPath) };
            if (options.TryGetValue("--json", out var jsonValues))
                outputs.Add(("--json", jsonValues[0]));

            var collision = CommandLineOptions.FindOutputCollision(outputs, new List<(string, string)>());
            if (collision != null)
                throw new ArgumentException(collision);

            var nodes = HarvestNodeCatalog.Read(Path.Combine(root, "Module", "utp"));
            var thresholds = GateThresholds.Default;
            var parity = new HarvestingParity(runs, seed, nodes, defaults);
            var context = new GateContext(null, null, runs, seed, AttributeSweep.Default);

            output.WriteLine("Playing the nodes...");
            var results = HarvestingReport.Run(context, parity, thresholds);
            var command = "SWLOR.CraftSim harvesting " + string.Join(" ", args);
            var report = HarvestingReport.Build(context, parity, thresholds, results, command);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            OutputFile.WriteAllText(outPath, report);
            output.WriteLine($"Wrote {outPath}");

            if (jsonValues != null)
            {
                var json = new { seed, runs, command, results };
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
