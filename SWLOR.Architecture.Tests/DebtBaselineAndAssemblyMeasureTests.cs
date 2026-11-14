using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Debt;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// The Phase 5 debt baseline format and comparison, and the measures read from the compiled server (domain cycles, static
/// state) on hand-made inputs.
/// </summary>
[TestFixture]
public class DebtBaselineAndAssemblyMeasureTests
{
    private const string Root = "SWLOR.Game.Server";

    private static TypeKey Type(string @namespace, string name) => new(Root + @namespace, name);

    [Test]
    public void A_rendered_baseline_reads_back_sorted_with_its_counts()
    {
        var text = DebtBaselineFile.Render([new DebtEntry("b.cs", 2, "detail is not written"), new DebtEntry("a.cs | x", 1)], ["Header", ""]);

        text.Should().Be("# Header\n#\n\na.cs | x | 1\nb.cs | 2\n");
        DebtBaselineFile.Parse(text.Replace("\n", "\r\n")).Should().BeEquivalentTo(new Dictionary<string, int> { ["a.cs | x"] = 1, ["b.cs"] = 2 });
    }

    [TestCase("a.cs")]
    [TestCase("a.cs | many")]
    [TestCase(" | 3")]
    public void Parsing_rejects_a_malformed_line(string line)
    {
        var parse = () => DebtBaselineFile.Parse(line);

        parse.Should().Throw<FormatException>().WithMessage("*line 1*");
    }

    [Test]
    public void Parsing_rejects_an_entry_listed_twice()
    {
        var parse = () => DebtBaselineFile.Parse("a.cs | 1\na.cs | 2\n");

        parse.Should().Throw<FormatException>().WithMessage("*second time*");
    }

    [Test]
    public void New_and_grown_debt_fails_with_what_to_do()
    {
        var comparison = DebtComparison.Compare(
            new Dictionary<string, int> { ["Grows.cs"] = 2, ["Same.cs"] = 1 },
            [new DebtEntry("Grows.cs", 3), new DebtEntry("Same.cs", 1), new DebtEntry("New.cs", 4, "4 in a static context")]);

        var message = comparison.Describe("1. Static service lookup", "Baseline/Phase5/StaticLookups.txt", "uses", "regenerate");

        comparison.IsClean.Should().BeFalse();
        message.Should().Contain("+ New.cs | 4 uses   [4 in a static context]")
            .And.Contain("> Grows.cs | 2 -> 3 uses")
            .And.Contain("only shrinks")
            .And.Contain("regenerate")
            .And.NotContain("Debt paid");
    }

    [Test]
    public void Paid_debt_fails_too_so_the_baseline_shrinks_in_the_same_pull_request()
    {
        var comparison = DebtComparison.Compare(
            new Dictionary<string, int> { ["Shrinks.cs"] = 5, ["Gone.cs"] = 1 },
            [new DebtEntry("Shrinks.cs", 4)]);

        var message = comparison.Describe("2. Large types", "LargeTypes.txt", "lines", "regenerate");

        comparison.IsClean.Should().BeFalse();
        message.Should().Contain("< Shrinks.cs | 5 -> 4 lines").And.Contain("- Gone.cs | 1 lines").And.Contain("Debt paid");
        DebtComparison.Compare(new Dictionary<string, int> { ["Shrinks.cs"] = 4 }, [new DebtEntry("Shrinks.cs", 4)]).IsClean.Should().BeTrue();
    }

