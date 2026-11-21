using FluentAssertions;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Debt;
using SWLOR.Architecture.Tests.Rules;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// The guards of content injection (MaintainabilityRefactorContentInjection.md, "Guards"; <see cref="ContentInjectionRules"/>): first
/// on a small in-memory server that breaks each rule, then on the real server, where the findings must equal the reviewed exceptions
/// below. An exception names its type and what it does, never a line, and says when it goes.
/// </summary>
[TestFixture]
public class ContentInjectionRulesTests
{
    private const string Server = SourceSnapshot.ServerProjectName;

    private static readonly Lazy<ContentInjectionRules> Real = new(() => ArchitectureProject.ContentInjection);

    private static readonly Lazy<ContentInjectionRules> Sample = new(() => new ContentInjectionRules(SampleSnapshot()));

    /// <summary>The composition root of the sample server: two services, the factory and one content singleton.</summary>
    private const string Composition = """
        using System;
        using Microsoft.Extensions.DependencyInjection;
        using SWLOR.Game.Server.Core;
        using SWLOR.Game.Server.Domain.Character;

        namespace SWLOR.Game.Server.Core
        {
            public interface IContentFactory { T Create<T>() where T : class; T Create<T, A1>(A1 a1) where T : class; }
            public class NWNEventHandler : Attribute { public NWNEventHandler(string script) { } }
        }

        namespace SWLOR.Game.Server.Domain.Character
        {
            public interface IStatService { int Level(uint creature); }
            public interface IBankService { void Deposit(); }
            public sealed class StatService : IStatService { public int Level(uint creature) => 1; }
            public sealed class BankService : IBankService { public void Deposit() { } }
        }

        namespace SWLOR.Game.Server.Composition
        {
            public sealed class ContentFactory : IContentFactory
            {
                public T Create<T>() where T : class => null!;
                public T Create<T, A1>(A1 a1) where T : class => null!;
            }

            public static partial class GameServices
            {
                public static IStatService Stat => null!;
            }

            public sealed class ContainerHost
            {
            }

            public static class CompositionRoot
            {
                public static ContainerHost ServerHost { get; } = new ContainerHost();
            }

            internal static class ServiceRegistrations
            {
                internal static void Register(IServiceCollection services)
                {
                    services.AddSingleton<IStatService, StatService>();
                    services.AddSingleton<IBankService, BankService>();
                    services.AddSingleton<IContentFactory, ContentFactory>();
                    ContentRegistrations.Register(services);
                }
            }

            internal static class ContentRegistrations
            {
                internal static void Register(IServiceCollection services)
                {
                    services.AddSingleton<SWLOR.Game.Server.Feature.Scripts>();
                    services.AddSingleton<SWLOR.Game.Server.Feature.Loop>();
                    services.AddSingleton<SWLOR.Game.Server.Feature.Back>();
                }
            }
        }
        """;

    private const string Content = """
        using SWLOR.Game.Server.Composition;
        using SWLOR.Game.Server.Core;
        using SWLOR.Game.Server.Domain.Character;

        namespace SWLOR.Game.Server.Feature
        {
            public class Scripts
            {
                public Scripts(IStatService stat) { Stat = stat; }
                private IStatService Stat { get; }
                [NWNEventHandler("x")] public void Handle() { }
            }

            public class Loop { public Loop(Back back) { } }
            public class Back { public Back(Loop loop) { } }

            public class Unregistered
            {
                public Unregistered(IStatService stat) { Stat = stat; }
                private IStatService Stat { get; }
                [NWNEventHandler("y")] public void Handle() { }
            }

            public class Venom
            {
                public Venom(IStatService stat, int potency) { Stat = stat; }
                private IStatService Stat { get; }
                public Venom Clone() => new Venom(Stat, 1);
            }

            public class Shapes
            {
                public IStatService Stat { get; }
                private IBankService Money { get; }
                private IStatService _stat;
                public Shapes(IStatService stat, IBankService bank) { Stat = stat; Money = bank; _stat = stat; }
                public void Inject(IStatService stat) { _stat = stat; }
            }

            public class Creator
            {
                private IContentFactory Content { get; }
                public Creator(IContentFactory content) { Content = content; }
                public object Bad() => new Venom(GameServices.Stat, 2);
                public object Good() => Content.Create<Venom, int>(2);
                public object Registered() => Content.Create<Scripts>();
                public object Service() => new StatService();
            }

            public static class Tables
            {
                public static readonly int Level = GameServices.Stat.Level(1);
                public static readonly object Host = CompositionRoot.ServerHost;
            }
        }
        """;

    private static SourceSnapshot SampleSnapshot() => SourceSnapshot.FromText(new Dictionary<string, IReadOnlyDictionary<string, string>>
    {
        [Server] = new Dictionary<string, string>
        {
            ["SWLOR.Game.Server/Composition/Composition.cs"] = Composition,
            ["SWLOR.Game.Server/Feature/Content.cs"] = Content,
        }
    });

