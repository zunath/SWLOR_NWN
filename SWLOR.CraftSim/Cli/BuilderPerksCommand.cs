using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario.Fabricator;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// The <c>builder-perks</c> command: plays the Fabrication Builder line and writes its evidence (each purchase's effect on actions,
    /// CP and quality at rank 50, the two styles compared on quality and actions, and the whole line at every rank), to
    /// <c>design/testing/crafting-revamp/fabrication-builder-perks.md</c> unless <c>--out</c> says otherwise. <c>--styles-only</c> plays
    /// just the styles' magnitudes (the retune table), which is much quicker.
    /// </summary>
    public static class BuilderPerksCommand
    {
        public const int MaximumRuns = 100_000;

        private static readonly string[] ValueOptions = { "--runs", "--seed", "--recipes-per-band", "--out", "--repo" };

        public static int Run(string[] args, TextWriter output)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions, new[] { "--styles-only" });
            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var outPath = options.TryGetValue("--out", out var outValues)
                ? outValues[0]
                : Path.Combine(root, "design", "testing", "crafting-revamp", "fabrication-builder-perks.md");

            var recipes = RecipeCatalog.Load();
            var gear = CraftingGearCatalog.Build(GearItemReader.ReadAll(RepositoryPaths.ModuleItems(root)), recipes);
            var report = FabricatorBuilderStudy.Build(
                recipes,
                gear,
                new FabricatorBuilderStudyOptions
                {
                    Runs = CommandLineOptions.GetInt(options, "--runs", 60, 1, MaximumRuns),
                    Seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue),
                    RecipesPerBand = CommandLineOptions.GetInt(options, "--recipes-per-band", 8, 1, 10_000),
                    StylesOnly = options.ContainsKey("--styles-only")
                },
                "SWLOR.CraftSim builder-perks " + string.Join(" ", args));

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            OutputFile.WriteAllText(outPath, report);
            output.WriteLine($"Wrote {outPath}");
            return 0;
        }
    }
}
