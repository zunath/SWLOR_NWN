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

        service.Interface.ToString().Should().Be("SWLOR.Game.Server.Domain.Character.IAchievementService");
        service.Instance.ToString().Should().Be("SWLOR.Game.Server.Domain.Character.AchievementService");
        service.Pieces.Should().ContainSingle().Which.AccessorProperty.Should().Be("Achievement");
    }

    // ---- the pieces

    [Test]
    public void A_service_with_its_interface_instance_and_accessor_property_passes()
    {
        var service = new ConvertedService("Achievement", "Character");
        var facts = new AssemblyFacts(
            [
                new TypeFact(service.Interface, true, false, false, []),
                new TypeFact(service.Instance, false, false, true, [service.Interface.ToString()]),
            ],
            [], [], [],
            [AccessorProperty("Achievement", service.Interface)]);

        ConvertedServiceRules.MissingPieces(ListOf(service), facts).Should().BeEmpty();
    }

    [Test]
    public void A_missing_interface_a_static_instance_class_and_a_missing_accessor_property_are_named()
    {
        var service = new ConvertedService("Achievement", "Character");
        var facts = FactsOf(new TypeFact(service.Instance, false, true, true, []));

        var problems = ConvertedServiceRules.MissingPieces(ListOf(service), facts);

        problems.Should().HaveCount(3);
        problems.Should().Contain(problem => problem.Contains("interface") && problem.Contains("does not exist"));
        problems.Should().Contain(problem => problem.Contains("must be an instance class"));
        problems.Should().Contain(problem => problem.Contains("GameServices has no static property Achievement of type") && problem.Contains("IAchievementService"));
    }

    [Test]
    public void A_misshapen_interface_an_instance_that_does_not_implement_it_and_an_accessor_property_of_another_type_are_named()
    {
        var service = new ConvertedService("Achievement", "Character");
        var facts = new AssemblyFacts(
            [
                new TypeFact(service.Interface, false, false, true, []),
                new TypeFact(service.Instance, false, false, true, []),
            ],
            [], [], [],
            [AccessorProperty("Achievement", new TypeKey("Elsewhere", "IOther"))]);

        var problems = ConvertedServiceRules.MissingPieces(ListOf(service), facts);

        problems.Should().HaveCount(3);
        problems.Should().Contain(problem => problem.Contains("must be an interface"));
        problems.Should().Contain(problem => problem.Contains("must implement"));
        problems.Should().Contain(problem => problem.Contains("has no static property Achievement of type"));
    }

    [Test]
    public void A_service_with_nothing_built_reports_its_interface_instance_and_accessor_property()
    {
        var problems = ConvertedServiceRules.MissingPieces(ListOf(new ConvertedService("Ghost", "Character")), FactsOf());

        problems.Should().HaveCount(3);
    }

    // ---- extras: further classes and parts

    private const string TrackerClass = "AchievementTracker";

    private static ConvertedService WithExtraClasses(params string[] classes) =>
        new("Achievement", "Character") { Extras = classes.Select(ExtraPiece.Class).ToArray() };

    private static ConvertedService WithParts(params string[] parts) =>
        new("Stat", "Character") { Extras = parts.Select(ExtraPiece.Part).ToArray() };

    // The types and accessor properties of a service whose main piece is built, so a test names only what it breaks.
    private static AssemblyFacts BuiltMain(ConvertedService service, IEnumerable<TypeFact> more, params StaticPropertyFact[] properties) =>
        new(
            new[]
            {
                new TypeFact(service.Interface, true, false, false, []),
                new TypeFact(service.Instance, false, false, true, [service.Interface.ToString()]),
            }.Concat(more).ToArray(),
            [], [], [],
            new[] { AccessorProperty(service.Name, service.Interface) }.Concat(properties).ToArray());

    [Test]
    public void The_extra_classes_of_a_service_are_read_from_the_list()
    {
        var list = ConvertedServiceList.Parse($$"""
            { "services": [
                { "name": "Achievement", "domain": "Character", "extras": [ { "class": "{{TrackerClass}}" } ] },
                { "name": "Stat", "domain": "Character" } ] }
            """);

        list.Problems.Should().BeEmpty();
        var achievement = list.Services[0];
        achievement.ExtraClasses.Should().Equal(TrackerClass);
        achievement.Parts.Should().BeEmpty();
        achievement.Pieces.Should().HaveCount(2);
        list.Services[1].ExtraClasses.Should().BeEmpty();
        list.Services[1].Pieces.Should().ContainSingle();
    }

    [Test]
    public void An_extra_class_is_named_by_convention_after_its_type()
    {
        var pieces = WithExtraClasses(TrackerClass).Pieces;

        pieces.Should().HaveCount(2);
        pieces[0].Label.Should().Be("Achievement");
        pieces[1].Label.Should().Be($"Achievement (class {TrackerClass})");
        pieces[1].AccessorProperty.Should().Be(TrackerClass);
        pieces[1].Interface.ToString().Should().Be("SWLOR.Game.Server.Domain.Character.IAchievementTrackerService");
        pieces[1].Instance.ToString().Should().Be("SWLOR.Game.Server.Domain.Character.AchievementTrackerService");
    }

    [Test]
    public void A_malformed_repeated_or_unsorted_class_name_is_a_problem_of_the_list()
    {
        var list = ConvertedServiceList.Parse("""
            { "services": [ { "name": "Achievement", "domain": "Character",
                "extras": [ { "class": "AchievementBeta" }, { "class": "AchievementAlpha" }, { "class": "AchievementAlpha" },
                            { "class": "bad name" }, { "class": "AchievementService.Gamma" }, { "class": "" } ] } ] }
            """);

        list.Problems.Should().Contain(problem => problem.Contains("The class AchievementAlpha of Achievement is out of order"));
        list.Problems.Should().Contain(problem => problem.Contains("Achievement lists the class AchievementAlpha twice"));
        list.Problems.Should().Contain(problem => problem.Contains("'bad name' (a class of Achievement) is not a plain class name"));
        list.Problems.Should().Contain(problem => problem.Contains("'AchievementService.Gamma' (a class of Achievement) is not a plain class name"));
        list.Problems.Should().Contain(problem => problem.Contains("'' (a class of Achievement)"));
        list.Services[0].ExtraClasses.Should().Equal("AchievementBeta", "AchievementAlpha");
    }

    [Test]
    public void An_extra_class_named_after_its_service_has_no_list_problems()
    {
        ConvertedServiceRules.ListProblems(ListOf(WithExtraClasses(TrackerClass, "AchievementZed")), DomainMap.Parse(MapJson)).Should().BeEmpty();
    }

    [Test]
    public void The_main_class_cannot_be_listed_as_an_extra()
    {
        var problems = ConvertedServiceRules.ListProblems(ListOf(WithExtraClasses("Achievement")), DomainMap.Parse(MapJson));

        problems.Should().ContainSingle().Which.Should().Contain("is the main class of Achievement");
    }

    [Test]
    public void An_extra_class_whose_service_class_would_not_count_as_the_service_is_reported()
    {
        var problems = ConvertedServiceRules.ListProblems(ListOf(WithExtraClasses("Tracker")), DomainMap.Parse(MapJson));

        problems.Should().HaveCount(2, "the interface and the instance class are both misplaced");
        problems.Should().OnlyContain(problem => problem.Contains("would count as no service, not as Achievement") && problem.Contains("starts with Achievement"));
    }

    [Test]
    public void An_extra_class_that_would_count_as_another_service_is_reported()
    {
        var problems = ConvertedServiceRules.ListProblems(ListOf(WithExtraClasses("StatTracker")), DomainMap.Parse(MapJson));

        problems.Should().HaveCount(2);
        problems.Should().OnlyContain(problem => problem.Contains("StatTracker") && problem.Contains("not as Achievement"));
    }

    [Test]
    public void A_service_with_every_piece_of_an_extra_class_passes()
    {
        var service = WithExtraClasses(TrackerClass);
        var extra = service.Pieces[1];
        var facts = BuiltMain(
            service,
            [new TypeFact(extra.Interface, true, false, false, []), new TypeFact(extra.Instance, false, false, true, [extra.Interface.ToString()])],
            AccessorProperty(TrackerClass, extra.Interface));

        ConvertedServiceRules.MissingPieces(ListOf(service), facts).Should().BeEmpty();
    }

    [Test]
    public void An_extra_class_that_does_not_exist_is_reported_with_its_pieces()
    {
        var service = WithExtraClasses(TrackerClass);
        var facts = BuiltMain(service, []);

        var problems = ConvertedServiceRules.MissingPieces(ListOf(service), facts);

        problems.Should().HaveCount(3);
        problems.Should().OnlyContain(problem => problem.StartsWith($"Achievement (class {TrackerClass}): "));
        problems.Should().Contain(problem => problem.Contains("GameServices has no static property AchievementTracker of type"));
        problems.Should().Contain(problem => problem.Contains("the interface") && problem.Contains("IAchievementTrackerService does not exist"));
        problems.Should().Contain(problem => problem.Contains("the instance class") && problem.Contains("AchievementTrackerService does not exist"));
    }

    [Test]
    public void A_misshapen_piece_of_an_extra_class_is_named()
    {
        var service = WithExtraClasses(TrackerClass);
        var extra = service.Pieces[1];
        var facts = BuiltMain(
            service,
            [new TypeFact(extra.Interface, false, false, true, []), new TypeFact(extra.Instance, false, false, true, [])],
            AccessorProperty(TrackerClass, extra.Interface));

        var problems = ConvertedServiceRules.MissingPieces(ListOf(service), facts);

        problems.Should().HaveCount(2);
        problems.Should().Contain(problem => problem.Contains("must be an interface") && problem.Contains("IAchievementTrackerService"));
        problems.Should().Contain(problem => problem.Contains("must implement") && problem.Contains("IAchievementTrackerService"));
    }

    [Test]
    public void The_pieces_of_an_extra_class_are_named_after_the_service_and_count_as_its_domain()
    {
        var map = DomainMap.Parse(MapJson);
        var service = WithExtraClasses(TrackerClass);

        foreach (var type in service.Pieces.SelectMany(pieces => new[] { pieces.Interface, pieces.Instance }))
        {
            UnitClassifier.ServiceOf(type, map).Should().Be(service.Name, type.ToString());
            UnitClassifier.Classify(type).Should().Be(new DependencyUnit(UnitKind.Domain, service.Domain), type.ToString());
        }
    }

    // ---- service classes in a domain namespace

    private static ConvertedServiceList AchievementWithTracker() => ListOf(WithExtraClasses(TrackerClass));

    private static TypeFact DomainType(string domain, string name, bool isInterface = false) =>
        new(new TypeKey(ConvertedService.ServerNamespace + ".Domain." + domain, name), isInterface, false, !isInterface, []);

    [Test]
    public void The_listed_pieces_and_the_support_types_beside_them_pass()
    {
        var facts = FactsOf(
            DomainType("Character", "IAchievementService", isInterface: true),
            DomainType("Character", "AchievementService"),
            DomainType("Character", "IAchievementTrackerService", isInterface: true),
            DomainType("Character", "AchievementTrackerService"),
            DomainType("Character", "AchievementNotification"),
            DomainType("Character", "HitResult"),
            DomainType("Character", "IUnrelatedRuntime", isInterface: true),
            DomainType("Character.Topic", "TopicBoard"),
            new TypeFact(new TypeKey(ConvertedService.ServerNamespace + ".Feature", "FeatureService"), false, false, true, []));

        ConvertedServiceRules.UnlistedServiceClasses(facts, AchievementWithTracker()).Should().BeEmpty();
    }

    [Test]
    public void A_service_class_or_interface_that_is_not_a_listed_piece_is_reported()
    {
        var facts = FactsOf(
            DomainType("Character", "IAchievementService", isInterface: true),
            DomainType("Character", "AchievementService"),
            DomainType("Character", "StatService"),
            DomainType("Character", "IStatService", isInterface: true),
            DomainType("Character", "HelperService"),
            DomainType("Character.Topic", "AchievementService"),
            DomainType("Combat", "AchievementTrackerService"));

        var problems = ConvertedServiceRules.UnlistedServiceClasses(facts, AchievementWithTracker());

        problems.Should().HaveCount(5);
        problems.Should().Contain(problem => problem.StartsWith("SWLOR.Game.Server.Domain.Character.StatService is named like a service class"));
        problems.Should().Contain(problem => problem.StartsWith("SWLOR.Game.Server.Domain.Character.IStatService is named like a service class"));
        problems.Should().Contain(problem => problem.StartsWith("SWLOR.Game.Server.Domain.Character.HelperService "));
        problems.Should().Contain(problem => problem.StartsWith("SWLOR.Game.Server.Domain.Character.Topic.AchievementService "), "a piece sits directly in its domain namespace, not a topic");
        problems.Should().Contain(problem => problem.StartsWith("SWLOR.Game.Server.Domain.Combat.AchievementTrackerService "), "a piece sits in the domain of its service");
        problems.Should().OnlyContain(problem => problem.Contains("not a piece of a converted service in ConvertedServices.json") && problem.Contains("without the Service suffix"));
    }

    [Test]
    public void The_parts_of_a_service_are_read_from_the_same_list_as_its_extra_classes()
    {
        var list = ConvertedServiceList.Parse($$"""
            { "services": [
                { "name": "Achievement", "domain": "Character", "extras": [ { "class": "{{TrackerClass}}" }, { "part": "AchievementUnlocks" } ] },
                { "name": "Stat", "domain": "Character", "extras": [ { "part": "StatCache" }, { "part": "StatEffects" } ] } ] }
            """);

        list.Problems.Should().BeEmpty();
        var mixed = list.Services[0];
        mixed.Extras.Should().Equal(ExtraPiece.Class(TrackerClass), ExtraPiece.Part("AchievementUnlocks"));
        mixed.ExtraClasses.Should().Equal(TrackerClass);
        mixed.Parts.Should().Equal("AchievementUnlocks");
        mixed.Pieces.Should().HaveCount(3);
        mixed.Pieces.Where(pieces => pieces.AccessorProperty is not null).Should().HaveCount(2, "a part has no GameServices property");
        list.Services[1].Parts.Should().Equal("StatCache", "StatEffects");
        list.Services[1].ExtraClasses.Should().BeEmpty();
    }

    [Test]
    public void A_part_is_named_by_convention_after_its_type_and_has_no_accessor_property()
    {
        var pieces = WithParts("StatCache").Pieces;

        pieces.Should().HaveCount(2);
        pieces[1].Label.Should().Be("Stat (part StatCache)");
        pieces[1].AccessorProperty.Should().BeNull();
        pieces[1].Interface.ToString().Should().Be("SWLOR.Game.Server.Domain.Character.IStatCacheService");
        pieces[1].Instance.ToString().Should().Be("SWLOR.Game.Server.Domain.Character.StatCacheService");
    }

    [Test]
    public void An_extra_that_is_neither_a_class_nor_a_part_or_both_is_a_problem_of_the_list()
    {
        var list = ConvertedServiceList.Parse("""
            { "services": [ { "name": "Stat", "domain": "Character",
                "extras": [ { }, { "class": "StatTracker", "part": "StatCache" }, "StatCache", { "facade": "StatService.StatTracker" }, { "part": "Stat Cache" }, { "part": "StatCache" }, { "part": "StatCache" }, { "part": "StatBeta" } ] } ] }
            """);

        list.Problems.Should().Contain(problem => problem.Contains("An extra of Stat must be an object with either a \"class\" or a \"part\""));
        list.Problems.Count(problem => problem.Contains("An extra of Stat must be an object")).Should().Be(4);
        list.Problems.Should().Contain(problem => problem.Contains("'Stat Cache' (a part of Stat) is not a plain class name"));
        list.Problems.Should().Contain(problem => problem.Contains("Stat lists the part StatCache twice"));
        list.Problems.Should().Contain(problem => problem.Contains("The part StatBeta of Stat is out of order"));
        list.Services[0].Parts.Should().Equal("StatCache", "StatBeta");
    }

    [Test]
    public void A_part_whose_classes_count_as_the_service_has_no_list_problems()
    {
        ConvertedServiceRules.ListProblems(ListOf(WithParts("StatCache", "StatEffects")), DomainMap.Parse(MapJson)).Should().BeEmpty();
    }

    [Test]
    public void A_part_that_the_cross_domain_guard_would_not_count_as_the_service_is_reported()
    {
        var problems = ConvertedServiceRules.ListProblems(ListOf(WithParts("Cache", "Statistics")), DomainMap.Parse(MapJson));

        problems.Should().HaveCount(4, "the interface and the instance class of each part are misplaced");
        problems.Should().OnlyContain(problem => problem.Contains("would count as") && problem.Contains("not as Stat") && problem.Contains("Name the part so that"));
    }

    [Test]
    public void A_part_cannot_be_the_main_class_of_its_service()
    {
        var problems = ConvertedServiceRules.ListProblems(ListOf(WithParts("Stat")), DomainMap.Parse(MapJson));

        problems.Should().ContainSingle().Which.Should().Contain("is the main class of Stat");
    }

    [Test]
    public void A_part_needs_its_interface_and_a_class_that_implements_it_but_no_accessor_property()
    {
        var service = WithParts("StatCache", "StatEffects");
        var cache = service.Pieces[1];
        var effects = service.Pieces[2];
        var facts = BuiltMain(
            service,
            [
                new TypeFact(cache.Interface, true, false, false, []),
                new TypeFact(cache.Instance, false, false, true, [cache.Interface.ToString()]),
                new TypeFact(effects.Interface, false, false, false, []),
                new TypeFact(effects.Instance, false, true, true, []),
            ]);

        var problems = ConvertedServiceRules.MissingPieces(ListOf(service), facts);

        problems.Should().NotContain(problem => problem.Contains("static property"), "a part has no GameServices property to look for");
        problems.Should().NotContain(problem => problem.StartsWith("Stat (part StatCache)"));
        problems.Should().Contain(problem => problem.StartsWith("Stat (part StatEffects)") && problem.Contains("IStatEffectsService must be an interface"));
        problems.Should().Contain(problem => problem.StartsWith("Stat (part StatEffects)") && problem.Contains("StatEffectsService must be an instance class"));
    }

    [Test]
    public void A_part_that_does_not_exist_is_reported_with_its_two_classes()
    {
        var service = WithParts("StatCache");
        var facts = BuiltMain(service, []);

        var problems = ConvertedServiceRules.MissingPieces(ListOf(service), facts);

        problems.Should().HaveCount(2);
        problems.Should().OnlyContain(problem => problem.StartsWith("Stat (part StatCache): "));
        problems.Should().Contain(problem => problem.Contains("the interface") && problem.Contains("IStatCacheService does not exist"));
        problems.Should().Contain(problem => problem.Contains("the instance class") && problem.Contains("StatCacheService does not exist"));
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
    public void Static_readonly_fields_of_deeply_immutable_types_are_accepted()
    {
        ConvertedServiceRules.StaticMutableFields(Facts, type => type == KeyOf(typeof(ImmutableTableHolder))).Should().BeEmpty();
    }

    [TestCase("Array")]
    [TestCase("List")]
    [TestCase("Dictionary")]
    [TestCase("Set")]
    [TestCase("ReadOnlyView")]
    [TestCase("FrozenOfMutable")]
    [TestCase("ImmutableOfArrays")]
    [TestCase("Settable")]
    [TestCase("Plain")]
    [TestCase("WithArray")]
    [TestCase("Anything")]
    [TestCase("TupleOfList")]
    [TestCase("Watch")]
    [TestCase("NotReadonly")]
    [TestCase("FrozenOfEngineClass")]
    public void Static_readonly_fields_that_are_or_may_be_mutable_are_still_reported(string field)
    {
        var problems = ConvertedServiceRules.StaticMutableFields(Facts, type => type == KeyOf(typeof(MutableTableHolder)));

        problems.Should().Contain(problem => problem.StartsWith($"{typeof(MutableTableHolder).Namespace}.MutableTableHolder.{field} "));
    }

    [Test]
    public void Every_field_of_the_mutable_table_fixture_is_reported()
    {
        ConvertedServiceRules.StaticMutableFields(Facts, type => type == KeyOf(typeof(MutableTableHolder))).Should().HaveCount(15);
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
    public void A_handler_on_the_instance_class_passes_and_one_anywhere_else_is_reported()
    {
        var instance = KeyOf(typeof(HandlerOnInstance));

        var problems = ConvertedServiceRules.HandlersOffTheInstance(
            Facts,
            type => type == instance || type == KeyOf(typeof(HandlerOnStaticClass)) ? new[] { instance } : null);

        problems.Should().ContainSingle().Which.Should().Contain("HandlerOnStaticClass.Handle").And.Contain("its instance class").And.Contain("HandlerRenames.txt");
        ConvertedServiceRules.HandlersOffTheInstance(Facts, _ => null).Should().BeEmpty();
    }

    [Test]
    public void A_service_with_several_instance_classes_may_keep_its_handlers_on_any_of_them()
    {
        var instance = KeyOf(typeof(HandlerOnInstance));
        var part = new TypeKey("Elsewhere", "PartService");

        var problems = ConvertedServiceRules.HandlersOffTheInstance(
            Facts,
            type => type == instance || type == KeyOf(typeof(HandlerOnStaticClass)) ? new[] { part, instance } : null);

        problems.Should().ContainSingle().Which.Should().Contain("HandlerOnStaticClass.Handle").And.Contain("one of its instance classes").And.Contain("PartService");
    }

    // ---- no in-progress list (P2-CB10 removed it when Combat was listed)

    [Test]
    public void A_list_of_services_in_progress_is_a_problem()
    {
        var list = ConvertedServiceList.Parse("""
            { "services": [ { "name": "CombatPoint", "domain": "Combat" } ], "inProgress": [ { "name": "Combat", "domain": "Combat" } ] }
            """);

        list.Services.Select(service => service.Name).Should().Equal("CombatPoint");
        list.Problems.Should().ContainSingle().Which.Should().Contain("The inProgress list is gone");
        ConvertedServiceList.Parse("""{ "services": [] }""").Problems.Should().BeEmpty();
    }

    // ---- service location

    private static IReadOnlyList<string> Locate()
    {
        return ServiceLocationRule.Find(
            Graph,
            Facts,
            typeof(ContainerOwner).Namespace!,
            KeyOf(typeof(FixtureAccessor)));
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
    [TestCase(typeof(StaticStateHolder))]
    public void Types_that_may_resolve_or_that_never_do_are_not_reported(Type allowed)
    {
        Locate().Should().NotContain(problem => problem.StartsWith(KeyOf(allowed).ToString() + " "));
    }

    // ---- the GameServices accessor

    private static StaticPropertyFact AccessorProperty(string name, TypeKey type) => new(ConvertedService.Accessor, name, type.ToString());

    [Test]
    public void The_facadeRemoved_flag_is_a_problem_of_the_list()
    {
        var list = ConvertedServiceList.Parse("""
            { "services": [ { "name": "Achievement", "domain": "Character", "facadeRemoved": true }, { "name": "Stat", "domain": "Character" } ] }
            """);

        list.Problems.Should().ContainSingle().Which.Should().Contain("\"facadeRemoved\" flag of Achievement is gone");
        ConvertedServiceList.Parse("""{ "services": [ { "name": "Achievement", "domain": "Character" } ] }""").Problems.Should().BeEmpty();
    }

    [Test]
    public void The_scanner_reads_static_properties_with_their_types()
    {
        var accessor = KeyOf(typeof(FixtureServices));

        Facts.StaticProperties.Should().Contain(new StaticPropertyFact(accessor, "Fixture", typeof(IFixtureContract).FullName!));
        Facts.StaticProperties.Should().Contain(new StaticPropertyFact(accessor, "Name", "System.String"));
        Facts.StaticProperties.Should().NotContain(property => property.Owner == KeyOf(typeof(CleanInstanceService)), "an instance property is not static");
    }

    [Test]
    public void A_class_the_container_builds_that_uses_the_accessor_is_reported()
    {
        var accessor = KeyOf(typeof(FixtureServices));

        var problems = ConvertedServiceRules.RegisteredClassesUseTheAccessor(Graph, [KeyOf(typeof(ServicesUser)), KeyOf(typeof(CleanInstanceService))], accessor);

        problems.Should().ContainSingle().Which.Should().StartWith(KeyOf(typeof(ServicesUser)) + " uses " + accessor).And.Contain("constructor");
    }

    private static AssemblyFacts FactsOf(params TypeFact[] types) => new(types, [], [], []);

    private static ConvertedServiceList ListOf(params ConvertedService[] services) =>
        ConvertedServiceList.Parse("{ \"services\": [" + string.Join(",", services.Select(s =>
            $"{{ \"name\": \"{s.Name}\", \"domain\": \"{s.Domain}\", \"extras\": [{string.Join(",", s.Extras.Select(extra => $"{{ \"{extra.Kind}\": \"{extra.Name}\" }}"))}]" +
            " }")) + "] }");

    private static TypeKey KeyOf(Type type) => new(type.Namespace ?? string.Empty, type.Name);
}