    [Test]
    public void Domain_cycles_are_pairs_with_edges_both_ways_counting_unit_edges()
    {
        static UnitEdge Edge(string from, string to, string fromDomain, string toDomain, int members)
        {
            DependencyUnit.TryParse(from, out var fromUnit).Should().BeTrue();
            DependencyUnit.TryParse(to, out var toUnit).Should().BeTrue();
            return new UnitEdge(fromUnit, toUnit, fromDomain, toDomain, Enumerable.Range(0, members).Select(index => $"m{index}").ToHashSet());
        }

        var cycles = DomainCycleScanner.Scan(
        [
            Edge("Domain/Combat", "Domain/Character", "Combat", "Character", 5),
            Edge("Domain/Combat", "Feature/PerkDefinition", "Combat", "Character", 1),
            Edge("Domain/Character", "Domain/Combat", "Character", "Combat", 2),
            Edge("Domain/Quests", "Domain/Character", "Quests", "Character", 1),
            Edge("Domain/Character", "Platform/Core", "Character", "Platform", 3),
            Edge("Platform/Native", "Domain/Character", "Platform", "Character", 1)
        ]);

        cycles.Select(cycle => cycle.Key).Should().Equal("Character <-> Combat", "Character <-> Platform");
        var combat = cycles[0];
        (combat.ForwardEdges, combat.BackwardEdges).Should().Be((1, 2));
        combat.Detail.Should().Be("Character -> Combat: 1 edge(s); Combat -> Character: 2 edge(s)");
        DebtMeasures.DomainCycles(cycles).Entries.Should().Contain(new DebtEntry("Character <-> Combat", 3, combat.Detail));
    }

    [Test]
    public void Static_state_lists_changeable_fields_and_container_accessors_outside_the_composition_root()
    {
        var content = Type(".Feature", "StoreManagement");
        var helper = Type(".Domain.Foundation", "Tables");
        var hooks = Type(".Composition", "NativeHookAccess");
        var accessor = Type(".Composition", "FacadeAccessor");
        var facts = new AssemblyFacts(
            [],
            [
                new StaticFieldFact(content, "StoreManagement", "_stores", IsLiteral: false, IsInitOnly: true),
                new StaticFieldFact(helper, "Tables", "Names", IsLiteral: false, IsInitOnly: true, IsDeeplyImmutable: true),
                new StaticFieldFact(helper, "Tables", "Limit", IsLiteral: true, IsInitOnly: false),
                new StaticFieldFact(helper, "Tables+Cache", "_last", IsLiteral: false, IsInitOnly: false),
                new StaticFieldFact(Type(".Composition", "ContainerHost"), "ContainerHost", "_current", IsLiteral: false, IsInitOnly: false)
            ],
            [],
            []);
        var graph = new TypeReferenceGraph(
            [],
            new Dictionary<(TypeKey, TypeKey), IReadOnlySet<string>>
            {
                [(hooks, accessor)] = new HashSet<string> { "NativeHookAccess::get_Hooks" },
                [(Type(".Composition", "GameServices"), accessor)] = new HashSet<string> { "GameServices::get_Stat" }
            });

        var holders = StaticStateScanner.Scan(facts, graph);
        var written = new HashSet<StaticFieldFact> { facts.StaticFields[0] };
        var exemptions = ExemptionList.Parse("""{ "exemptions": [], "approvedStaticEntryPoints": [ { "type": "SWLOR.Game.Server.Domain.Foundation.Tables", "reason": "approved" } ] }""");
        var measure = DebtMeasures.StaticState(holders, written, exemptions);

        holders.Select(holder => (holder.Type.ToString(), holder.Count, holder.IsContent)).Should().BeEquivalentTo(new[]
        {
            ("SWLOR.Game.Server.Composition.NativeHookAccess", 1, false),
            ("SWLOR.Game.Server.Domain.Foundation.Tables", 1, false),
            ("SWLOR.Game.Server.Feature.StoreManagement", 1, true)
        });
        measure.Entries.Should().BeEquivalentTo(new[]
        {
            new DebtEntry("SWLOR.Game.Server.Composition.NativeHookAccess", 1, "resolves from the container: NativeHookAccess::get_Hooks"),
            new DebtEntry("SWLOR.Game.Server.Feature.StoreManagement", 1, "fields: _stores*")
        }, "an approved entry point is not listed");
    }
}
