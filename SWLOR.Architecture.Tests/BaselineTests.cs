using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Domains;

namespace SWLOR.Architecture.Tests;

/// <summary>The baseline file format and the comparison that decides whether the guard fails.</summary>
[TestFixture]
public class BaselineTests
{
    private static UnitEdge Edge(string from, string to, string fromDomain, string toDomain, params string[] members)
    {
        DependencyUnit.TryParse(from, out var fromUnit).Should().BeTrue();
        DependencyUnit.TryParse(to, out var toUnit).Should().BeTrue();
        return new UnitEdge(fromUnit, toUnit, fromDomain, toDomain, members.ToHashSet());
    }

    [Test]
    public void A_rendered_baseline_reads_back_to_the_same_edges_in_a_stable_order()
    {
        var edges = new[]
        {
            Edge("Quest", "Stat", "Quests", "Character", "Quest::A"),
            Edge("Combat", "Stat", "Combat", "Character", "Combat::A", "Combat::B"),
            Edge("Combat", "Skill", "Combat", "Character", "Combat::C"),
            Edge("Combat", "Quest", "Combat", "Quests", "Combat::D")
        };

        var text = EdgeBaselineFile.Render(edges, ["Header line"]);

        text.Should().Be(
            "# Header line\n" +
            "\n# Combat -> Character (2 edges)\nCombat -> Skill | 1\nCombat -> Stat | 2\n" +
            "\n# Combat -> Quests (1 edge)\nCombat -> Quest | 1\n" +
            "\n# Quests -> Character (1 edge)\nQuest -> Stat | 1\n");
        EdgeBaselineFile.ParseKeys(text).Should().BeEquivalentTo("Combat -> Skill", "Combat -> Stat", "Combat -> Quest", "Quest -> Stat");
        EdgeBaselineFile.Render(edges.Reverse(), ["Header line"]).Should().Be(text, "the order must not depend on discovery order");
    }

    [Test]
    public void Parsing_accepts_windows_line_endings_comments_and_blank_lines()
    {
        var keys = EdgeBaselineFile.ParseKeys("# comment\r\n\r\nCombat -> Stat | 3\r\n  \r\nFeature/PerkDefinition -> Combat | 12\r\n");

        keys.Should().BeEquivalentTo("Combat -> Stat", "Feature/PerkDefinition -> Combat");
    }

    [TestCase("Combat -> Stat")]
    [TestCase("Combat Stat | 3")]
    [TestCase("Combat -> Stat | many")]
    public void Parsing_rejects_a_malformed_line(string line)
    {
        var parse = () => EdgeBaselineFile.ParseKeys(line);

        parse.Should().Throw<FormatException>().WithMessage("*line 1*");
    }

    [Test]
    public void Parsing_rejects_an_edge_listed_twice()
    {
        var parse = () => EdgeBaselineFile.ParseKeys("Combat -> Stat | 1\nCombat -> Stat | 2\n");

        parse.Should().Throw<FormatException>().WithMessage("*second time*");
    }

    [Test]
    public void The_member_count_is_not_compared()
    {
        var baseline = EdgeBaselineFile.ParseKeys("Combat -> Stat | 1\n");

        var comparison = BaselineComparison.Compare(baseline, [Edge("Combat", "Stat", "Combat", "Character", "A", "B", "C")]);

        comparison.IsClean.Should().BeTrue();
    }

    [Test]
    public void A_new_edge_fails_with_a_message_that_names_it_and_says_how_to_regenerate()
    {
        var baseline = EdgeBaselineFile.ParseKeys("Combat -> Stat | 1\n");
        var actual = new[]
        {
            Edge("Combat", "Stat", "Combat", "Character", "A"),
            Edge("Combat", "Quest", "Combat", "Quests", "Combat::ApplyDamage")
        };

        var comparison = BaselineComparison.Compare(baseline, actual);
        var message = comparison.Describe("Baseline/CrossDomainEdges.txt", "SWLOR_UPDATE_SNAPSHOTS=1 dotnet test");

        comparison.IsClean.Should().BeFalse();
        comparison.NewEdges.Select(edge => edge.Key).Should().Equal("Combat -> Quest");
        comparison.RemovedEdges.Should().BeEmpty();
        message.Should().Contain("+ Combat -> Quest");
        message.Should().Contain("Combat -> Quests");
        message.Should().Contain("Combat::ApplyDamage");
        message.Should().Contain("SWLOR_UPDATE_SNAPSHOTS=1");
        message.Should().NotContain("no longer exist");
    }

    [Test]
    public void A_baseline_edge_that_is_gone_fails_so_the_baseline_stays_tight()
    {
        var baseline = EdgeBaselineFile.ParseKeys("Combat -> Stat | 1\nCombat -> Quest | 1\n");

        var comparison = BaselineComparison.Compare(baseline, [Edge("Combat", "Stat", "Combat", "Character", "A")]);
        var message = comparison.Describe("Baseline/CrossDomainEdges.txt", "regenerate");

        comparison.NewEdges.Should().BeEmpty();
        comparison.RemovedEdges.Should().Equal("Combat -> Quest");
        message.Should().Contain("- Combat -> Quest");
        message.Should().Contain("no longer exist");
    }

    [Test]
    public void The_domain_pair_report_totals_edges_per_pair()
    {
        var edges = new[]
        {
            Edge("Combat", "Stat", "Combat", "Character", "A", "B"),
            Edge("Combat", "Skill", "Combat", "Character", "C"),
            Edge("Quest", "Stat", "Quests", "Character", "D")
        };

        var report = DomainPairReport.Render("Title", edges);

        report.Should().StartWith("Title: 3 unit edge(s), 2 domain pair(s)");
        report.Should().MatchRegex(@"Combat -> Character\s+2 edge\(s\)\s+3 member\(s\)");
        report.Should().MatchRegex(@"Quests -> Character\s+1 edge\(s\)\s+1 member\(s\)");
    }
}
