using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario.Experiment;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// The <c>researcher-perks</c> command: plays the Fabrication Researcher line and writes its evidence (the two styles against each
    /// other with the style-balance gate's thresholds, each purchase's effect, Peer Review for an undecided player, and Fabrication XP
    /// per real hour against the crafting ceiling), to <c>design/testing/crafting-revamp/fabrication-researcher-perks.md</c> unless
    /// <c>--out</c> says otherwise. <c>--search</c> instead searches the Methodical and Intuitive numbers for a pair that passes the style
    /// balance both alone and with the whole line, and writes <c>fabrication-researcher-style-search.md</c>. <c>--ablate</c> instead takes each shared purchase out of the whole line (and adds it to the
    /// styles alone) to show which one moves the style balance, and writes <c>fabrication-researcher-line-ablation.md</c>.
    /// </summary>
    public static class ResearcherPerksCommand
    {
        public const int MaximumRuns = 100_000;

        private static readonly string[] ValueOptions =
        {
            "--runs", "--seed", "--seconds-per-action", "--craft-seconds-per-action", "--job-overhead-seconds", "--out", "--repo"
        };

        public static int Run(string[] args, TextWriter output) => Run(args, output, null);

        /// <summary>The same command with the Fabrication gear already built, so a test does not read the module's items.</summary>
        internal static int Run(string[] args, TextWriter output, IReadOnlyList<GearLevel> gear)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions, new[] { "--search", "--ablate" });
            var search = options.ContainsKey("--search");
            var ablate = options.ContainsKey("--ablate");
            if (search && ablate)
                throw new ArgumentException("--search and --ablate are separate reports; give one.");
            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var outPath = options.TryGetValue("--out", out var outValues)
                ? outValues[0]
                : Path.Combine(root, "design", "testing", "crafting-revamp", search ? "fabrication-researcher-style-search.md" : ablate ? "fabrication-researcher-line-ablation.md" : "fabrication-researcher-perks.md");

            if (gear == null)
            {
                var recipes = RecipeCatalog.Load();
                gear = CraftingGearCatalog.Build(GearItemReader.ReadAll(RepositoryPaths.ModuleItems(root)), recipes).Levels(SkillType.Fabrication);
            }

            var studyOptions = new ResearcherPerkStudyOptions
            {
                Runs = CommandLineOptions.GetInt(options, "--runs", 400, 1, MaximumRuns),
                Seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue),
                SecondsPerAction = CommandLineOptions.GetPositiveNumber(options, "--seconds-per-action", 6),
                CraftSecondsPerAction = CommandLineOptions.GetPositiveNumber(options, "--craft-seconds-per-action", 8),
                JobOverheadSeconds = CommandLineOptions.GetPositiveNumber(options, "--job-overhead-seconds", 30),
                Gear = gear
            };
            var command = "SWLOR.CraftSim researcher-perks " + string.Join(" ", args);
            var report = search ? ResearcherStyleSearch.Build(studyOptions, command)
                : ablate ? ResearcherLineAblation.Build(studyOptions, command)
                : ResearcherPerkStudy.Build(studyOptions, command);

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            OutputFile.WriteAllText(outPath, report);
            output.WriteLine($"Wrote {outPath}");
            return 0;
        }
    }
}
