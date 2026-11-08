using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Proves, for every converted service in <c>ConvertedServices.json</c>, that its static facade keeps the API the
/// content code calls and forwards each member to the container's instance with the same arguments and result, and
/// that the container holds exactly one instance of it. The architecture tests prove the shape (static state,
/// handlers, service location); these prove the behaviour of the forwarding.
/// </summary>
[TestFixture]
public class FacadeForwardingTests
{
    // Every class of every converted service (the main one, extra facades and parts), and those that have a static facade.
    private static IEnumerable<TestCaseData> Services() => ConvertedServiceFixtures.AsTestCases();

    private static IEnumerable<TestCaseData> Facades() => ConvertedServiceFixtures.AsFacadeTestCases();

    // The classes whose static facade the branch removed (P2-Z): they have no facade to forward, and GameServices reaches them.
    private static IEnumerable<TestCaseData> RemovedFacades() => ConvertedServiceFixtures.AsRemovedFacadeTestCases();

    // The facade's methods and the getters of its static properties (the interface lists a property as its get_ method),
    // plus the internal static methods that code inside the server (a native hook, say) calls. Private members are the
    // facade's own plumbing.
    private static IEnumerable<MethodInfo> FacadeMethods(Type facade) =>
        facade.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.IsPublic || method.IsAssembly)
            .Where(method => !method.IsSpecialName || method.Name.StartsWith("get_", StringComparison.Ordinal));

    private static MethodInfo Signature(Type contract, MethodInfo facadeMethod) =>
        contract.GetMethod(facadeMethod.Name, facadeMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray())
        ?? throw new AssertionException($"{contract.Name} has no member {facadeMethod.Name}({string.Join(", ", facadeMethod.GetParameters().Select(p => p.ParameterType.Name))}).");

    [TestCaseSource(nameof(Facades))]
    public void The_facade_and_the_interface_have_the_same_members(ConvertedServiceTypes service)
    {
        var facadeMembers = FacadeMethods(service.Facade!)
            .Select(method => $"{method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})")
            .OrderBy(text => text, StringComparer.Ordinal);
        var contractMembers = service.Contract.GetMethods()
            .Where(method => !method.IsSpecialName || method.Name.StartsWith("get_", StringComparison.Ordinal))
            .Select(method => $"{method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})")
            .OrderBy(text => text, StringComparer.Ordinal);

        facadeMembers.Should().Equal(contractMembers, "the facade is the interface's members as static methods, so no caller changes");
    }

    [TestCaseSource(nameof(Facades))]
    public void Every_facade_member_forwards_to_the_instance_with_the_same_arguments_and_returns_its_result(ConvertedServiceTypes service)
    {
        foreach (var openMethod in FacadeMethods(service.Facade!))
        {
            var method = CloseGeneric(openMethod);
            var proxyInstance = RecordingProxy.Create(service.Contract, out var proxy);
            var provider = ContainerHost.Create(new ServiceCollection().AddSingleton(service.Contract, proxyInstance));
            var arguments = method.GetParameters().Select(parameter => SampleValue(parameter.ParameterType)).ToArray();

            object? result;
            using (CompositionRoot.InstallForTests(provider))
            {
                result = method.Invoke(null, arguments);
            }

            var call = proxy.Calls.Should().ContainSingle($"{service.Facade!.Name}.{method.Name} makes exactly one call").Subject;
            call.Method.Name.Should().Be(method.Name);
            call.Arguments.Should().Equal(arguments, $"{service.Facade!.Name}.{method.Name} passes its arguments through");
            if (method.ReturnType != typeof(void))
            {
                var returned = proxy.ReturnedBy(call.Method);
                var because = $"{service.Facade!.Name}.{method.Name} returns what the instance returned";
                if (method.ReturnType.IsValueType)
                    result.Should().Be(returned, because);
                else
                    result.Should().BeSameAs(returned, because);
            }
            provider.Dispose();
        }
    }

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
    public void The_instance_class_has_no_event_handlers_and_the_facade_keeps_the_ones_it_had(ConvertedServiceTypes service)
    {
        const string HandlerAttribute = "NWNEventHandler";

        if (service.RemovedFacade is { } removed)
        {
            // The facade is gone (P2-Z): its handlers moved onto the instance methods it forwarded to, each renamed in
            // tools/Refactor/HandlerRenames.txt, and the script registry binds them to the container's singleton.
            var moved = service.Instance.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes().Any(a => a.GetType().Name == HandlerAttribute))
                .ToList();
            moved.Select(method => service.Instance.FullName + "." + method.Name).Should().BeEquivalentTo(
                ConvertedServiceFixtures.HandlerRenames().Where(rename => rename.Master.StartsWith(removed + ".", StringComparison.Ordinal)).Select(rename => rename.Branch),
                "the instance keeps exactly the handlers its facade had, under the names HandlerRenames.txt gives them");
            moved.Should().OnlyContain(method => method.IsPublic && !method.IsStatic && method.GetParameters().Length == 0 &&
                                                 (method.ReturnType == typeof(void) || method.ReturnType == typeof(bool)),
                "the script registry binds a public instance handler as an Action (or a conditional script) on the singleton");
            return;
        }

        service.Instance.GetMethods().Where(method => method.GetCustomAttributes().Any(a => a.GetType().Name == HandlerAttribute))
            .Should().BeEmpty("the script registry binds handlers as static methods of the facade");
        if (service.Facade is null)
            return;

        FacadeMethods(service.Facade).Where(method => method.GetCustomAttributes().Any(a => a.GetType().Name == HandlerAttribute))
            .Should().OnlyContain(method => (method.ReturnType == typeof(void) || method.ReturnType == typeof(bool)) && method.GetParameters().Length == 0,
                "the script registry binds a handler as an Action, or as a conditional script when it returns bool (Snippet.ConversationAppearsWhen)");
    }

    [TestCaseSource(nameof(RemovedFacades))]
    public void The_accessor_property_of_a_removed_facade_returns_the_installed_containers_instance(ConvertedServiceTypes service)
    {
        var name = service.Name[(service.Name.LastIndexOf('.') + 1)..];
        var property = typeof(GameServices).GetProperty(name, BindingFlags.Public | BindingFlags.Static);
        property.Should().NotBeNull($"GameServices.{name} replaces the removed facade {service.RemovedFacade}");
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
    public void GameServices_has_a_property_for_each_removed_facade_and_nothing_else()
    {
        const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        var removed = ConvertedServiceFixtures.Load().Where(service => service.RemovedFacade is not null)
            .Select(service => $"{service.Contract.Name} {service.Name[(service.Name.LastIndexOf('.') + 1)..]}")
            .OrderBy(text => text, StringComparer.Ordinal);

        typeof(GameServices).GetProperties(Declared).Select(property => $"{property.PropertyType.Name} {property.Name}")
            .OrderBy(text => text, StringComparer.Ordinal)
            .Should().Equal(removed, "the accessor has one property per removed facade, named like it and typed as its interface");
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

    // A generic facade member (Gui.PublishRefreshEvent<T>) is closed over the first type of the server assembly that meets
    // the constraint of each type parameter, or over object when it has none.
    private static MethodInfo CloseGeneric(MethodInfo method)
    {
        if (!method.IsGenericMethodDefinition)
            return method;

        var arguments = method.GetGenericArguments().Select(parameter =>
        {
            var constraint = parameter.GetGenericParameterConstraints().FirstOrDefault();
            return constraint == null
                ? typeof(object)
                : typeof(CompositionRoot).Assembly.GetTypes()
                    .First(candidate => candidate.IsClass && !candidate.IsAbstract && !candidate.IsGenericTypeDefinition &&
                                        constraint.IsAssignableFrom(candidate));
        }).ToArray();
        return method.MakeGenericMethod(arguments);
    }

    private static object? SampleValue(Type type)
    {
        if (type == typeof(uint))
            return 1001u;
        if (type == typeof(int))
            return 7;
        if (type == typeof(long))
            return 7L;
        if (type == typeof(string))
            return "sample";
        if (type == typeof(bool))
            return true;
        if (type == typeof(float))
            return 1.5f;
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(values.Length > 1 ? 1 : 0);
        }

        return type.IsValueType ? Activator.CreateInstance(type) : null;
    }
}
