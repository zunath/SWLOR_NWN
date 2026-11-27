using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Debt;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// The source measures of the debt guards (<see cref="Phase5DebtTests"/>) on small in-memory projects: what counts
/// as a use of the static lookup, how types and methods are measured, which constructor parameters are services.
/// </summary>
[TestFixture]
public class DebtSourceMeasureTests
{
    private const string Server = SourceSnapshot.ServerProjectName;

    /// <summary>A stand-in for the server's composition root: the lookup, two services and their registrations.</summary>
    private const string Composition = """
        using System;
        using Microsoft.Extensions.DependencyInjection;

        namespace SWLOR.Game.Server.Composition
        {
            public interface IStatService { int Level(uint creature); }
            public interface IBankService { void Deposit(); }
            public sealed partial class StatService : IStatService { public int Level(uint creature) => 1; }
            public sealed class BankService : IBankService { public void Deposit() { } }

            public static partial class GameServices
            {
                public static IStatService Stat => FacadeAccessor.Resolve<IStatService>();
                public static IBankService Bank => FacadeAccessor.Resolve<IBankService>();
            }

            internal static class FacadeAccessor
            {
                internal static T Resolve<T>() where T : class => null!;
            }

            internal static class ServiceRegistrations
            {
                internal static void Register(IServiceCollection services)
                {
                    services.AddSingleton<IStatService, StatService>();
                    services.AddSingleton<IBankService, BankService>();
                }
            }
        }
        """;

    /// <summary>A domain map with an order: Platform, Foundation, Character, Combat, Gameplay, bottom to top.</summary>
    private const string OrderedMap = """
        {
          "platformDomain": "Platform", "featureTypeDomain": "Gameplay", "unrestrictedTargetDomains": ["Platform"],
          "wiringPlatformAreas": ["Composition", "NativeSinks"], "contentRuleExemptAreas": ["Composition"],
          "order": ["Platform", "Foundation", "Character", "Combat", "Gameplay"], "contentBand": "aboveAllLogic",
          "domains": {
            "Foundation": { "services": [], "featureFolders": [] },
            "Character": { "services": [], "featureFolders": ["StatusEffectDefinition"] },
            "Combat": { "services": [], "featureFolders": [] },
            "Gameplay": { "services": [], "featureFolders": [] }
          }
        }
        """;

    private static SourceSnapshot Snapshot(params (string Project, string File, string Text)[] files)
    {
        var projects = files
            .Append((Project: Server, File: "SWLOR.Game.Server/Composition/Composition.cs", Text: Composition))
            .GroupBy(file => file.Project)
            .ToDictionary(
                project => project.Key,
                project => (IReadOnlyDictionary<string, string>)project.ToDictionary(file => file.File, file => file.Text));
        return SourceSnapshot.FromText(projects);
    }

    private static string CommentLines(int count) => string.Concat(Enumerable.Range(0, count).Select(index => $"        // line {index}\n"));

    [Test]
    public void A_lookup_use_is_counted_where_the_compiler_binds_it_and_never_in_comments_or_strings()
    {
        var snapshot = Snapshot((Server, "SWLOR.Game.Server/Feature/Content.cs", """
            using static SWLOR.Game.Server.Composition.GameServices;

            namespace SWLOR.Game.Server.Feature
            {
                /// <summary>Calls <see cref="SWLOR.Game.Server.Composition.GameServices.Stat"/>.</summary>
                public static class Content
                {
                    // GameServices.Stat.Level(1) in a comment does not count.
                    private const string Text = "GameServices.Stat";

                    public static int Run() => Stat.Level(1) + SWLOR.Game.Server.Composition.GameServices.Bank.GetHashCode();

                    public static System.Type Kind() => typeof(SWLOR.Game.Server.Composition.GameServices);
                }
            }
            """));

        var sites = LookupScanner.Scan(snapshot);

        sites.Select(site => site.Member).Should().BeEquivalentTo("using GameServices", "GameServices.Stat", "GameServices.Bank", "GameServices");
        sites.Single(site => site.Member == "GameServices.Stat").Service.Should().Be("SWLOR.Game.Server.Composition.IStatService");
        sites.Should().OnlyContain(site => site.File == "SWLOR.Game.Server/Feature/Content.cs", "the lookup's own types are not uses of it");
        sites.Where(site => !site.IsImport).Should().OnlyContain(site => site.IsStaticContext && site.Type == "SWLOR.Game.Server.Feature.Content");
    }

