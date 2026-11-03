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

    // ---- extra facades

    private const string TrackerFacade = "AchievementService.AchievementTracker";

    private static ConvertedService WithExtraFacades(params string[] facades) =>
        new("Achievement", "Character") { ExtraFacades = facades };

    [Test]
    public void The_extra_facades_of_a_service_are_read_from_the_list()
    {
        var list = ConvertedServiceList.Parse($$"""
            { "services": [
                { "name": "Achievement", "domain": "Character", "facades": [ "{{TrackerFacade}}" ] },
                { "name": "Stat", "domain": "Character" } ] }
            """);

        list.Problems.Should().BeEmpty();
        var achievement = list.Services[0];
        achievement.ExtraFacades.Should().Equal(TrackerFacade);
        achievement.Facades.Select(facade => facade.ToString()).Should().Equal(
            "SWLOR.Game.Server.Service.Achievement",
            "SWLOR.Game.Server.Service.AchievementService.AchievementTracker");
        list.Services[1].ExtraFacades.Should().BeEmpty();
        list.Services[1].Pieces.Should().ContainSingle();
    }

    [Test]
    public void An_extra_facade_is_named_by_convention_after_its_type()
    {
        var pieces = WithExtraFacades(TrackerFacade).Pieces;

        pieces.Should().HaveCount(2);
        pieces[0].Label.Should().Be("Achievement");
        pieces[1].Label.Should().Be($"Achievement (extra facade {TrackerFacade})");
        pieces[1].Facade.ToString().Should().Be("SWLOR.Game.Server.Service.AchievementService.AchievementTracker");
        pieces[1].Interface.ToString().Should().Be("SWLOR.Game.Server.Domain.Character.IAchievementTrackerService");
        pieces[1].Instance.ToString().Should().Be("SWLOR.Game.Server.Domain.Character.AchievementTrackerService");
    }

    [Test]
    public void An_extra_facade_may_sit_directly_in_the_service_namespace_or_deeper()
    {
        ConvertedPieces.FacadeKey("Tracker").ToString().Should().Be("SWLOR.Game.Server.Service.Tracker");
        ConvertedPieces.FacadeKey("AchievementService.Sub.Tracker").ToString().Should().Be("SWLOR.Game.Server.Service.AchievementService.Sub.Tracker");
    }

    [Test]
    public void A_malformed_repeated_or_unsorted_facade_name_is_a_problem_of_the_list()
    {
        var list = ConvertedServiceList.Parse("""
            { "services": [ { "name": "Achievement", "domain": "Character",
                "facades": [ "AchievementService.Beta", "AchievementService.Alpha", "AchievementService.Alpha", "bad name", "AchievementService..Gamma", "" ] } ] }
            """);

        list.Problems.Should().Contain(problem => problem.Contains("Alpha of Achievement is out of order"));
        list.Problems.Should().Contain(problem => problem.Contains("Achievement lists the extra facade AchievementService.Alpha twice"));
        list.Problems.Should().Contain(problem => problem.Contains("'bad name'") && problem.Contains("not a plain type name"));
        list.Problems.Should().Contain(problem => problem.Contains("'AchievementService..Gamma'") && problem.Contains("not a plain type name"));
        list.Problems.Should().Contain(problem => problem.Contains("'' (an extra facade of Achievement)"));
        list.Services[0].ExtraFacades.Should().Equal("AchievementService.Beta", "AchievementService.Alpha");
    }

    [Test]
    public void An_extra_facade_in_the_namespace_of_its_service_has_no_list_problems()
    {
        ConvertedServiceRules.ListProblems(ListOf(WithExtraFacades(TrackerFacade, "AchievementService.Sub.AchievementZed")), DomainMap.Parse(MapJson)).Should().BeEmpty();
    }

    [Test]
    public void An_extra_facade_outside_the_namespace_of_its_service_is_reported()
    {
        var problems = ConvertedServiceRules.ListProblems(
            ListOf(WithExtraFacades("StatService.StatTracker", "Helper")),
            DomainMap.Parse(MapJson));

        problems.Should().Contain(problem => problem.Contains("StatService.StatTracker") && problem.Contains("belongs to Stat, not to Achievement"));
        problems.Should().Contain(problem => problem.Contains("Helper") && problem.Contains("belongs to Helper, not to Achievement") && problem.Contains("Service.AchievementService"));
    }

    [Test]
    public void The_main_facade_cannot_be_listed_as_an_extra_one()
    {
        var problems = ConvertedServiceRules.ListProblems(ListOf(WithExtraFacades("Achievement")), DomainMap.Parse(MapJson));

        problems.Should().ContainSingle().Which.Should().Contain("is the main facade of Achievement");
    }

    [Test]
    public void An_extra_facade_whose_service_class_would_not_count_as_the_service_is_reported()
    {
        var problems = ConvertedServiceRules.ListProblems(ListOf(WithExtraFacades("AchievementService.Tracker")), DomainMap.Parse(MapJson));

        problems.Should().HaveCount(2, "the interface and the instance class are both misplaced");
        problems.Should().OnlyContain(problem => problem.Contains("would count as Platform/Domain.Character, not as Achievement") && problem.Contains("starts with Achievement"));
    }

    [Test]
    public void A_service_with_all_three_pieces_of_every_facade_passes()
    {
        var service = WithExtraFacades(TrackerFacade);
        var extra = service.Pieces[1];
        var facts = FactsOf(
            new TypeFact(service.Facade, false, true, true, []),
            new TypeFact(service.Interface, true, false, false, []),
            new TypeFact(service.Instance, false, false, true, [service.Interface.ToString()]),
            new TypeFact(extra.Facade, false, true, true, []),
            new TypeFact(extra.Interface, true, false, false, []),
            new TypeFact(extra.Instance, false, false, true, [extra.Interface.ToString()]));

        ConvertedServiceRules.MissingPieces(ListOf(service), facts).Should().BeEmpty();
    }

    [Test]
    public void An_extra_facade_that_does_not_exist_is_reported_with_its_pieces()
    {
        var service = WithExtraFacades(TrackerFacade);
        var facts = FactsOf(
            new TypeFact(service.Facade, false, true, true, []),
            new TypeFact(service.Interface, true, false, false, []),
            new TypeFact(service.Instance, false, false, true, [service.Interface.ToString()]));

        var problems = ConvertedServiceRules.MissingPieces(ListOf(service), facts);

        problems.Should().HaveCount(3);
        problems.Should().OnlyContain(problem => problem.StartsWith($"Achievement (extra facade {TrackerFacade}): "));
        problems.Should().Contain(problem => problem.Contains("the static facade") && problem.Contains("AchievementService.AchievementTracker does not exist"));
        problems.Should().Contain(problem => problem.Contains("the interface") && problem.Contains("IAchievementTrackerService does not exist"));
        problems.Should().Contain(problem => problem.Contains("the instance class") && problem.Contains("AchievementTrackerService does not exist"));
    }

    [Test]
    public void A_misshapen_piece_of_an_extra_facade_is_named()
    {
        var service = WithExtraFacades(TrackerFacade);
        var extra = service.Pieces[1];
        var facts = FactsOf(
            new TypeFact(service.Facade, false, true, true, []),
            new TypeFact(service.Interface, true, false, false, []),
            new TypeFact(service.Instance, false, false, true, [service.Interface.ToString()]),
            new TypeFact(extra.Facade, false, false, false, []),
            new TypeFact(extra.Interface, true, false, false, []),
            new TypeFact(extra.Instance, false, false, true, []));

        var problems = ConvertedServiceRules.MissingPieces(ListOf(service), facts);

        problems.Should().HaveCount(2);
        problems.Should().Contain(problem => problem.Contains("must be a static class") && problem.Contains("AchievementTracker"));
        problems.Should().Contain(problem => problem.Contains("must implement") && problem.Contains("IAchievementTrackerService"));
    }

    [Test]
    public void The_instance_of_an_extra_facade_must_not_call_that_facade_but_may_call_the_main_one_and_the_other_way_round()
    {
        var service = WithExtraFacades(TrackerFacade);
        var extra = service.Pieces[1];
        var graph = new TypeReferenceGraph(
            [service.Facade, service.Instance, service.Interface, extra.Facade, extra.Instance, extra.Interface],
            new Dictionary<(TypeKey Source, TypeKey Target), IReadOnlySet<string>>
            {
                [(extra.Instance, extra.Facade)] = new HashSet<string> { "AchievementTrackerService::Track" },
                [(extra.Instance, service.Facade)] = new HashSet<string> { "AchievementTrackerService::Give" },
                [(service.Instance, extra.Facade)] = new HashSet<string> { "AchievementService::Track" }
            });

        var problems = ConvertedServiceRules.InstanceUsesItsFacade(graph, ListOf(service));

        problems.Should().ContainSingle().Which.Should().Contain("AchievementTrackerService uses its own facade").And.Contain("AchievementTracker").And.Contain("Track");
    }

    [Test]
    public void The_classifier_counts_the_pieces_of_an_extra_facade_as_the_service()
    {
        var map = DomainMap.Parse(MapJson);
        var service = WithExtraFacades(TrackerFacade);

        foreach (var type in service.Pieces.SelectMany(pieces => new[] { pieces.Facade, pieces.Interface, pieces.Instance }))
            UnitClassifier.Classify(type, map).Should().Be(service.Unit, type.ToString());
    }

    // ---- static state

    [Test]
    public void Static_state_in_a_converted_service_is_reported_but_a_constant_is_not()
    {
        var problems = ConvertedServiceRules.StaticMutableFields(Facts, type => type == KeyOf(typeof(StaticStateHolder)));

        problems.Should().HaveCount(4);
        problems.Should().Contain(problem => problem.Contains("StaticStateHolder.Cache") && problem.Contains("static readonly"));
        problems.Should().Contain(problem => problem.Contains("StaticStateHolder.Counter") && !problem.Contains("static readonly"));
        problems.Should().Contain(problem => problem.Contains("StaticStateHolder.Total (static auto-property)") && !problem.Contains("static readonly"));
        problems.Should().Contain(problem => problem.Contains("StaticStateHolder.Items (static auto-property)") && problem.Contains("instance property"));
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
            type => type == facade || type == KeyOf(typeof(HandlerOnInstance)) ? new[] { facade } : null);

        problems.Should().ContainSingle().Which.Should().Contain("HandlerOnInstance.Handle").And.Contain("its facade").And.Contain("HandlerOnFacade");
    }

    [Test]
    public void Handlers_of_types_that_are_not_converted_services_are_left_alone()
    {
        ConvertedServiceRules.HandlersOffTheFacade(Facts, _ => null).Should().BeEmpty();
    }

    [Test]
    public void A_service_with_extra_facades_may_keep_its_handlers_on_any_of_them()
    {
        var facade = KeyOf(typeof(HandlerOnFacade));
        var extra = new TypeKey("Elsewhere", "ExtraFacade");

        var problems = ConvertedServiceRules.HandlersOffTheFacade(
            Facts,
            type => type == facade || type == KeyOf(typeof(HandlerOnInstance)) ? new[] { extra, facade } : null);

        problems.Should().ContainSingle().Which.Should().Contain("HandlerOnInstance.Handle").And.Contain("one of its facades").And.Contain("ExtraFacade");
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
        ConvertedServiceList.Parse("{ \"services\": [" + string.Join(",", services.Select(s =>
            $"{{ \"name\": \"{s.Name}\", \"domain\": \"{s.Domain}\", \"facades\": [{string.Join(",", s.ExtraFacades.Select(facade => $"\"{facade}\""))}] }}")) + "] }");

    private static TypeKey KeyOf(Type type) => new(type.Namespace ?? string.Empty, type.Name);
}
