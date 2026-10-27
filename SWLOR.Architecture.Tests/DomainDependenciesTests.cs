using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>Domain map parsing and the lift from type references to unit edges, on small synthetic inputs.</summary>
[TestFixture]
public class DomainDependenciesTests
{
    private const string MapJson = """
        {
          "platformDomain": "Platform",
          "featureTypeDomain": "Gameplay",
          "unrestrictedTargetDomains": ["Core", "Platform"],
          "domains": {
            "Core": { "services": ["Log"], "featureFolders": [] },
            "Combat": { "services": ["Combat", "Enmity"], "featureFolders": [] },
            "Character": { "services": ["Stat"], "featureFolders": ["PerkDefinition"] },
            "Gameplay": { "services": [], "featureFolders": [] }
          }
        }
        """;

    private static TypeKey Service(string name, string? subFolder = null) =>
        new(subFolder is null ? "SWLOR.Game.Server.Service" : $"SWLOR.Game.Server.Service.{subFolder}", name);

    private static TypeKey FeatureFolder(string folder, string name) => new($"SWLOR.Game.Server.Feature.{folder}", name);

    private static TypeKey FeatureType(string name) => new("SWLOR.Game.Server.Feature", name);

    private static TypeKey PlatformType(string name) => new("SWLOR.Game.Server.Native", name);

    private static TypeReferenceGraph GraphOf(
        IEnumerable<TypeKey> types,
        params (TypeKey Source, TypeKey Target, string[] Members)[] references) =>
        new(
            types.ToList(),
            references.ToDictionary(
                reference => (reference.Source, reference.Target),
                reference => (IReadOnlySet<string>)reference.Members.ToHashSet()));

    [Test]
    public void Map_assigns_services_folders_feature_types_and_platform_code_to_domains()
    {
        var map = DomainMap.Parse(MapJson);

        map.Problems.Should().BeEmpty();
        map.DomainOf(new DependencyUnit(UnitKind.Service, "Stat")).Should().Be("Character");
        map.DomainOf(new DependencyUnit(UnitKind.FeatureFolder, "PerkDefinition")).Should().Be("Character");
        map.DomainOf(new DependencyUnit(UnitKind.FeatureType, "Auditing")).Should().Be("Gameplay");
        map.DomainOf(new DependencyUnit(UnitKind.Platform, "Native")).Should().Be("Platform");
        map.DomainOf(new DependencyUnit(UnitKind.Service, "Unlisted")).Should().BeNull();
        map.IsUnrestrictedTarget("Core").Should().BeTrue();
        map.IsUnrestrictedTarget("Combat").Should().BeFalse();
    }

    [Test]
    public void Map_reports_a_unit_listed_in_two_domains()
    {
        var json = MapJson.Replace("\"services\": [\"Stat\"]", "\"services\": [\"Stat\", \"Enmity\"]");

        DomainMap.Parse(json).Problems.Should().ContainSingle(problem => problem.Contains("Enmity") && problem.Contains("Combat") && problem.Contains("Character"));
    }

    [Test]
    public void Map_reports_names_that_are_not_plain_identifiers_and_unknown_domains()
    {
        var json = MapJson
            .Replace("\"services\": [\"Log\"]", "\"services\": [\"Log\", \"Bad Name\"]")
            .Replace("[\"Core\", \"Platform\"]", "[\"Core\", \"Missing\"]");

        var problems = DomainMap.Parse(json).Problems;

        problems.Should().Contain(problem => problem.Contains("Bad Name"));
        problems.Should().Contain(problem => problem.Contains("Missing"));
    }