    [Test]
    public void A_member_that_shadows_a_lookup_property_is_not_a_use()
    {
        var snapshot = Snapshot((Server, "SWLOR.Game.Server/Feature/Shadowed.cs", """
            using static SWLOR.Game.Server.Composition.GameServices;

            namespace SWLOR.Game.Server.Feature
            {
                public class Shadowed
                {
                    private int Stat => 3;

                    public int Run() => Stat + Bank.GetHashCode();
                }
            }
            """));

        var sites = LookupScanner.Scan(snapshot);

        sites.Select(site => site.Member).Should().BeEquivalentTo("using GameServices", "GameServices.Bank");
        sites.Single(site => site.Member == "GameServices.Bank").IsStaticContext.Should().BeFalse();
    }

    [Test]
    public void A_project_wide_global_using_makes_every_file_of_that_project_a_candidate()
    {
        var snapshot = Snapshot(
            ("SWLOR.Game.Server.EngineTests", "SWLOR.Game.Server.EngineTests/GlobalUsings.cs", "global using static SWLOR.Game.Server.Composition.GameServices;"),
            ("SWLOR.Game.Server.EngineTests", "SWLOR.Game.Server.EngineTests/LevelTests.cs", """
                namespace SWLOR.Game.Server.EngineTests
                {
                    public class LevelTests
                    {
                        public int Level() => Stat.Level(1);
                    }
                }
                """),
            ("SWLOR.Game.Server.EngineTests", "SWLOR.Game.Server.EngineTests/Unrelated.cs", "namespace SWLOR.Game.Server.EngineTests { public class Unrelated { } }"));

        var byFile = LookupScanner.Scan(snapshot).GroupBy(site => site.File).ToDictionary(group => group.Key, group => group.Count());

        byFile.Should().BeEquivalentTo(new Dictionary<string, int>
        {
            ["SWLOR.Game.Server.EngineTests/GlobalUsings.cs"] = 1,
            ["SWLOR.Game.Server.EngineTests/LevelTests.cs"] = 1
        });
    }

    [Test]
    public void Another_composition_type_that_resolves_through_the_accessor_is_a_use()
    {
        var snapshot = Snapshot((Server, "SWLOR.Game.Server/Composition/HookAccess.cs", """
            namespace SWLOR.Game.Server.Composition
            {
                public static class HookAccess
                {
                    public static IStatService Hooks => FacadeAccessor.Resolve<IStatService>();
                }
            }
            """));

        var site = LookupScanner.Scan(snapshot).Should().ContainSingle().Subject;

        site.Member.Should().Be("FacadeAccessor.Resolve");
        site.Service.Should().Be("SWLOR.Game.Server.Composition.IStatService");
    }

    [Test]
    public void A_type_is_measured_by_its_files_all_partial_files_together()
    {
        var snapshot = Snapshot(
            (Server, "SWLOR.Game.Server/Domain/Big.cs", "namespace SWLOR.Game.Server.Domain\n{\n    public partial class Big\n    {\n" + CommentLines(10) + "    }\n}\n"),
            (Server, "SWLOR.Game.Server/Domain/Big.Part.cs", "namespace SWLOR.Game.Server.Domain\n{\n    public partial class Big\n    {\n" + CommentLines(5) + "    }\n}"),
            (Server, "SWLOR.Game.Server/Domain/Two.cs", "namespace SWLOR.Game.Server.Domain\n{\n    // about the file\n    public class First\n    {\n    }\n\n    public class Second\n    {\n" + CommentLines(3) + "    }\n}\n"));

        var types = SizeScanner.Types(snapshot.Server, ExemptionList.Empty).ToDictionary(type => type.Type);

        types["SWLOR.Game.Server.Domain.Big"].Lines.Should().Be(16 + 11, "a file that declares one type counts all its lines, and a final line break starts no line");
        types["SWLOR.Game.Server.Domain.Big"].Files.Should().Equal("SWLOR.Game.Server/Domain/Big.Part.cs", "SWLOR.Game.Server/Domain/Big.cs");
        types["SWLOR.Game.Server.Domain.First"].Lines.Should().Be(3, "a file that declares several types counts each declaration's span");
        types["SWLOR.Game.Server.Domain.Second"].Lines.Should().Be(6);
    }

