using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Rules;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// Applies the dependency-injection rules to the real server: the services in <c>ConvertedServices.json</c> are built
/// the way Readmes/DependencyInjection.md says, and nothing outside <c>Composition</c> locates a service. <see cref="ConvertedServiceRulesTests"/> proves the rules themselves.
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
    public void Every_converted_service_has_an_interface_an_instance_class_and_a_GameServices_property()
    {
        Fail(ConvertedServiceRules.MissingPieces(List, ArchitectureProject.Facts), "A converted service is missing a piece");
    }

    /// <summary>
    /// Support types of services that were never converted (the folders <c>AnimationService</c>, <c>DMToolService</c> and
    /// <c>RoleplayXPService</c>, whose services are not in <c>ConvertedServices.json</c>). The rules below never reached them
    /// while they sat in <c>Service.&lt;Name&gt;Service</c>; P2-Z2 moved them into their domain namespace, which the rules now
    /// cover whole, so they keep their constant tables and the one boot handler (<c>AnimationPreviewCatalog.CacheCategories</c>)
    /// until their code is converted. Nothing may be added: remove a line when its type is fixed (the list is kept tight).
    /// </summary>
    private static readonly string[] SupportTypesOfUnconvertedServices =
    [
        "SWLOR.Game.Server.Domain.Administration.DMSearchText",
        "SWLOR.Game.Server.Domain.Character.OOCDetectionModel",
        "SWLOR.Game.Server.Domain.Character.OOCMessageScorer",
        "SWLOR.Game.Server.Domain.Character.RoleplayMessageClassifier",
        "SWLOR.Game.Server.Domain.Foundation.ActiveAbilityAnimationCatalog",
        "SWLOR.Game.Server.Domain.Foundation.AnimationPreviewCatalog"
    ];

    [Test]
    public void Each_support_type_of_an_unconverted_service_still_breaks_a_rule()
    {
        var services = List.Services;
        var withState = ConvertedServiceRules.StaticMutableFields(ArchitectureProject.Facts, type => ServicesOwning(type, services, exempt: false).Count > 0);
        var withHandlers = ConvertedServiceRules.HandlersOffTheInstance(ArchitectureProject.Facts,
            type => ServicesOwning(type, services, exempt: false) is { Count: > 0 } owners ? owners.SelectMany(service => service.Pieces).Select(pieces => pieces.Instance).ToList() : null);

        var stale = SupportTypesOfUnconvertedServices
            .Where(type => !withState.Concat(withHandlers).Any(problem => problem.StartsWith(type + ".", StringComparison.Ordinal)))
            .ToList();

        Fail(stale, "These types no longer hold static state or a handler: remove them from SupportTypesOfUnconvertedServices");
    }

    [Test]
    public void Converted_services_hold_no_static_mutable_state()
    {
        var services = List.Services;
        bool IsServiceType(TypeKey type) => ServicesOwning(type, services).Count > 0;

        Fail(ConvertedServiceRules.StaticMutableFields(ArchitectureProject.Facts, IsServiceType),
            "Converted services must keep their state on the instance");
    }

    [Test]
    public void Event_handlers_of_converted_services_are_on_their_instance_classes()
    {
        var services = List.Services;
        // A part of a split service (Combat) is an instance class too: each handler is on the part that owns it.
        IReadOnlyList<TypeKey>? InstancesOf(TypeKey type) => ServicesOwning(type, services) is { Count: > 0 } owners
            ? owners.SelectMany(service => service.Pieces).Select(pieces => pieces.Instance).ToList()
            : null;

        Fail(ConvertedServiceRules.HandlersOffTheInstance(ArchitectureProject.Facts, InstancesOf),
            "Event handlers of converted services are on their instance classes");
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
    public void Nothing_locates_a_service_outside_composition()
    {
        var problems = ServiceLocationRule.Find(
            ArchitectureProject.Graph,
            ArchitectureProject.Facts,
            CompositionNamespace,
            new TypeKey(CompositionNamespace, "FacadeAccessor"));

        Fail(problems, "Service location is allowed only in Composition");
    }

    [Test]
    public void Service_classes_in_a_domain_namespace_are_pieces_of_converted_services()
    {
        // Since P2-Z2 a domain's support types share its namespace with its services, so only the types named like a
        // service class (FooService, IFooService) must be registered pieces; ListProblems and MissingPieces tie each piece
        // to a service of its domain in DomainMap.json.
        Fail(ConvertedServiceRules.UnlistedServiceClasses(ArchitectureProject.Facts, List),
            "A class named like a service in Domain.<Domain> is a piece of a converted service");
    }

    /// <summary>
    /// The converted services a top-level type of the server belongs to: for a type in <c>Domain.&lt;Domain&gt;</c> or a topic
    /// below it, every converted service of that domain (the domain's services and their support types share the namespace,
    /// so the rules hold for all of it, except <see cref="SupportTypesOfUnconvertedServices"/> unless
    /// <paramref name="exempt"/> is false); for a type directly in <c>Service</c>, the converted service named like it. Empty
    /// for other types.
    /// </summary>
    private static IReadOnlyList<ConvertedService> ServicesOwning(TypeKey type, IReadOnlyList<ConvertedService> services, bool exempt = true)
    {
        if (exempt && SupportTypesOfUnconvertedServices.Contains(type.ToString()))
            return Array.Empty<ConvertedService>();

        return UnitClassifier.Classify(type) switch
        {
            { Kind: UnitKind.Domain } domain => services.Where(service => service.Domain == domain.Name).ToList(),
            { Kind: UnitKind.Service } named => services.Where(service => service.Name == named.Name).ToList(),
            _ => Array.Empty<ConvertedService>()
        };
    }

    private static void Fail(IReadOnlyList<string> problems, string headline)
    {
        if (problems.Count == 0)
            return;

        Assert.Fail($"{headline} ({problems.Count}):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", problems)}");
    }
}
