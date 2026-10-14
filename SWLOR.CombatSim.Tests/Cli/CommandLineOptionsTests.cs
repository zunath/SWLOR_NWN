using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Cli;

namespace SWLOR.CombatSim.Tests.Cli
{
    [TestFixture]
    public class CommandLineOptionsTests
    {
        private static readonly string[] RunOptions = { "--rules", "--rules-file", "--runs", "--seed", "--out", "--json" };
        private static readonly string[] RunFlags = { "--detail" };
        private static readonly string[] RunRepeatable = { "--rules", "--rules-file" };

        [TestCase("report.md", "report.md", true)]
        [TestCase("report.md", "./sub/../report.md", true)]
        [TestCase("report.md", "results.json", false)]
        public void AreSameFile_ComparesResolvedPaths(string first, string second, bool same)
        {
            CommandLineOptions.AreSameFile(first, second).Should().Be(same);
        }

        [TestCase("scenario.json", "report.md", "*--out*scenario*")]
        [TestCase("report.md", "rules.json", "*--json*rules file*")]
        [TestCase("report.md", "report.md", "*--json and --out*")]
        public void FindOutputCollision_NamesAnOutputThatWouldOverwriteAnInputOrTheOtherOutput(string outPath, string jsonPath, string expected)
        {
            var outputs = new List<(string, string)> { ("--json", jsonPath), ("--out", outPath) };
            var inputs = new List<(string, string)> { ("scenario", "scenario.json"), ("rules file", "./rules.json") };

            CommandLineOptions.FindOutputCollision(outputs, inputs).Should().Match(expected);
        }

        [Test]
        public void FindOutputCollision_AcceptsOutputsThatAreTheirOwnFiles()
        {
            var outputs = new List<(string, string)> { ("--json", "results.json"), ("--out", "report.md") };
            var inputs = new List<(string, string)> { ("scenario", "scenario.json") };

            CommandLineOptions.FindOutputCollision(outputs, inputs).Should().BeNull();
        }

        [TestCase("0")]
        [TestCase("100001")]
        [TestCase("2147483647")]
        [TestCase("ten")]
        public void GetInt_RejectsValuesOutsideTheRange(string value)
        {
            var options = new Dictionary<string, List<string>> { ["--runs"] = new() { value } };

            var act = () => CommandLineOptions.GetInt(options, "--runs", 200, 1, 100_000);

            act.Should().Throw<ArgumentException>().WithMessage("*--runs*1*100000*");
        }

        [Test]
        public void GetInt_ReturnsTheDefaultWhenNotGiven()
        {
            CommandLineOptions.GetInt(new Dictionary<string, List<string>>(), "--runs", 200, 1, 100_000).Should().Be(200);
        }

        [Test]
        public void Parse_RejectsAnOptionTheCommandDoesNotTake()
        {
            var act = () => Parse("--rule", "tuned");

            act.Should().Throw<ArgumentException>().WithMessage("*Unknown option '--rule'*--rules*");
        }

        [Test]
        public void Parse_RejectsAValueAfterAFlag()
        {
            var act = () => Parse("--detail", "yes");

            act.Should().Throw<ArgumentException>().WithMessage("*Unexpected argument 'yes'*");
        }

        [Test]
        public void Parse_RejectsASingleValueOptionGivenTwice()
        {
            var act = () => Parse("--runs", "100", "--runs", "200");

            act.Should().Throw<ArgumentException>().WithMessage("*--runs can only be given once*");
        }

        [Test]
        public void Parse_RejectsAnOptionWithoutItsValue()
        {
            var act = () => Parse("--out", "--detail");

            act.Should().Throw<ArgumentException>().WithMessage("*--out needs a value*");
        }

        [Test]
        public void Parse_CollectsRepeatedOptionsFlagsAndValuesRegardlessOfCase()
        {
            var options = Parse("--rules-file", "a.json", "--RULES-FILE", "b.json", "--detail", "--runs", "50");

            options["--rules-file"].Should().Equal("a.json", "b.json");
            options.Should().ContainKey("--detail");
            options["--runs"].Should().Equal("50");
        }

        private static Dictionary<string, List<string>> Parse(params string[] args) =>
            CommandLineOptions.Parse(args, RunOptions, RunFlags, RunRepeatable);
    }
}
