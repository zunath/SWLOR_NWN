using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Cli;

namespace SWLOR.CraftSim.Tests.Cli
{
    [TestFixture]
    public class CommandLineOptionsTests
    {
        private static readonly string[] RunOptions = { "--section", "--runs", "--seed", "--out", "--json" };
        private static readonly string[] RunFlags = { "--verbose" };
        private static readonly string[] RunRepeatable = { "--section" };

        [TestCase("report.md", "report.md", true)]
        [TestCase("report.md", "./sub/../report.md", true)]
        [TestCase("report.md", "results.json", false)]
        public void AreSameFile_ComparesResolvedPaths(string first, string second, bool same)
        {
            CommandLineOptions.AreSameFile(first, second).Should().Be(same);
        }

        [Test]
        public void FindOutputCollision_NamesTwoOutputsThatWouldOverwriteEachOther()
        {
            var outputs = new List<(string, string)> { ("--json", "report.md"), ("--out", "./report.md") };

            CommandLineOptions.FindOutputCollision(outputs, new List<(string, string)>()).Should().Match("*--json and --out*");
        }

        [Test]
        public void FindOutputCollision_AcceptsOutputsThatAreTheirOwnFiles()
        {
            var outputs = new List<(string, string)> { ("--json", "results.json"), ("--out", "report.md") };

            CommandLineOptions.FindOutputCollision(outputs, new List<(string, string)>()).Should().BeNull();
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
            var act = () => Parse("--run", "5");

            act.Should().Throw<ArgumentException>().WithMessage("*Unknown option '--run'*--runs*");
        }

        [Test]
        public void Parse_RejectsAValueAfterAFlag()
        {
            var act = () => Parse("--verbose", "yes");

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
            var act = () => Parse("--out", "--verbose");

            act.Should().Throw<ArgumentException>().WithMessage("*--out needs a value*");
        }

        [Test]
        public void Parse_CollectsRepeatedOptionsFlagsAndValuesRegardlessOfCase()
        {
            var options = Parse("--section", "crafting", "--SECTION", "gathering", "--verbose", "--runs", "50");

            options["--section"].Should().Equal("crafting", "gathering");
            options.Should().ContainKey("--verbose");
            options["--runs"].Should().Equal("50");
        }

        private static Dictionary<string, List<string>> Parse(params string[] args) =>
            CommandLineOptions.Parse(args, RunOptions, RunFlags, RunRepeatable);
    }
}
