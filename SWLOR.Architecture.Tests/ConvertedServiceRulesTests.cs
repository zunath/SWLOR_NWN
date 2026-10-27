using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Fixtures.Rules;
using SWLOR.Architecture.Tests.Fixtures.Rules.Composition;
using SWLOR.Architecture.Tests.Rules;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// Proves that each rule for converted services fails on a deliberate violation and passes on the right shape, using
/// the types in <c>Fixtures/Rules</c> and small synthetic inputs. <see cref="ConvertedServicesTests"/> applies the
/// same rules to the real server.
/// </summary>
[TestFixture]
public class ConvertedServiceRulesTests
{
    private const string MapJson = """
        {
          "platformDomain": "Platform",
          "featureTypeDomain": "Gameplay",
          "unrestrictedTargetDomains": ["Core", "Platform"],
          "domains": {
            "Core": { "services": ["Log"], "featureFolders": [] },
            "Character": { "services": ["Achievement", "Stat"], "featureFolders": [] },
            "Gameplay": { "services": [], "featureFolders": [] }
          }
        }
        """;

    private static readonly AssemblyFacts Facts = AssemblyFactScanner.Scan(typeof(ConvertedServiceRulesTests).Assembly.Location);
    private static readonly TypeReferenceGraph Graph = AssemblyScanner.Scan(typeof(ConvertedServiceRulesTests).Assembly.Location);

    // ---- the converted-services list

    [Test]
    public void A_well_formed_list_in_the_right_domains_has_no_problems()
    {
        var list = ConvertedServiceList.Parse("""{ "services": [ { "name": "Achievement", "domain": "Character" }, { "name": "Stat", "domain": "Character" } ] }""");

        ConvertedServiceRules.ListProblems(list, DomainMap.Parse(MapJson)).Should().BeEmpty();
    }

    [Test]
    public void The_list_must_agree_with_the_domain_map()
    {
        var list = ConvertedServiceList.Parse("""{ "services": [ { "name": "Achievement", "domain": "Core" }, { "name": "Unmapped", "domain": "Core" } ] }""");

        var problems = ConvertedServiceRules.ListProblems(list, DomainMap.Parse(MapJson));

        problems.Should().Contain(problem => problem.Contains("Achievement") && problem.Contains("puts it in Character"));
        problems.Should().Contain(problem => problem.Contains("Unmapped") && problem.Contains("not a service in DomainMap.json"));
    }

    [Test]
    public void The_list_must_be_sorted_without_duplicates_and_with_plain_names()
    {
        var list = ConvertedServiceList.Parse("""{ "services": [ { "name": "Stat", "domain": "Character" }, { "name": "Achievement", "domain": "Character" }, { "name": "Stat", "domain": "Character" }, { "name": "Bad Name", "domain": "Character" } ] }""");

        list.Problems.Should().Contain(problem => problem.Contains("Achievement is out of order"));
        list.Problems.Should().Contain(problem => problem.Contains("Stat is listed twice"));
        list.Problems.Should().Contain(problem => problem.Contains("'Bad Name' is not a plain service name"));
    }

    [Test]
    public void A_converted_service_is_named_by_convention()
    {
        var service = new ConvertedService("Achievement", "Character");

        service.Facade.ToString().Should().Be("SWLOR.Game.Server.Service.Achievement");
        service.Interface.ToString().Should().Be("SWLOR.Game.Server.Domain.Character.IAchievementService");
        service.Instance.ToString().Should().Be("SWLOR.Game.Server.Domain.Character.AchievementService");
    }

    // ---- the three pieces

    [Test]
    public void A_service_with_all_three_pieces_passes()
    {
        var service = new ConvertedService("Achievement", "Character");
        var facts = FactsOf(
            new TypeFact(service.Facade, false, true, true, []),
            new TypeFact(service.Interface, true, false, false, []),
            new TypeFact(service.Instance, false, false, true, [service.Interface.ToString()]));

        ConvertedServiceRules.MissingPieces(ListOf(service), facts).Should().BeEmpty();
    }

    [Test]
    public void A_missing_or_misshapen_piece_is_named()
    {
        var service = new ConvertedService("Achievement", "Character");
        var facts = FactsOf(
            new TypeFact(service.Facade, false, false, false, []),
            new TypeFact(service.Instance, false, false, true, []));

        var problems = ConvertedServiceRules.MissingPieces(ListOf(service), facts);

        problems.Should().Contain(problem => problem.Contains("must be a static class"));
        problems.Should().Contain(problem => problem.Contains("interface") && problem.Contains("does not exist"));
        problems.Should().Contain(problem => problem.Contains("must implement"));
    }