    /// <summary>The finding without its location, so an exception survives an unrelated edit to its file.</summary>
    private static IEnumerable<string> WithoutLocation(IEnumerable<string> findings) =>
        findings.Select(finding => finding.Contains(" (SWLOR", StringComparison.Ordinal) ? finding[..finding.LastIndexOf(" (SWLOR", StringComparison.Ordinal)] : finding);

    [Test]
    public void The_sample_breaks_each_rule_where_it_should()
    {
        var rules = Sample.Value;

        WithoutLocation(rules.ServiceSetters()).Should().Equal("SWLOR.Game.Server.Feature.Shapes: sets Shapes._stat (IStatService) outside its constructor");
        WithoutLocation(rules.RegisteredCreations()).Should().Equal("SWLOR.Game.Server.Feature.Creator: builds StatService, which the container registers: take it through the constructor");
        WithoutLocation(rules.MadeContentCreations()).Should().Equal(
            "SWLOR.Game.Server.Feature.Creator: builds Venom, which takes services, with services that are not its own: use the content factory");
        WithoutLocation(rules.ContentMemberShapes()).Should().Equal(
            "SWLOR.Game.Server.Feature.Shapes: Shapes.Money holds IBankService: name it Bank (or BankService where the class has a member Bank)",
            "SWLOR.Game.Server.Feature.Shapes: Shapes.Stat holds IStatService: content takes a service as a private get-only property named Stat",
            "SWLOR.Game.Server.Feature.Shapes: Shapes._stat holds IStatService: content takes a service as a private get-only property named Stat");
        rules.SingletonCycles().Should().Equal(
            "SWLOR.Game.Server.Feature.Back: needs itself through other content singletons; the container cannot build it",
            "SWLOR.Game.Server.Feature.Loop: needs itself through other content singletons; the container cannot build it");
        WithoutLocation(rules.StaticInitializers()).Should().Equal("SWLOR.Game.Server.Feature.Tables: a static initializer of SWLOR.Game.Server.Feature.Tables reads a service");
        WithoutLocation(rules.StaticContainerHosts()).Should().Equal("SWLOR.Game.Server.Feature.Tables reads CompositionRoot.ServerHost");
        WithoutLocation(rules.RegistrationProblems()).Should().Equal(
            "SWLOR.Game.Server.Feature.Creator: builds Scripts with the content factory, but the container registers it",
            "SWLOR.Game.Server.Feature.Unregistered: Unregistered has the instance handler Handle but ContentRegistrations does not register it");
    }

    [Test]
    public void No_method_or_property_sets_a_service_typed_member()
    {
        WithoutLocation(Real.Value.ServiceSetters()).Should().BeEmpty("services come in through constructors only (owner decision 8)");
    }

    [Test]
    public void A_service_or_content_singleton_is_never_built_outside_composition()
    {
        WithoutLocation(Real.Value.RegisteredCreations()).Should().Equal(new[]
        {
            // The native entry binds this build's sinks before the container exists; the sinks are resolved from the container later.
            "SWLOR.Game.Server.Core.ServerBootstrapper: builds NativeHookTable, which the container registers: take it through the constructor",
            // The clean-up of a hosted boot that stopped before the container was built: there is no container to ask.
            "SWLOR.Game.Server.Core.ServerBootstrapper: builds ServerStaticState, which the container registers: take it through the constructor",
        }, "the container builds every service and content singleton");
    }

    [Test]
    public void A_type_that_takes_services_is_built_by_the_factory_by_itself_or_as_a_private_collaborator()
    {
        WithoutLocation(Real.Value.MadeContentCreations()).Should().BeEmpty();
    }

    [Test]
    public void Service_typed_members_of_content_are_private_and_named_like_the_old_property()
    {
        WithoutLocation(Real.Value.ContentMemberShapes()).Should().BeEmpty();
    }

    [Test]
    public void No_two_content_singletons_depend_on_each_other()
    {
        Real.Value.SingletonCycles().Should().BeEmpty();
    }

    [Test]
    public void No_static_initializer_reads_a_service()
    {
        WithoutLocation(Real.Value.StaticInitializers()).Should().Equal(new[]
        {
            // WeaponDamageScales reads Item 11 times; the read moves to first use when the migrations are converted to instance services.
            "SWLOR.Game.Server.Feature.MigrationDefinition.SerializedItemWeaponDamageTypeMigration: a static initializer of SWLOR.Game.Server.Feature.MigrationDefinition.SerializedItemWeaponDamageTypeMigration reads a service",
        });
    }

    [Test]
    public void Only_the_entry_points_read_a_static_ContainerHost()
    {
        // The entry points until ServerHost is removed: the composition root that declares it, and the bootstrapper's default
        // constructor (ServerManager boots the server's container through it).
        WithoutLocation(Real.Value.StaticContainerHosts()).Distinct().Should().Equal(
            "SWLOR.Game.Server.Composition.CompositionRoot reads CompositionRoot.ServerHost",
            "SWLOR.Game.Server.Core.ServerBootstrapper reads CompositionRoot.ServerHost");
    }

    [Test]
    public void ContentRegistrations_lists_exactly_the_content_singletons()
    {
        WithoutLocation(Real.Value.RegistrationProblems()).Should().BeEmpty();
    }
}
