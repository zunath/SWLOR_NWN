using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Rules;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// Applies the dependency-injection rules to the real server: the services in <c>ConvertedServices.json</c> are built
/// the way Readmes/DependencyInjection.md says, and nothing outside <c>Composition</c> and the facades locates a
/// service. <see cref="ConvertedServiceRulesTests"/> proves the rules themselves.
/// </summary>
[TestFixture]
public class ConvertedServicesTests
{
    private const string ListName = "SWLOR.Architecture.Tests/ConvertedServices.json";
    private const string CompositionNamespace = "SWLOR.Game.Server.Composition";

    private static ConvertedServiceList List => ArchitectureProject.ConvertedServices;

    private static DomainMap Map => DomainMap.Load(ArchitectureProject.DomainMapPath);

    [Test]
    public void The_converted_services_list_is_well_formed_and_agrees_with_the_domain_map()
    {
        Fail(ConvertedServiceRules.ListProblems(List, Map), $"{ListName} has problems");
    }

    [Test]
    public void Combat_is_listed_with_its_parts_and_there_is_no_list_of_services_in_progress()
    {
        // P2-CB10 listed Combat, the last service converted in pieces, and removed the temporary inProgress list.
        using var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(ArchitectureProject.ConvertedServicesPath));
        document.RootElement.TryGetProperty("inProgress", out _).Should().BeFalse($"{ListName} has no inProgress list any more");
        ConvertedServiceList.Parse("""{ "services": [], "inProgress": [] }""").Problems
            .Should().ContainSingle(problem => problem.Contains("The inProgress list is gone"));

        var combat = List.Services.Should().ContainSingle(service => service.Name == "Combat").Subject;
        combat.Domain.Should().Be("Combat");
        combat.Extras.Select(extra => extra.Name).Should().Equal(
            "CombatAbilityActivation", "CombatAbilityBonus", "CombatAbilityHit", "CombatAbilityImpact", "CombatActivity",
            "CombatAttackTiming", "CombatDamageCatalog", "CombatDamageModifiers", "CombatDamageTaken", "CombatFeedback",
            "CombatHitEffects", "CombatQueries", "CombatResources", "CombatSuppression");
    }

    [Test]
    public void Every_converted_service_has_a_facade_an_interface_and_an_instance_class()
    {
        Fail(ConvertedServiceRules.MissingPieces(List, ArchitectureProject.Facts), "A converted service is missing a piece");
    }

    [Test]
    public void Converted_services_hold_no_static_mutable_state()
    {
        var map = Map;
        var services = List.Services;
        bool IsServiceType(TypeKey type) => ServiceOf(type, map, services) is not null;

        Fail(ConvertedServiceRules.StaticMutableFields(ArchitectureProject.Facts, IsServiceType),
            "Converted services must keep their state on the instance");
    }

    [Test]
    public void Event_handlers_of_converted_services_stay_on_the_facade()
    {
        var map = Map;
        var services = List.Services;

        // A service whose facades were removed is checked by the next rule: its handlers are on the instance class.
        Fail(ConvertedServiceRules.HandlersOffTheFacade(ArchitectureProject.Facts, type => ServiceOf(type, map, services) is { FacadeRemoved: false } service ? service.Facades : null),
            "Event handlers of converted services stay on the static facade");
    }

    [Test]
    public void Event_handlers_of_a_service_whose_facade_was_removed_are_on_its_instance_class()
    {
        var map = Map;
        var services = List.Services;
        // A part of a split service (Combat) is an instance class too: the facade forwarded each handler to the part that owns it.
        IReadOnlyList<TypeKey>? InstancesOf(TypeKey type) => ServiceOf(type, map, services) is { FacadeRemoved: true } service
            ? service.Pieces.Select(pieces => pieces.Instance).ToList()
            : null;

        services.Should().Contain(service => service.FacadeRemoved, "the pilot (ServerTask, Taxi) removed its facades, so the rule has something to check");
        Fail(ConvertedServiceRules.HandlersOffTheInstance(ArchitectureProject.Facts, InstancesOf),
            "Event handlers of a service whose facade was removed are on its instance class");
    }

    [Test]
    public void No_class_the_container_builds_uses_GameServices()
    {
        ArchitectureProject.Facts.Types.Should().Contain(type => type.Key == ConvertedService.Accessor, "the rule names the real accessor");
        var registered = List.Services.SelectMany(service => service.Pieces).SelectMany(pieces => new[] { pieces.Instance, pieces.Interface });

        Fail(ConvertedServiceRules.RegisteredClassesUseTheAccessor(ArchitectureProject.Graph, registered, ConvertedService.Accessor),
            "A class the container builds takes its dependencies through the constructor, never through GameServices");
    }

    [Test]
    public void An_instance_class_never_calls_the_facade_that_forwards_to_it()
    {
        Fail(ConvertedServiceRules.InstanceUsesItsFacade(ArchitectureProject.Graph, List), "An instance uses its own facade");
    }

    [Test]
    public void Nothing_locates_a_service_outside_composition_and_the_facades_of_converted_services()
    {
        var facades = ConvertedServiceRules.FacadesMayLocate(List);

        var problems = ServiceLocationRule.Find(
            ArchitectureProject.Graph,
            ArchitectureProject.Facts,
            CompositionNamespace,
            new TypeKey(CompositionNamespace, "FacadeAccessor"),
            facades.Contains);

        Fail(problems, "Service location is allowed only in Composition and in the facades of converted services");
    }

    [Test]
    public void Types_in_a_domain_namespace_are_named_after_a_service_of_that_domain()
    {
        // Domain.<Domain> types are classified by name (UnitClassifier). A type that names no service of its domain
        // would show up as a Platform dependency, so name it after its service (for example AchievementNotification).
        var map = Map;
        var misplaced = ArchitectureProject.Facts.Types
            .Where(type => type.Key.Namespace.StartsWith(UnitClassifier.RootNamespace + ".Domain.", StringComparison.Ordinal))
            .Where(type => UnitClassifier.Classify(type.Key, map) is { Kind: not UnitKind.Service })
            .Select(type => $"{type.Key} is in a domain namespace but names no service of that domain in DomainMap.json")
            .OrderBy(message => message, StringComparer.Ordinal)
            .ToList();

        Fail(misplaced, "Types in Domain.<Domain> belong to a service");
    }

    /// <summary>The converted service a top-level type of the server belongs to, or null.</summary>
    private static ConvertedService? ServiceOf(TypeKey type, DomainMap map, IReadOnlyList<ConvertedService> services)
    {
        var unit = UnitClassifier.Classify(type, map);
        return unit is { Kind: UnitKind.Service } service ? services.FirstOrDefault(s => s.Name == service.Name) : null;
    }

    private static void Fail(IReadOnlyList<string> problems, string headline)
    {
        if (problems.Count == 0)
            return;

        Assert.Fail($"{headline} ({problems.Count}):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", problems)}");
    }
}
