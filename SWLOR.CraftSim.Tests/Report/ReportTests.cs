using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy.Crafting;
using SWLOR.CraftSim.Legacy.Gathering;
using SWLOR.CraftSim.Report;
using SWLOR.CraftSim.Scenario;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.CraftSim.Tests.Report
{
    [TestFixture]
    public class ReportTests
    {
        private static readonly Lazy<GatheringBaselineResults> Gathering =
            new(() => GatheringBaseline.Run(new GatheringBaselineOptions { Trials = 500, Seed = 3 }));

        private static readonly Lazy<CraftingBaselineResults> Crafting = new(() =>
            CraftingBaselineResults.Run(SharedData.Recipes, SharedData.Gear, new LegacyCraftBaselineOptions
            {
                Runs = 10,
                Seed = 3,
                Policies = new ILegacyCraftPolicy[] { new DominantRotationPolicy(), new RolloutPolicy(2) },
                Skills = new[] { SkillType.Smithery, SkillType.Espionage }
            }));

        [Test]
        public void MarkdownTable_WritesAHeaderARuleAndRows()
        {
            var table = new MarkdownTable("A", "B").Add("1", "2").Add("3", "4");

            table.ToString().Should().Be("| A | B |\n| --- | --- |\n| 1 | 2 |\n| 3 | 4 |\n".ReplaceLineEndings());
            table.RowCount.Should().Be(2);
        }

        [Test]
        public void MarkdownTable_RejectsARowOfTheWrongWidth()
        {
            var act = () => new MarkdownTable("A", "B").Add("1");

            act.Should().Throw<ArgumentException>();
        }

        [TestCase(0.9567, 0, "96%")]
        [TestCase(0.9567, 1, "95.7%")]
        [TestCase(0, 0, "0%")]
        public void Percent_UsesTheInvariantCulture(double value, int decimals, string expected)
        {
            MarkdownTable.Percent(value, decimals).Should().Be(expected);
        }

        [Test]
        public void Signed_ShowsAPlusOnlyOnPositiveNumbers()
        {
            (MarkdownTable.Signed(3), MarkdownTable.Signed(0), MarkdownTable.Signed(-2)).Should().Be(("+3", "0", "-2"));
        }

        [Test]
        public void GatheringReport_HasEveryRuleAndTheAttributeSweep()
        {
            var report = GatheringReport.Build(Gathering.Value, AttributeSweep.Default, "SWLOR.CraftSim baseline --section gathering");

            report.Should().Contain("# Crafting revamp: legacy baseline of gathering, refining and fishing");
            report.Should().Contain("## Harvesting").And.Contain("## Scavenging").And.Contain("## Refining").And.Contain("## Fishing").And.Contain("## Limitations");
            report.Should().Contain("-1 to +5");
            report.Should().Contain("| +2 | 50% | 2.50 | 3.75 |");
            report.Should().Contain("WIL +2 (14)");
            report.Should().Contain("`HarvesterItemDefinition.cs:102-109`").And.Contain("`FishingFightLifecycle.cs:52-62`");
            report.Should().Contain("until the Line fight replaced it");
            report.Should().Contain("| 1 (5) | 1050 | 600 |");
        }

        [Test]
        public void CraftingReport_ListsInputsGearAndEachSkillsBands()
        {
            var report = CraftingReport.Build(Crafting.Value, SharedData.Recipes, "SWLOR.CraftSim baseline --section crafting");

            report.Should().Contain("# Crafting revamp: legacy baseline of the old craft minigame");
            report.Should().Contain("Seed 3; 10 crafts per recipe level");
            report.Should().Contain("## Gear levels").And.Contain("## Results by skill").And.Contain("## Rank sensitivity").And.Contain("## Limitations");
            report.Should().Contain("## Summary across the four gear skills");
            report.Should().Contain("### Smithery").And.Contain("### Espionage");
            report.Should().Contain("| Smithery | best gear | 5 | 29 | 30 | 37 | 86 | 105 |");
            report.Should().Contain("Dominant rotation: success").And.Contain("Lookahead: success");
            report.Should().Contain("`CraftViewModel.cs:446-454`");
        }

        [Test]
        public void TheReports_UseOnlyAsciiPunctuation()
        {
            var reports = new[]
            {
                GatheringReport.Build(Gathering.Value, AttributeSweep.Default, "cmd"),
                CraftingReport.Build(Crafting.Value, SharedData.Recipes, "cmd")
            };

            foreach (var report in reports)
                report.Should().MatchRegex("^[\\x09\\x0A\\x0D\\x20-\\x7E]*$", "reports are committed as plain ASCII");
        }
    }
}
