using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario.Experiment;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// The <c>research</c> command: plays the research Experiment and writes its evidence report (length against the 5-8 turn target,
    /// bonus size against today's dice and the change in supply, the time cut, and Fabrication XP per real hour), to
    /// <c>design/testing/crafting-revamp/research-experiment-report.md</c> unless <c>--out</c> says otherwise.
    /// </summary>
    public static class ResearchCommand
    {
        public const int MaximumRuns = 100_000;

        private static readonly string[] ValueOptions =
        {
            "--runs", "--adaptive-runs", "--seed", "--seconds-per-action", "--job-overhead-seconds", "--out", "--repo"
        };

        public static int Run(string[] args, TextWriter output) => Run(args, output, null);

        /// <summary>The same command with the Fabrication gear already built, so a test does not read the module's items.</summary>
        internal static int Run(string[] args, TextWriter output, IReadOnlyList<GearLevel> gear)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions);
            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var outPath = options.TryGetValue("--out", out var outValues)
                ? outValues[0]
                : Path.Combine(root, "design", "testing", "crafting-revamp", "research-experiment-report.md");

            if (gear == null)
            {
                var recipes = RecipeCatalog.Load();
                gear = CraftingGearCatalog.Build(GearItemReader.ReadAll(RepositoryPaths.ModuleItems(root)), recipes).Levels(SkillType.Fabrication);
            }

            var report = ResearchReport.Build(
                new ResearchReportOptions
                {
                    Runs = CommandLineOptions.GetInt(options, "--runs", 400, 1, MaximumRuns),
                    AdaptiveRuns = CommandLineOptions.GetInt(options, "--adaptive-runs", 40, 1, MaximumRuns),
                    Seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue),
                    SecondsPerAction = CommandLineOptions.GetPositiveNumber(options, "--seconds-per-action", 6),
                    JobOverheadSeconds = CommandLineOptions.GetPositiveNumber(options, "--job-overhead-seconds", 30),
                    Gear = gear
                },
                "SWLOR.CraftSim research " + string.Join(" ", args));

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            OutputFile.WriteAllText(outPath, report);
            output.WriteLine($"Wrote {outPath}");
            return 0;
        }
    }
}