    [Test]
    public void Members_of_a_gui_window_definition_get_the_layout_limit_and_overloads_are_told_apart()
    {
        var snapshot = Snapshot(
            (Server, "SWLOR.Game.Server/Feature/Window.cs", """
                namespace SWLOR.Game.Server.Feature
                {
                    public interface IGuiWindowDefinition { void BuildWindow(); }

                    public class StoreWindow : IGuiWindowDefinition
                    {
                        public void BuildWindow()
                        {
                        }
                    }

                    public class Logic
                    {
                        public void Run(int value) { }
                        public void Run(string value, uint target) { }
                        public int Value { get; } = 3;
                        public Logic() { }
                    }
                }
                """));

        var members = SizeScanner.Members(snapshot.Server, ExemptionList.Empty, methodLimit: 150, layoutLimit: 400).ToDictionary(member => member.Key);

        members["SWLOR.Game.Server.Feature.StoreWindow.BuildWindow"].Limit.Should().Be(400);
        members["SWLOR.Game.Server.Feature.StoreWindow.BuildWindow"].Lines.Should().Be(3);
        members.Keys.Should().Contain(["SWLOR.Game.Server.Feature.Logic.Run(int)", "SWLOR.Game.Server.Feature.Logic.Run(string, uint)", "SWLOR.Game.Server.Feature.Logic.Value", "SWLOR.Game.Server.Feature.Logic..ctor"]);
        members["SWLOR.Game.Server.Feature.Logic.Value"].Limit.Should().Be(150);
    }

    [Test]
    public void Exemptions_take_files_types_and_members_out_and_an_exempt_member_out_of_its_type()
    {
        var exemptions = ExemptionList.Parse("""
            {
              "exemptions": [
                { "match": "folder", "value": "SWLOR.Game.Server/Feature/Data/", "measures": ["typeSize", "methodSize"], "category": "content data", "reason": "data" },
                { "match": "generatedHeader", "value": "<auto-generated", "measures": ["typeSize"], "category": "generated code", "reason": "generated" },
                { "match": "type", "value": "SWLOR.Game.Server.Domain.Kind", "measures": ["typeSize"], "category": "attribute-row enum", "reason": "rows" },
                { "match": "member", "value": "SWLOR.Game.Server.Domain.Catalog.Songs", "measures": ["typeSize", "methodSize"], "category": "data catalog member", "reason": "list" }
              ],
              "approvedStaticEntryPoints": []
            }
            """);
        var snapshot = Snapshot(
            (Server, "SWLOR.Game.Server/Feature/Data/Recipes.cs", "namespace SWLOR.Game.Server.Feature.Data { public class Recipes { public void Tier1() { } } }"),
            (Server, "SWLOR.Game.Server/Domain/Generated.cs", "// <auto-generated by a tool>\nnamespace SWLOR.Game.Server.Domain { public partial class Board { } }"),
            (Server, "SWLOR.Game.Server/Domain/Board.cs", "namespace SWLOR.Game.Server.Domain\n{\n    public partial class Board\n    {\n    }\n}"),
            (Server, "SWLOR.Game.Server/Domain/Kind.cs", "namespace SWLOR.Game.Server.Domain { public enum Kind { A } }"),
            (Server, "SWLOR.Game.Server/Domain/Catalog.cs", "namespace SWLOR.Game.Server.Domain\n{\n    public class Catalog\n    {\n        public int[] Songs { get; } =\n        {\n            1,\n        };\n    }\n}"));

        exemptions.Problems.Should().BeEmpty();
        exemptions.Unmatched(snapshot.Server).Should().BeEmpty();

        var types = SizeScanner.Types(snapshot.Server, exemptions).ToDictionary(type => type.Type);
        types["SWLOR.Game.Server.Feature.Data.Recipes"].Exemption!.Category.Should().Be("content data");
        types["SWLOR.Game.Server.Domain.Board"].Exemption.Should().BeNull("only the generated partial is exempt");
        types["SWLOR.Game.Server.Domain.Board"].Lines.Should().Be(6, "the generated partial's lines are left out");
        types["SWLOR.Game.Server.Domain.Kind"].Exemption!.Category.Should().Be("attribute-row enum");
        types["SWLOR.Game.Server.Domain.Catalog"].Lines.Should().Be(10 - 4, "the exempt member's lines are taken out of its type");

        var members = SizeScanner.Members(snapshot.Server, exemptions, 150, 400).ToDictionary(member => member.Key);
        members["SWLOR.Game.Server.Domain.Catalog.Songs"].Exemption!.Category.Should().Be("data catalog member");
        members["SWLOR.Game.Server.Feature.Data.Recipes.Tier1"].Exemption!.Category.Should().Be("content data");
    }

