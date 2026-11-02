using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Scenario.Kitchen;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// The <c>chef-perks</c> command: plays the Agriculture Chef line and writes its evidence (each purchase's effect on actions, CP and
    /// quality at rank 50, the two styles compared on quality and actions, the whole line at every rank, and the styles' magnitudes), to
    /// <c>design/testing/crafting-revamp/agriculture-chef-perks.md</c> unless <c>--out</c> says otherwise. <c>--styles-only</c> plays just the
    /// styles' magnitudes (the retune table), which is much quicker; <c>--candidates</c> takes the magnitudes to try as
    /// <c>slowLow/slowMedium/flashHigh</c> groups separated by semicolons, such as <c>40/25/35;100/25/20</c>.
    /// </summary>
    public static class ChefPerksCommand
    {
        public const int MaximumRuns = 100_000;

        private static readonly string[] ValueOptions = { "--runs", "--seed", "--recipes-per-band", "--candidates", "--out", "--repo" };

        public static int Run(string[] args, TextWriter output)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions, new[] { "--styles-only", "--mid-gear-only" });
            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var outPath = options.TryGetValue("--out", out var outValues)
                ? outValues[0]
                : Path.Combine(root, "design", "testing", "crafting-revamp", "agriculture-chef-perks.md");

            IReadOnlyList<KitchenChefStudy.StyleCandidate> candidates = null;
            if (options.TryGetValue("--candidates", out var candidateValues))
            {
                candidates = candidateValues[0]
                    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(group => group.Split('/').Select(ParseNumber).ToArray())
                    .Select(numbers => numbers.Length == 3
                        ? KitchenChefStudy.Candidate(numbers[0], numbers[1], numbers[2])
                        : throw new ArgumentException("A candidate is slowLow/slowMedium/flashHigh."))
                    .ToList();
                if (candidates.Count == 0)
                    throw new ArgumentException("--candidates needs at least one slowLow/slowMedium/flashHigh group.");
            }

            var recipes = RecipeCatalog.Load();
            var gear = CraftingGearCatalog.Build(GearItemReader.ReadAll(RepositoryPaths.ModuleItems(root)), recipes);
            var report = KitchenChefStudy.Build(
                recipes,
                gear,
                new KitchenChefStudyOptions
                {
                    Runs = CommandLineOptions.GetInt(options, "--runs", 40, 1, MaximumRuns),
                    Seed = CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue),
                    RecipesPerBand = CommandLineOptions.GetInt(options, "--recipes-per-band", 6, 1, 10_000),
                    StylesOnly = options.ContainsKey("--styles-only"),
                    MidGearOnly = options.ContainsKey("--mid-gear-only"),
                    Candidates = candidates
                },
                "SWLOR.CraftSim chef-perks " + string.Join(" ", args));

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            OutputFile.WriteAllText(outPath, report);
            output.WriteLine($"Wrote {outPath}");
            return 0;
        }

        private static int ParseNumber(string text) =>
            int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new ArgumentException($"'{text}' is not a whole number in --candidates.");
    }
}
