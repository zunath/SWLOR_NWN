using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SWLOR.CraftSim.Cli;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;

namespace SWLOR.CraftSim.Tests.Cli
{
    [TestFixture]
    public class GatesCommandTests
    {
        private string _directory;

        [SetUp]
        public void CreateFolder() => _directory = Directory.CreateTempSubdirectory("craftsim-gates-").FullName;

        [TearDown]
        public void DeleteFolder() => Directory.Delete(_directory, true);

        private static ScenarioRegistry Registry() => new(new ICraftScenario[]
        {
            new BenchScenario(),
            new BenchScenario { Name = "dominant", Dominant = true }
        });

        private int Run(IEnumerable<string> args, out string output, ScenarioRegistry registry = null)
        {
            var writer = new StringWriter();
            var code = GatesCommand.Run(
                args.ToArray(), writer, registry ?? Registry(), BenchRecipes.Catalog(new[] { 15, 45 }), SharedData.Gear);
            output = writer.ToString();
            return code;
        }

        private string Path(string name) => System.IO.Path.Combine(_directory, name);

        private string[] Common(params string[] more) =>
            new[] { "--scenario", "bench", "--runs", "12", "--recipes-per-band", "1", "--out", Path("report.md") }.Concat(more).ToArray();

        [Test]
        public void WritesTheMarkdownReportAndTheJson()
        {
            var code = Run(Common("--seed", "3", "--gates", "length,xp", "--json", Path("results.json")), out var output);

            code.Should().Be(0);
            output.Should().Contain("Running Length...").And.Contain("Running XP rate...").And.Contain("Wrote").And.Contain("Length: ");
            var report = File.ReadAllText(Path("report.md"));
            report.Should().Contain("# Crafting revamp: balance gates of the Test Bench minigame");
            report.Should().Contain("Seed 3; 12 runs per case").And.Contain("## Inputs").And.Contain("## Verdicts").And.Contain("## Limitations");
            report.Should().Contain("## Length").And.Contain("## XP rate").And.NotContain("## No fixed rotation");
            report.Should().Contain("- Gates run: Length, XP rate.");

            var json = JObject.Parse(File.ReadAllText(Path("results.json")));
            json["scenario"]["Name"].Value<string>().Should().Be("bench");
            json["scenario"]["Skill"].Value<string>().Should().Be("Smithery");
            json["seed"].Value<int>().Should().Be(3);
            json["runs"].Value<int>().Should().Be(12);
            json["allGatesPassed"].Value<bool>().Should().BeFalse("only two of the six gates were run");
            json["results"].Select(r => r["Gate"].Value<string>()).Should().Equal("Length", "XP rate");
            json["results"].Select(r => r["Verdict"].Value<string>()).Should().OnlyContain(v => v == "Passed" || v == "Failed");
            json["thresholds"].Select(t => t["Name"].Value<string>()).Should().Contain("RotationMarginPoints");
        }

        [Test]
        public void RunsEveryGateWhenNoneAreNamed()
        {
            var code = Run(new[] { "--scenario", "BENCH", "--runs", "10", "--recipes-per-band", "1", "--out", Path("all.md") }, out var output);

            code.Should().Be(0);
            var report = File.ReadAllText(Path("all.md"));
            foreach (var gate in new[] { "No fixed rotation", "Style balance", "Perks optional", "Length", "Economy parity", "XP rate" })
                report.Should().Contain($"| {gate} |");

            report.Should().NotContain("Gates run:");
            output.Should().Contain("Style balance: NotApplicable");
        }

        [Test]
        public void ASeedRepeatsTheReportExactlyAndAnotherSeedChangesIt()
        {
            string Report(string seed)
            {
                Run(Common("--seed", seed, "--gates", "length"), out _);
                return File.ReadAllText(Path("report.md"));
            }

            var first = Report("2");

            Report("2").Should().Be(first);
            Report("3").Should().NotBe(first);
        }

        [Test]
        public void WithoutStrictAFailedGateIsAResultAndWithItTheExitCode()
        {
            var args = new[] { "--scenario", "dominant", "--runs", "10", "--recipes-per-band", "1", "--gates", "rotation", "--strict", "--out", Path("s.md") };

            Run(args, out var output).Should().Be(2);
            output.Should().Contain("No fixed rotation: Failed");
            Run(args.Where(arg => arg != "--strict"), out _).Should().Be(0, "a failing gate is a result, not an error");

            var passing = new[] { "--scenario", "bench", "--runs", "10", "--recipes-per-band", "1", "--gates", "length,styles", "--strict", "--out", Path("p.md") };
            Run(passing, out _).Should().Be(0, "a passing gate and one that does not apply do not block");
        }