    [Test]
    public void An_exemption_that_names_nothing_or_lacks_a_reason_is_a_problem()
    {
        var exemptions = ExemptionList.Parse("""
            {
              "exemptions": [
                { "match": "type", "value": "SWLOR.Game.Server.Domain.Gone", "measures": ["typeSize"], "category": "fluent builder", "reason": "builder" },
                { "match": "folder", "value": "SWLOR.Game.Server/Feature/NoSlash", "measures": ["typeSize"], "category": "content data", "reason": "" },
                { "match": "pattern", "value": "x", "measures": ["lines"], "category": "c", "reason": "r" }
              ],
              "approvedStaticEntryPoints": [ { "type": "SWLOR.Game.Server.Core.Missing", "reason": "approved" } ]
            }
            """);
        var snapshot = Snapshot();

        exemptions.Problems.Should().Contain(problem => problem.Contains("ends with '/'"))
            .And.Contain(problem => problem.Contains("needs a category and a reason"))
            .And.Contain(problem => problem.Contains("match must be one of"))
            .And.Contain(problem => problem.Contains("measures must be some of"));
        exemptions.Unmatched(snapshot.Server).Should().Contain(problem => problem.Contains("SWLOR.Game.Server.Domain.Gone"))
            .And.Contain(problem => problem.Contains("SWLOR.Game.Server.Core.Missing"));
    }

    [Test]
    public void Constructor_services_are_registered_types_direct_or_deferred_and_never_values_or_the_type_itself()
    {
        var snapshot = Snapshot(
            (Server, "SWLOR.Game.Server/Domain/Worker.cs", """
                using System;
                using SWLOR.Game.Server.Composition;

                namespace SWLOR.Game.Server.Domain
                {
                    public sealed record Settings(int Value);

                    public sealed class Worker(IBankService primary)
                    {
                        public Worker(IStatService stat, Lazy<IBankService> bank, Func<IStatService> later, Lazy<int> number, Settings settings, int count)
                            : this(bank.Value)
                        {
                        }
                    }
                }
                """),
            (Server, "SWLOR.Game.Server/Composition/StatService.Tracker.cs", """
                namespace SWLOR.Game.Server.Composition
                {
                    public sealed partial class StatService
                    {
                        private sealed class Tracker
                        {
                            public Tracker(StatService owner, IBankService bank) { }
                        }
                    }
                }
                """));

        var serviceTypes = ServiceScanner.ServiceTypes(snapshot);
        var services = ServiceScanner.ConstructorServices(snapshot, serviceTypes);

        serviceTypes.Should().BeEquivalentTo(
            "SWLOR.Game.Server.Composition.IStatService", "SWLOR.Game.Server.Composition.StatService",
            "SWLOR.Game.Server.Composition.IBankService", "SWLOR.Game.Server.Composition.BankService");
        services.Select(service => (service.Type, service.Service, service.Wrapper)).Should().BeEquivalentTo(new[]
        {
            ("SWLOR.Game.Server.Domain.Worker", "SWLOR.Game.Server.Composition.IBankService", string.Empty),
            ("SWLOR.Game.Server.Domain.Worker", "SWLOR.Game.Server.Composition.IStatService", string.Empty),
            ("SWLOR.Game.Server.Domain.Worker", "SWLOR.Game.Server.Composition.IBankService", "Lazy"),
            ("SWLOR.Game.Server.Domain.Worker", "SWLOR.Game.Server.Composition.IStatService", "Func"),
            ("SWLOR.Game.Server.Composition.StatService", "SWLOR.Game.Server.Composition.IBankService", string.Empty)
        }, "values, records and the service a nested helper belongs to are not services");
    }

