using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Cli;
using SWLOR.CraftSim.Scenario.Kitchen;
using SWLOR.CraftSim.Tests.Support;

namespace SWLOR.CraftSim.Tests.Scenario
{
    /// <summary>Covers the Chef study's report: the tables it writes and the candidates it tries.</summary>
    [TestFixture]
    public class KitchenChefStudyTests
    {
        [Test]
        public void TheStylesOnlyStudy_PlaysEachCandidateAloneAndWithTheLine_AndSaysWhatItInputs()
        {
            var options = new KitchenChefStudyOptions
            {
                Runs = 1,
                RecipesPerBand = 1,
                StylesOnly = true,
                MidGearOnly = true,
                Candidates = new[] { KitchenChefStudy.Candidate(100, 25, 20), KitchenChefStudy.Candidate(80, 25, 15) }
            };

            var report = KitchenChefStudy.Build(SharedData.Recipes, SharedData.Gear, options, "SWLOR.CraftSim chef-perks --styles-only");

            report.Should().StartWith("# Crafting revamp: the Agriculture Chef line");
            report.Should().Contain("## Inputs").And.Contain("## The styles' magnitudes").And.Contain("## Limitations");
            report.Should().NotContain("## Rank 50", "the styles-only study skips the rank 50 and through-the-ranks tables");
            report.Should().Contain("Slow +100% Low / -25% Medium drift, Flash -20% High drift").And.Contain("Slow +80% Low / -25% Medium drift, Flash -15% High drift");
            report.Should().Contain("| alone |").And.Contain("| full line |");
            report.Where(character => character > 127).Should().BeEmpty("the report is ASCII");
        }

        [Test]
        public void ACandidateIsNamedByItsMagnitudes()
        {
            KitchenChefStudy.Candidate(100, 25, 20).Should().Be(new KitchenChefStudy.StyleCandidate(
                "Slow +100% Low / -25% Medium drift, Flash -20% High drift", 100, 25, 20));
        }

        [Test]
        public void TheCommandRefusesACandidateThatIsNotThreeNumbers()
        {
            var output = new StringWriter();

            ((Action)(() => ChefPerksCommand.Run(new[] { "--styles-only", "--candidates", "100/25", "--out", Path.Combine(Path.GetTempPath(), "chef-never.md") }, output)))
                .Should().Throw<ArgumentException>();
            ((Action)(() => ChefPerksCommand.Run(new[] { "--bogus", "1" }, output))).Should().Throw<ArgumentException>();
        }

        [TestCase("a/b/c", "'a' is not a whole number")]
        [TestCase("100/25/99999999999", "'99999999999' is not a whole number")]
        [TestCase(";", "needs at least one")]
        public void TheCommandRefusesACandidateThatIsNotWholeNumbers_WithAnArgumentException(string candidates, string message)
        {
            var output = new StringWriter();

            ((Action)(() => ChefPerksCommand.Run(new[] { "--styles-only", "--candidates", candidates, "--out", Path.Combine(Path.GetTempPath(), "chef-never.md") }, output)))
                .Should().Throw<ArgumentException>().WithMessage($"*{message}*");
        }
    }
}