    [Test]
    public void Edges_between_domains_are_found_and_edges_inside_a_domain_are_not()
    {
        var graph = GraphOf(
            [Service("Combat"), Service("Enmity"), Service("Stat"), FeatureFolder("PerkDefinition", "ArmorPerks")],
            (Service("Combat"), Service("Enmity"), ["Combat::A"]),
            (Service("Combat"), Service("Stat"), ["Combat::B", "Combat::C"]),
            (FeatureFolder("PerkDefinition", "ArmorPerks"), Service("Stat"), ["ArmorPerks::D"]));

        var dependencies = DomainDependencies.Build(graph, DomainMap.Parse(MapJson));

        dependencies.UnmappedUnits.Should().BeEmpty();
        dependencies.Edges.Select(edge => edge.Key).Should().Equal("Combat -> Stat");
        dependencies.Edges.Single().DomainPair.Should().Be("Combat -> Character");
        dependencies.Edges.Single().MemberCount.Should().Be(2);
    }

    [Test]
    public void Types_of_one_service_and_its_sub_namespace_are_one_unit_with_their_members_merged()
    {
        var graph = GraphOf(
            [Service("Combat"), Service("HitResult", "CombatService"), Service("Stat")],
            (Service("Combat"), Service("Stat"), ["Combat::A"]),
            (Service("HitResult", "CombatService"), Service("Stat"), ["HitResult::B"]));

        var edge = DomainDependencies.Build(graph, DomainMap.Parse(MapJson)).Edges.Single();

        edge.Key.Should().Be("Combat -> Stat");
        edge.Members.Should().BeEquivalentTo("Combat::A", "HitResult::B");
    }

    [Test]
    public void Edges_into_core_and_platform_are_unrestricted_and_the_rest_are_enforced()
    {
        var graph = GraphOf(
            [Service("Combat"), Service("Log"), Service("Stat"), PlatformType("Hook"), FeatureType("Auditing")],
            (Service("Combat"), Service("Log"), ["Combat::A"]),
            (Service("Combat"), PlatformType("Hook"), ["Combat::B"]),
            (Service("Combat"), Service("Stat"), ["Combat::C"]),
            (PlatformType("Hook"), Service("Combat"), ["Hook::D"]),
            (FeatureType("Auditing"), Service("Combat"), ["Auditing::E"]),
            (Service("Log"), Service("Stat"), ["Log::F"]));

        var dependencies = DomainDependencies.Build(graph, DomainMap.Parse(MapJson));

        dependencies.UnrestrictedEdges.Select(edge => edge.Key).Should().BeEquivalentTo("Combat -> Log", "Combat -> Platform/Native");
        dependencies.EnforcedEdges.Select(edge => edge.Key).Should().BeEquivalentTo(
            "Combat -> Stat", "Platform/Native -> Combat", "Feature.Auditing -> Combat", "Log -> Stat");
    }

    [Test]
    public void An_unlisted_service_or_folder_is_reported_and_its_edges_are_left_out()
    {
        var graph = GraphOf(
            [Service("Combat"), Service("Brand"), FeatureFolder("NewDefinition", "Thing")],
            (Service("Combat"), Service("Brand"), ["Combat::A"]),
            (FeatureFolder("NewDefinition", "Thing"), Service("Combat"), ["Thing::B"]));

        var dependencies = DomainDependencies.Build(graph, DomainMap.Parse(MapJson));

        dependencies.UnmappedUnits.Select(unit => unit.ToString()).Should().Equal("Brand", "Feature/NewDefinition");
        dependencies.Edges.Should().BeEmpty();
    }

    [Test]
    public void Types_that_belong_to_no_unit_are_ignored()
    {
        var generated = new TypeKey(string.Empty, "<>f__AnonymousType0");
        var graph = GraphOf(
            [Service("Combat"), generated],
            (Service("Combat"), generated, ["Combat::A"]),
            (generated, Service("Combat"), ["generated::B"]));

        var dependencies = DomainDependencies.Build(graph, DomainMap.Parse(MapJson));

        dependencies.Units.Select(unit => unit.ToString()).Should().Equal("Combat");
        dependencies.Edges.Should().BeEmpty();
    }
}