    [Test]
    public void Services_per_type_unites_constructor_and_lookup_services_and_lists_only_types_over_the_limit()
    {
        var many = Enumerable.Range(0, DebtLimits.ServicesPerType).Select(index => new ConstructorService("Big", "Big.cs", $"IService{index}", string.Empty)).ToList();
        var constructor = many.Append(new ConstructorService("Small", "Small.cs", "IService0", "Lazy")).ToList();
        var lookups = new[]
        {
            new LookupSite(Server, "Big.cs", "Big", "GameServices.Service0", "IService0", false),
            new LookupSite(Server, "Big.cs", "Big", "GameServices.Extra", "IExtra", true)
        };

        var measure = DebtMeasures.ServicesPerType(lookups, constructor, Snapshot().Server, ExemptionList.Empty);

        measure.Entries.Should().ContainSingle().Which.Should().Be(new DebtEntry("Big", DebtLimits.ServicesPerType + 1, $"constructor {DebtLimits.ServicesPerType}, lookup 2"));
    }

    [Test]
    public void Deferred_resolution_lists_lazy_parameters_per_file_and_direction_and_IServiceProvider_outside_composition()
    {
        var snapshot = Snapshot(
            (Server, "SWLOR.Game.Server/Domain/Locator.cs", """
                using System;
                namespace SWLOR.Game.Server.Domain
                {
                    public sealed class Locator
                    {
                        public Locator(IServiceProvider provider) { }
                    }
                }
                """),
            (Server, "SWLOR.Game.Server/Composition/Root.cs", "namespace SWLOR.Game.Server.Composition { public sealed class Root { public Root(System.IServiceProvider provider) { } } }"));
        const string Character = "SWLOR.Game.Server.Domain.Character.";
        const string Combat = "SWLOR.Game.Server.Domain.Combat.";

        var uses = ServiceScanner.ServiceProviderUses(snapshot, DebtMeasures.CompositionFolder);
        var measure = DebtMeasures.DeferredResolution(
            [
                new ConstructorService(Character + "StatService", "Stat.cs", Combat + "ICombatService", "Lazy"),
                new ConstructorService(Character + "StatService", "Stat.cs", Combat + "IEnmityService", "Func"),
                new ConstructorService(Combat + "CombatService", "Combat.cs", Character + "IStatService", "Lazy"),
                new ConstructorService(Combat + "CombatService", "Combat.cs", Combat + "IEnmityService", "Lazy"),
                new ConstructorService(Combat + "CombatService", "Combat.cs", Character + "IStatService", string.Empty)
            ],
            uses,
            SWLOR.Architecture.Tests.Domains.DomainMap.Parse(OrderedMap));

        uses.Should().Equal(("SWLOR.Game.Server/Domain/Locator.cs", "SWLOR.Game.Server.Domain.Locator"));
        measure.Entries.Select(entry => (entry.Key, entry.Count)).Should().BeEquivalentTo(new[]
        {
            ("Stat.cs -> Combat (up)", 2),
            ("Combat.cs -> Character (down)", 1),
            ("Combat.cs -> Combat (inside)", 1),
            ("SWLOR.Game.Server/Domain/Locator.cs (IServiceProvider)", 1)
        }, "a direct parameter is not deferred, and the order puts Character below Combat");
        measure.Entries.Single(entry => entry.Key == "Stat.cs -> Combat (up)").Detail.Should().Be("Character -> Combat: Func 1, Lazy 1 (ICombatService, IEnmityService)");
        measure.Summary.Should().Contain("By direction in the domain order: up 2, down 1, inside one domain 1.").And.Contain("Up: Character -> Combat 2.");
    }