        [Test]
        public void TheLegacySecondsPerActionAreAnOption()
        {
            Run(Common("--gates", "xp", "--legacy-seconds-per-action", "12"), out _);

            File.ReadAllText(Path("report.md")).Should().Contain("12.0 s per action legacy").And.Contain("12.0 s in the old one");
        }

        [Test]
        public void ThoroughGivesTheSolverTheLongerSearch()
        {
            Run(Common("--gates", "length", "--thorough"), out _);
            var thorough = File.ReadAllText(Path("report.md"));
            Run(Common("--gates", "length"), out _);
            var standard = File.ReadAllText(Path("report.md"));

            thorough.Should().Contain("beam 12").And.Contain("local search from the best 5 candidate(s) for up to 4 round(s) each");
            standard.Should().Contain("beam 3", "the test bench's own small solver");
        }

        [Test]
        public void ListShowsEachScenarioAndSaysWhenThereAreNone()
        {
            Run(new[] { "--list" }, out var listed).Should().Be(0);
            Run(new[] { "--list" }, out var none, new ScenarioRegistry(Array.Empty<ICraftScenario>())).Should().Be(0);

            listed.Should().Contain("bench: Test Bench (Smithery, Forge)").And.Contain("dominant: Test Bench");
            none.Should().Contain("No scenarios yet");
            Directory.GetFiles(_directory).Should().BeEmpty("listing writes nothing");
        }

        [TestCase("--scenario", "forge", "No scenario named 'forge'")]
        [TestCase("--runs", "0", "--runs needs a whole number")]
        [TestCase("--runs", "x", "--runs needs a whole number")]
        [TestCase("--seed", "x", "--seed needs a whole number")]
        [TestCase("--recipes-per-band", "0", "--recipes-per-band needs a whole number")]
        [TestCase("--gates", "rotation,balance", "--gates names")]
        [TestCase("--legacy-seconds-per-action", "0", "--legacy-seconds-per-action needs a number above zero")]
        [TestCase("--legacy-seconds-per-action", "fast", "--legacy-seconds-per-action needs a number above zero")]
        [TestCase("--bogus", "1", "Unknown option '--bogus'")]
        public void BadOptionsAreRejectedBeforeAnythingIsPlayed(string option, string value, string message)
        {
            var args = new List<string> { "--scenario", "bench", "--out", Path("x.md") };
            var index = args.IndexOf(option);
            if (index >= 0)
                args[index + 1] = value;
            else
                args.AddRange(new[] { option, value });

            var act = () => Run(args, out _);

            act.Should().Throw<ArgumentException>().WithMessage($"*{message}*");
            File.Exists(Path("x.md")).Should().BeFalse();
        }

        [Test]
        public void AScenarioNameIsRequired()
        {
            var act = () => Run(new[] { "--runs", "5" }, out _);

            act.Should().Throw<ArgumentException>().WithMessage("*--scenario needs the name of a scenario*");
        }

        [Test]
        public void AllRecipesAndARecipeLimitCannotBeCombined()
        {
            var act = () => Run(Common("--all-recipes"), out _);

            act.Should().Throw<ArgumentException>().WithMessage("*cannot be given together*");
        }

        [Test]
        public void TheReportAndTheJsonCannotBeTheSameFile()
        {
            var act = () => Run(Common("--json", Path("report.md")), out _);

            act.Should().Throw<ArgumentException>().WithMessage("*name the same file*");
        }

        [Test]
        public void TheDefaultReportGoesToTheCraftingRevampEvidenceFolder()
        {
            var path = GatesCommand.DefaultReportPath(System.IO.Path.Combine("repo", "root"), new BenchScenario { Name = "forge" });

            path.Should().Be(System.IO.Path.Combine("repo", "root", "design", "testing", "crafting-revamp", "forge-gates.md"));
        }

        [Test]
        public void AReportFolderThatDoesNotExistYetIsCreated()
        {
            var nested = Path(System.IO.Path.Combine("a", "b", "report.md"));

            Run(new[] { "--scenario", "bench", "--runs", "6", "--recipes-per-band", "1", "--gates", "length", "--out", nested }, out _).Should().Be(0);

            File.Exists(nested).Should().BeTrue();
        }
    }
}
