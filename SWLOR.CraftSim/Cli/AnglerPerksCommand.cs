using SWLOR.CraftSim.Data;
using SWLOR.CraftSim.Fishing;

namespace SWLOR.CraftSim.Cli
{
    /// <summary>
    /// The <c>angler-perks</c> command: plays the Agriculture Angler line and writes its evidence (each purchase alone, the whole line at
    /// every rank against Quick Reel and the unperked fight, the two styles, and the special rare fish), to
    /// <c>design/testing/crafting-revamp/agriculture-angler-perks.md</c> unless <c>--out</c> says otherwise.
    /// </summary>
    public static class AnglerPerksCommand
    {
        public const int MaximumRuns = 100_000;

        private static readonly string[] ValueOptions = { "--runs", "--seed", "--out", "--repo" };

        public static int Run(string[] args, TextWriter output)
        {
            var options = CommandLineOptions.Parse(args, ValueOptions, Array.Empty<string>());
            var root = options.TryGetValue("--repo", out var repo) ? RepositoryPaths.FindRoot(repo[0]) : RepositoryPaths.FindRoot();
            var outPath = options.TryGetValue("--out", out var outValues)
                ? outValues[0]
                : Path.Combine(root, "design", "testing", "crafting-revamp", "agriculture-angler-perks.md");

            var parity = new FishingParity(
                CommandLineOptions.GetInt(options, "--runs", 400, 1, MaximumRuns),
                CommandLineOptions.GetInt(options, "--seed", 1, int.MinValue));
            output.WriteLine("Playing the fights...");
            var report = AnglerPerkStudy.Build(parity, FishingParityOptions.Default, "SWLOR.CraftSim angler-perks " + string.Join(" ", args));

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
            OutputFile.WriteAllText(outPath, report);
            output.WriteLine($"Wrote {outPath}");
            return 0;
        }
    }
}