    [Test]
    public void Logic_that_names_content_is_listed_per_file_and_content_unit_the_sinks_included_and_the_composition_root_is_not()
    {
        var map = SWLOR.Architecture.Tests.Domains.DomainMap.Parse("""
            {
              "platformDomain": "Platform", "featureTypeDomain": "Gameplay", "unrestrictedTargetDomains": ["Platform"],
              "wiringPlatformAreas": ["Composition", "NativeSinks"], "contentRuleExemptAreas": ["Composition"],
              "domains": {
                "Combat": { "services": [], "featureFolders": [] },
                "Character": { "services": [], "featureFolders": ["StatusEffectDefinition"] },
                "Gameplay": { "services": [], "featureFolders": [] }
              }
            }
            """);
        var snapshot = Snapshot(
            (Server, "SWLOR.Game.Server/Feature/StatusEffectDefinition/Effects.cs", """
                namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
                {
                    public sealed class StunnedStatusEffect { public static int Rank => 1; }
                    public enum EffectKind { Stun }
                    public sealed class Uses { public object Logic() => new SWLOR.Game.Server.Domain.Combat.CombatService(); }
                }
                """),
            (Server, "SWLOR.Game.Server/Feature/UsePerkFeat.cs", "namespace SWLOR.Game.Server.Feature { public static class UsePerkFeat { public static void Use() { } } }"),
            (Server, "SWLOR.Game.Server/Domain/Combat/CombatService.cs", """
                using SWLOR.Game.Server.Feature.StatusEffectDefinition;
                namespace SWLOR.Game.Server.Domain.Combat
                {
                    /// <summary>Applies <see cref="SWLOR.Game.Server.Feature.UsePerkFeat"/> in a comment only.</summary>
                    public sealed class CombatService
                    {
                        public int Apply() => StunnedStatusEffect.Rank + (int)EffectKind.Stun;
                        public string Name() => nameof(StunnedStatusEffect);
                    }
                }
                """),
            (Server, "SWLOR.Game.Server/Domain/Character/StatService.cs", """
                namespace SWLOR.Game.Server.Domain.Character
                {
                    public sealed class StatService { public System.Type Kind() => typeof(SWLOR.Game.Server.Feature.StatusEffectDefinition.StunnedStatusEffect); }
                }
                """),
            (Server, "SWLOR.Game.Server/Native/GetDamageRoll.cs", "namespace SWLOR.Game.Server.Native { public static class GetDamageRoll { public static void Roll() => SWLOR.Game.Server.Feature.UsePerkFeat.Use(); } }"),
            (Server, "SWLOR.Game.Server/NativeSinks/AttackSink.cs", "namespace SWLOR.Game.Server.NativeSinks { public sealed class AttackSink { public void Attack() => SWLOR.Game.Server.Feature.UsePerkFeat.Use(); } }"),
            (Server, "SWLOR.Game.Server/Composition/Wiring.cs", "namespace SWLOR.Game.Server.Composition { public static class Wiring { public static object Build() => new SWLOR.Game.Server.Feature.StatusEffectDefinition.StunnedStatusEffect(); } }"));

        var references = LogicToContentScanner.Scan(snapshot, map);

        references.Select(reference => (reference.Key, reference.Types.Count, reference.IsSameDomain)).Should().BeEquivalentTo(new[]
        {
            ("SWLOR.Game.Server/Domain/Combat/CombatService.cs -> Feature/StatusEffectDefinition", 2, false),
            ("SWLOR.Game.Server/Domain/Character/StatService.cs -> Feature/StatusEffectDefinition", 1, true),
            ("SWLOR.Game.Server/Native/GetDamageRoll.cs -> Feature.UsePerkFeat", 1, false),
            ("SWLOR.Game.Server/NativeSinks/AttackSink.cs -> Feature.UsePerkFeat", 1, false)
        }, "a constant and nameof name content too, and the sinks are wiring under the no-content rule; comments, content naming " +
           "logic and the composition root do not count");
    }

    [Test]
    public void A_static_field_is_runtime_state_only_when_code_writes_it_after_its_initialization()
    {
        var snapshot = Snapshot((Server, "SWLOR.Game.Server/Feature/Holder.cs", """
            using System.Collections.Generic;
            namespace SWLOR.Game.Server.Feature
            {
                public static class Holder
                {
                    private static readonly Dictionary<int, string> Table = new() { [1] = "one" };
                    private static readonly List<int> Seen = new();
                    private static readonly Dictionary<int, int> Counts = new();
                    private static string _cached;
                    private static readonly List<int> Loaded = new();

                    static Holder()
                    {
                        Loaded.Add(1);
                    }

                    public static string Name(int id) => Table[id];
                    public static void Mark(int id) => Seen.Add(id);
                    public static void Count(int id) => Counts[id] = 1;
                    public static string Cached() => _cached ??= "value";
                }
            }
            """));
        var fields = new[] { "Table", "Seen", "Counts", "_cached", "Loaded" }
            .Select(name => new SWLOR.Architecture.Tests.Scanning.StaticFieldFact(new SWLOR.Architecture.Tests.Scanning.TypeKey("SWLOR.Game.Server.Feature", "Holder"), "Holder", name, false, name != "_cached"))
            .ToList();

        var written = StaticFieldWriteScanner.Written(snapshot, fields);

        written.Select(field => field.Name).Should().BeEquivalentTo("Seen", "Counts", "_cached");
    }
}
