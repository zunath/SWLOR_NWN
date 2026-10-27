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

        Fail(ConvertedServiceRules.HandlersOffTheFacade(ArchitectureProject.Facts, type => ServiceOf(type, map, services)?.Facade),
            "Event handlers of converted services stay on the static facade");
    }

    [Test]
    public void An_instance_class_never_calls_the_facade_that_forwards_to_it()
    {
        Fail(ConvertedServiceRules.InstanceUsesItsFacade(ArchitectureProject.Graph, List), "An instance uses its own facade");
    }

    [Test]
    public void Nothing_locates_a_service_outside_composition_and_the_facades_of_converted_services()
    {
        var facades = List.Services.Select(service => service.Facade).ToHashSet();

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
