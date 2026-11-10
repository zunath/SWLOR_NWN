using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Proves, for every converted service in <c>ConvertedServices.json</c>, that the container holds exactly one instance of it,
/// that its handlers are on the instance class under the names <c>HandlerRenames.txt</c> gives them, and that the
/// <c>GameServices</c> property static code reaches it through returns the installed container's instance. The architecture
/// tests prove the shape (static state, handlers, service location).
/// <para>
/// A service whose members went to several classes (<c>Combat</c>) has one <c>GameServices</c> property per part, so a call
/// goes through no other service first; its pure members are on its rules class (<c>Domain.Combat.CombatRules</c>).
/// </para>
/// </summary>
[TestFixture]
public class ConvertedServiceContainerTests
{
    // Every class of every converted service: the main one, its further classes and its parts.
    private static IEnumerable<TestCaseData> Services() => ConvertedServiceFixtures.AsTestCases();

    // The classes static code reaches through a GameServices property.
    private static IEnumerable<TestCaseData> Accessed() => ConvertedServiceFixtures.AsAccessedTestCases();

    // The pure static rules class of a service (Domain.<Domain>.<Name>Rules), whose members static code calls directly.
    private static Type? Rules(ConvertedServiceTypes service) =>
        typeof(CompositionRoot).Assembly.GetType($"SWLOR.Game.Server.Domain.{service.Domain}.{service.Name}Rules") is { IsAbstract: true, IsSealed: true } rules
            ? rules
            : null;

    [TestCaseSource(nameof(Services))]
    public void The_container_holds_one_instance_of_the_service_and_it_is_the_instance_class(ConvertedServiceTypes service)
    {
        var host = new ContainerHost();
        CompositionRoot.Build(host);

        var first = host.Provider.GetRequiredService(service.Contract);
        var second = host.Provider.GetRequiredService(service.Contract);

        first.Should().BeOfType(service.Instance);
        second.Should().BeSameAs(first);
    }

    [TestCaseSource(nameof(Services))]
    public void The_handlers_of_a_service_are_on_its_instance_class_under_the_names_HandlerRenames_gives_them(ConvertedServiceTypes service)
    {
        const string HandlerAttribute = "NWNEventHandler";

        // The static facades are gone: their handlers moved onto the instance methods they forwarded to, each renamed in
        // tools/Refactor/HandlerRenames.txt, and the script registry binds them to the container's singleton. A line names
        // master's handler, which for an earlier move is not on the facade master had
        // (SWLOR.Game.Server.Domain.Combat.TemporaryStatModifier before the facade moved to Service.TemporaryStatModifier), so a line
        // belongs to this service when its master name is on that facade or its branch name on the instance; of a split
        // service (Combat), only a line whose branch name is on this class: the facade's handlers went to the parts that own
        // them.
        var moved = service.Instance.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes().Any(a => a.GetType().Name == HandlerAttribute))
            .ToList();
        var isSplit = service.IsSplitFacade;
        moved.Select(method => service.Instance.FullName + "." + method.Name).Should().BeEquivalentTo(
            ConvertedServiceFixtures.HandlerRenames()
                .Where(rename => (!isSplit && service.RemovedFacade is not null && rename.Master.StartsWith(service.RemovedFacade + ".", StringComparison.Ordinal)) ||
                                 rename.Branch.StartsWith(service.Instance.FullName + ".", StringComparison.Ordinal))
                .Select(rename => rename.Branch),
            "the instance keeps exactly the handlers its facade had, under the names HandlerRenames.txt gives them");
        moved.Should().OnlyContain(method => method.IsPublic && !method.IsStatic && method.GetParameters().Length == 0 &&
                                             (method.ReturnType == typeof(void) || method.ReturnType == typeof(bool)),
            "the script registry binds a public instance handler as an Action (or a conditional script) on the singleton");
    }

    [TestCaseSource(nameof(Accessed))]
    public void The_GameServices_property_of_a_service_returns_the_installed_containers_instance(ConvertedServiceTypes service)
    {
        var name = service.AccessorProperty!;
        var property = typeof(GameServices).GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        property.Should().NotBeNull($"static code reaches {service.Contract.Name} through GameServices.{name}");
        property!.PropertyType.Should().Be(service.Contract);

        var proxyInstance = RecordingProxy.Create(service.Contract, out var proxy);
        using (var provider = ContainerHost.Create(new ServiceCollection().AddSingleton(service.Contract, proxyInstance)))
        using (CompositionRoot.InstallForTests(provider))
        {
            property.GetValue(null).Should().BeSameAs(proxyInstance, "the property reads the container a test installed");
        }

        proxy.Calls.Should().BeEmpty("reading the property only resolves the service");

        var host = new ContainerHost();
        CompositionRoot.Build(host);
        using (CompositionRoot.InstallForTests(host.Provider))
        {
            property.GetValue(null).Should().BeOfType(service.Instance).And.BeSameAs(host.Provider.GetRequiredService(service.Contract));
        }
    }

    [Test]
    public void GameServices_has_a_property_for_each_class_static_code_reaches_and_nothing_else()
    {
        const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var expected = ConvertedServiceFixtures.Load().Where(service => service.AccessorProperty is not null)
            .Select(service => $"{service.Contract.Name} {service.AccessorProperty}")
            .OrderBy(text => text, StringComparer.Ordinal);

        typeof(GameServices).GetProperties(Declared).Select(property => $"{property.PropertyType.Name} {property.Name}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .Should().Equal(expected, "the accessor has one property per service and per further class, and per part of a split service, named like it and typed as its interface");
        typeof(GameServices).GetFields(Declared).Should().BeEmpty("the accessor holds no state; its properties only resolve");
        typeof(GameServices).GetMethods(Declared).Should().OnlyContain(method => method.IsSpecialName && method.Name.StartsWith("get_", StringComparison.Ordinal));
    }

    [Test]
    public void Every_domain_service_in_the_container_is_listed_as_converted()
    {
        var notServices = Array.Empty<string>();
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);

        var registered = services
            .Select(descriptor => descriptor.ServiceType)
            .Where(type => type.Namespace!.StartsWith("SWLOR.Game.Server.Domain.", StringComparison.Ordinal) && !notServices.Contains(type.Name))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal);
        var listed = ConvertedServiceFixtures.Load().Select(service => service.Contract.Name).OrderBy(name => name, StringComparer.Ordinal);

        registered.Should().Equal(listed, "SWLOR.Architecture.Tests/ConvertedServices.json lists exactly the services Composition registers");
    }

    [Test]
    public void The_server_registrations_build_and_every_service_is_a_singleton()
    {
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);

        services.Should().OnlyContain(descriptor => descriptor.Lifetime == ServiceLifetime.Singleton);
        var build = () => ContainerHost.Create(services).Dispose();
        build.Should().NotThrow();
    }

    [Test]
    public void Combat_lists_its_fourteen_parts_and_each_part_has_a_GameServices_property()
    {
        // The cases above would pass vacuously if the list lost its parts.
        var combat = ConvertedServiceFixtures.Load().Single(service => service.Name == "Combat");
        combat.Parts.Should().HaveCount(14);
        Rules(combat).Should().NotBeNull();
        combat.Parts.Should().OnlyContain(part => typeof(GameServices).GetProperty(part.Name.Substring(1, part.Name.Length - 1 - "Service".Length), BindingFlags.Public | BindingFlags.Static) != null);
    }
}
