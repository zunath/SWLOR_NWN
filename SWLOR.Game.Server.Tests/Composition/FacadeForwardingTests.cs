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
    private static IEnumerable<TestCaseData> Services() => ConvertedServiceFixtures.AsTestCases();

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

    [TestCaseSource(nameof(Services))]
    public void The_facade_and_the_interface_have_the_same_members(ConvertedServiceTypes service)
    {
        var facadeMembers = FacadeMethods(service.Facade)
            .Select(method => $"{method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})")
            .OrderBy(text => text, StringComparer.Ordinal);
        var contractMembers = service.Contract.GetMethods()
            .Select(method => $"{method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})")
            .OrderBy(text => text, StringComparer.Ordinal);

        facadeMembers.Should().Equal(contractMembers, "the facade is the interface's members as static methods, so no caller changes");
    }

    [TestCaseSource(nameof(Services))]
    public void Every_facade_member_forwards_to_the_instance_with_the_same_arguments_and_returns_its_result(ConvertedServiceTypes service)
    {
        foreach (var method in FacadeMethods(service.Facade))
        {
            var proxyInstance = RecordingProxy.Create(service.Contract, out var proxy);
            var provider = ContainerHost.Create(new ServiceCollection().AddSingleton(service.Contract, proxyInstance));
            var arguments = method.GetParameters().Select(parameter => SampleValue(parameter.ParameterType)).ToArray();

            object? result;
            using (CompositionRoot.InstallForTests(provider))
            {
                result = method.Invoke(null, arguments);
            }

            var call = proxy.Calls.Should().ContainSingle($"{service.Facade.Name}.{method.Name} makes exactly one call").Subject;
            call.Method.Name.Should().Be(method.Name);
            call.Arguments.Should().Equal(arguments, $"{service.Facade.Name}.{method.Name} passes its arguments through");
            if (method.ReturnType != typeof(void))
            {
                var returned = proxy.ReturnedBy(call.Method);
                var because = $"{service.Facade.Name}.{method.Name} returns what the instance returned";
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
    public void The_container_holds_one_instance_of_each_part_of_the_service_and_it_is_the_part_class(ConvertedServiceTypes service)
    {
        var host = new ContainerHost();
        CompositionRoot.Build(host);

        foreach (var part in service.Parts)
        {
            var first = host.Provider.GetRequiredService(part.Contract);
            var second = host.Provider.GetRequiredService(part.Contract);

            first.Should().BeOfType(part.Instance, $"{part.Name} is registered behind {part.Contract.Name}");
            second.Should().BeSameAs(first, $"{part.Name} is a singleton");
        }
    }

    [TestCaseSource(nameof(Services))]
    public void The_instance_class_has_no_event_handlers_and_the_facade_keeps_the_ones_it_had(ConvertedServiceTypes service)
    {
        const string HandlerAttribute = "NWNEventHandler";

        service.Instance.GetMethods().Concat(service.Parts.SelectMany(part => part.Instance.GetMethods()))
            .Where(method => method.GetCustomAttributes().Any(a => a.GetType().Name == HandlerAttribute))
            .Should().BeEmpty("the script registry binds handlers as static methods of the facade");
        FacadeMethods(service.Facade).Where(method => method.GetCustomAttributes().Any(a => a.GetType().Name == HandlerAttribute))
            .Should().OnlyContain(method => method.ReturnType == typeof(void) && method.GetParameters().Length == 0,
                "the script registry binds a handler as an Action");
    }

    [Test]
    public void Every_domain_service_in_the_container_is_listed_as_converted()
    {
        // IDBService is the temporary adapter over the static DB class; it is not a converted service (DB converts in wave C).
        var notServices = new[] { "IDBService" };
        var services = new ServiceCollection();
        CompositionRoot.RegisterServerServices(services);

        var registered = services
            .Select(descriptor => descriptor.ServiceType)
            .Where(type => type.Namespace!.StartsWith("SWLOR.Game.Server.Domain.", StringComparison.Ordinal) && !notServices.Contains(type.Name))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal);
        var listed = ConvertedServiceFixtures.Load()
            .SelectMany(service => service.Parts.Select(part => part.Contract.Name).Prepend(service.Contract.Name))
            .OrderBy(name => name, StringComparer.Ordinal);

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