    [Test]
    public void A_service_with_nothing_built_reports_all_three_pieces()
    {
        var problems = ConvertedServiceRules.MissingPieces(ListOf(new ConvertedService("Ghost", "Character")), FactsOf());

        problems.Should().HaveCount(3);
    }

    // ---- static state

    [Test]
    public void Static_state_in_a_converted_service_is_reported_but_a_constant_is_not()
    {
        var problems = ConvertedServiceRules.StaticMutableFields(Facts, type => type == KeyOf(typeof(StaticStateHolder)));

        problems.Should().HaveCount(2);
        problems.Should().Contain(problem => problem.Contains("StaticStateHolder.Cache") && problem.Contains("static readonly"));
        problems.Should().Contain(problem => problem.Contains("StaticStateHolder.Counter") && !problem.Contains("static readonly"));
        problems.Should().NotContain(problem => problem.Contains("Limit"));
    }

    [Test]
    public void A_service_that_keeps_its_state_on_the_instance_passes()
    {
        ConvertedServiceRules.StaticMutableFields(Facts, type => type == KeyOf(typeof(CleanInstanceService))).Should().BeEmpty();
    }

    [Test]
    public void Static_state_in_a_type_that_is_not_a_converted_service_is_not_this_rules_business()
    {
        ConvertedServiceRules.StaticMutableFields(Facts, _ => false).Should().BeEmpty();
    }

    // ---- handlers

    [Test]
    public void A_handler_on_the_instance_class_is_reported_and_one_on_the_facade_is_not()
    {
        var facade = KeyOf(typeof(HandlerOnFacade));

        var problems = ConvertedServiceRules.HandlersOffTheFacade(
            Facts,
            type => type == facade || type == KeyOf(typeof(HandlerOnInstance)) ? facade : null);

        problems.Should().ContainSingle().Which.Should().Contain("HandlerOnInstance.Handle").And.Contain("HandlerOnFacade");
    }

    [Test]
    public void Handlers_of_types_that_are_not_converted_services_are_left_alone()
    {
        ConvertedServiceRules.HandlersOffTheFacade(Facts, _ => null).Should().BeEmpty();
    }

    // ---- instance and facade

    [Test]
    public void An_instance_that_calls_its_own_facade_is_reported()
    {
        var service = new ConvertedService("Achievement", "Character");
        var graph = new TypeReferenceGraph(
            [service.Facade, service.Instance, service.Interface],
            new Dictionary<(TypeKey Source, TypeKey Target), IReadOnlySet<string>>
            {
                [(service.Instance, service.Facade)] = new HashSet<string> { "AchievementService::Give" },
                [(service.Facade, service.Interface)] = new HashSet<string> { "Achievement::Give" }
            });

        var problems = ConvertedServiceRules.InstanceUsesItsFacade(graph, ListOf(service));

        problems.Should().ContainSingle().Which.Should().Contain("AchievementService").And.Contain("AchievementService::Give");
    }

    // ---- service location

    private static IReadOnlyList<string> Locate()
    {
        return ServiceLocationRule.Find(
            Graph,
            Facts,
            typeof(ContainerOwner).Namespace!,
            KeyOf(typeof(FixtureAccessor)),
            type => type == KeyOf(typeof(AccessorFacade)));
    }

    [TestCase(typeof(ProviderUser), "System.IServiceProvider")]
    [TestCase(typeof(ExtensionUser), "ServiceProviderServiceExtensions")]
    [TestCase(typeof(ParameterUser), "System.IServiceProvider")]
    [TestCase(typeof(ActivatorUser), "ActivatorUtilities")]
    [TestCase(typeof(AccessorIntruder), "FixtureAccessor")]
    public void Asking_the_container_for_a_service_outside_composition_is_reported(Type offender, string used)
    {
        Locate().Should().Contain(problem => problem.StartsWith(KeyOf(offender).ToString()) && problem.Contains(used));
    }

    [TestCase(typeof(CleanUser))]
    [TestCase(typeof(ContainerOwner))]
    [TestCase(typeof(FixtureAccessor))]
    [TestCase(typeof(AccessorFacade))]
    [TestCase(typeof(StaticStateHolder))]
    public void Types_that_may_resolve_or_that_never_do_are_not_reported(Type allowed)
    {
        Locate().Should().NotContain(problem => problem.StartsWith(KeyOf(allowed).ToString() + " "));
    }

    private static AssemblyFacts FactsOf(params TypeFact[] types) => new(types, [], [], []);

    private static ConvertedServiceList ListOf(params ConvertedService[] services) =>
        ConvertedServiceList.Parse("{ \"services\": [" + string.Join(",", services.Select(s => $"{{ \"name\": \"{s.Name}\", \"domain\": \"{s.Domain}\" }}")) + "] }");

    private static TypeKey KeyOf(Type type) => new(type.Namespace ?? string.Empty, type.Name);
}
