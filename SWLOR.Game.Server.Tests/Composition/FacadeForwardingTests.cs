using System.Linq.Expressions;
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
/// <para>
/// The facade of a service split into parts (<c>Combat</c>) forwards a member a part owns straight to that part's
/// interface, and a member of the service's pure static rules class (<c>Domain.Combat.CombatRules</c>) straight to the
/// rule, so a call goes through no other service first. Every member of the main interface stays on the facade.
/// </para>
/// </summary>
[TestFixture]
public class FacadeForwardingTests
{
    private const BindingFlags Statics = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

    // Every class of every converted service (the main one, extra facades and parts), and those that have a static facade.
    private static IEnumerable<TestCaseData> Services() => ConvertedServiceFixtures.AsTestCases();

    private static IEnumerable<TestCaseData> Facades() => ConvertedServiceFixtures.AsFacadeTestCases();

    // The facade's methods and the getters of its static properties (the interface lists a property as its get_ method),
    // plus the internal static methods that code inside the server (a native hook, say) calls. Private members are the
    // facade's own plumbing.
    private static IEnumerable<MethodInfo> FacadeMethods(Type facade) =>
        facade.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.IsPublic || method.IsAssembly)
            .Where(method => !method.IsSpecialName || method.Name.StartsWith("get_", StringComparison.Ordinal));

    private static IEnumerable<MethodInfo> ContractMethods(Type contract) =>
        contract.GetMethods().Where(method => !method.IsSpecialName || method.Name.StartsWith("get_", StringComparison.Ordinal));

    private static string Text(MethodInfo method) =>
        $"{method.ReturnType.Name} {method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";

    // The pure static rules class of a service (Domain.<Domain>.<Name>Rules), whose members its facade may forward to directly.
    private static Type? Rules(ConvertedServiceTypes service) =>
        typeof(CompositionRoot).Assembly.GetType($"SWLOR.Game.Server.Domain.{service.Domain}.{service.Name}Rules") is { IsAbstract: true, IsSealed: true } rules
            ? rules
            : null;

    private static IEnumerable<MethodInfo> RuleMethods(Type? rules) =>
        rules?.GetMethods(Statics).Where(method => !method.IsPrivate && !method.IsSpecialName) ?? Enumerable.Empty<MethodInfo>();

    // The member of a type with the facade member's name and parameter types. A generic member (Ability.CaptureRepeatedAbilityImpact<T>,
    // DB.Set<T>) is matched by its type parameter count and the names of its parameter types (T against T).
    private static MethodInfo? Find(Type type, MethodInfo facadeMethod, BindingFlags flags)
    {
        var types = facadeMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        if (!facadeMethod.IsGenericMethodDefinition)
            return type.GetMethod(facadeMethod.Name, flags, null, types, null);

        return type.GetMethods(flags).SingleOrDefault(candidate =>
            candidate.Name == facadeMethod.Name &&
            candidate.IsGenericMethodDefinition &&
            candidate.GetGenericArguments().Length == facadeMethod.GetGenericArguments().Length &&
            candidate.GetParameters().Select(parameter => parameter.ParameterType.ToString()).SequenceEqual(types.Select(parameterType => parameterType.ToString())));
    }

    // The interface a facade member forwards to: the service's own when it declares the member, otherwise the one part that
    // does, or null when no interface does (a rule of the rules class).
    private static Type? Owner(ConvertedServiceTypes service, MethodInfo facadeMethod)
    {
        if (Find(service.Contract, facadeMethod, BindingFlags.Public | BindingFlags.Instance) != null)
            return service.Contract;

        var parts = service.Parts.Where(part => Find(part, facadeMethod, BindingFlags.Public | BindingFlags.Instance) != null).ToArray();
        parts.Length.Should().BeLessThanOrEqualTo(1,
            $"{service.Facade!.Name}.{facadeMethod.Name} must name one part, but {string.Join(", ", parts.Select(part => part.Name))} all declare it");
        return parts.SingleOrDefault();
    }

    [TestCaseSource(nameof(Facades))]
    public void The_facade_and_the_interface_have_the_same_members(ConvertedServiceTypes service)
    {
        var facadeMembers = FacadeMethods(service.Facade!).Select(Text).OrderBy(text => text, StringComparer.Ordinal).ToList();
        var contractMembers = ContractMethods(service.Contract).Select(Text).OrderBy(text => text, StringComparer.Ordinal).ToList();

        facadeMembers.Should().OnlyHaveUniqueItems();
        facadeMembers.Should().Contain(contractMembers, "the facade is the interface's members as static methods, so no caller changes");

        // Anything else on the facade is a member of one of the service's parts or a rule of its rules class; a service with
        // neither has exactly its interface's members.
        var owned = service.Parts.SelectMany(ContractMethods).Concat(RuleMethods(Rules(service))).Select(Text).ToHashSet(StringComparer.Ordinal);
        facadeMembers.Except(contractMembers).Should().OnlyContain(text => owned.Contains(text),
            "a facade member that is not on the service's interface forwards to a part or a rule that declares it");
    }

    [TestCaseSource(nameof(Facades))]
    public void Every_facade_member_forwards_to_the_instance_with_the_same_arguments_and_returns_its_result(ConvertedServiceTypes service)
    {
        var contracts = new[] { service.Contract }.Concat(service.Parts).ToArray();
        foreach (var openMethod in FacadeMethods(service.Facade!))
        {
            var method = CloseGeneric(openMethod);
            var owner = Owner(service, openMethod);
            if (owner is null)
            {
                AssertForwardsToItsRule(service, method);
                continue;
            }

            var proxies = new Dictionary<Type, RecordingProxy>();
            var registrations = new ServiceCollection();
            foreach (var contract in contracts)
            {
                registrations.AddSingleton(contract, RecordingProxy.Create(contract, out var recording));
                recording.ReturnsInterfaceStandIns = true;
                proxies[contract] = recording;
            }

            var provider = ContainerHost.Create(registrations);
            var parameters = method.GetParameters();
            var arguments = parameters.Select(parameter => SampleValue(parameter.ParameterType)).ToArray();
            for (var i = 0; i < parameters.Length; i++)
            {
                if (!parameters[i].ParameterType.IsByRef)
                    arguments[i].Should().NotBeNull($"{service.Facade!.Name}.{method.Name}({parameters[i].Name}) gets a sample, so a forwarder that drops it fails");
            }

            object? result;
            using (CompositionRoot.InstallForTests(provider))
            {
                result = method.Invoke(null, arguments);
            }

            proxies.Values.SelectMany(recording => recording.Calls).Should().ContainSingle($"{service.Facade!.Name}.{method.Name} makes exactly one call");
            var proxy = proxies[owner];
            var call = proxy.Calls.Should().ContainSingle($"{service.Facade!.Name}.{method.Name} calls {owner.Name}").Subject;
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
                if (method.ReturnType.IsInterface)
                    returned.Should().NotBeNull($"{service.Facade!.Name}.{method.Name} returns a distinct stand-in, so a facade that returns null fails");
            }
            provider.Dispose();
        }
    }

    // A facade member with no interface behind it forwards to the rule of the same name and parameter types of the service's
    // rules class (it needs no container: the Beast constructors call Combat.CreateDefaultDefenseValues). Its body is that
    // one call with its parameters in order: the compiled method loads each argument once, in order, calls the rule and
    // returns (a Debug build may add a nop and keep the result in a local first).
    private static void AssertForwardsToItsRule(ConvertedServiceTypes service, MethodInfo method)
    {
        var label = $"{service.Facade!.Name}.{method.Name}";
        var rules = Rules(service);
        rules.Should().NotBeNull($"{label} is on no interface of {service.Name}, so it must forward to its rules class");
        var rule = Find(rules!, method, Statics);
        rule.Should().NotBeNull($"{label} forwards to the rule of the same name and parameter types");
        rule!.IsPrivate.Should().BeFalse(label);
        rule.ReturnType.Should().Be(method.ReturnType, label);

        var il = method.GetMethodBody()!.GetILAsByteArray()!;
        var loaded = new List<int>();
        var called = new List<MethodBase>();
        var position = 0;
        var returned = false;
        while (position < il.Length)
        {
            var code = il[position++];
            switch (code)
            {
                case 0x00: // nop
                case 0x06: // ldloc.0
                case 0x0A: // stloc.0
                    break;
                case >= 0x02 and <= 0x05: // ldarg.0 to ldarg.3
                    loaded.Add(code - 0x02);
                    break;
                case 0x0E: // ldarg.s
                    loaded.Add(il[position++]);
                    break;
                case 0xFE when il[position] == 0x09: // ldarg
                    loaded.Add(BitConverter.ToUInt16(il, position + 1));
                    position += 3;
                    break;
                case 0x2B: // br.s
                    position++;
                    break;
                case 0x28: // call
                    called.Add(method.Module.ResolveMethod(BitConverter.ToInt32(il, position))!);
                    position += 4;
                    break;
                case 0x2A: // ret
                    returned = true;
                    break;
                default:
                    Assert.Fail($"{label} does more than forward: opcode 0x{code:X2} at {position - 1}.");
                    break;
            }
        }

        called.Should().Equal(new[] { rule }, $"{label} makes exactly one call, to {rules!.Name}.{rule.Name}");
        loaded.Should().Equal(Enumerable.Range(0, method.GetParameters().Length), $"{label} passes its parameters in order");
        returned.Should().BeTrue(label);
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

        service.Instance.GetMethods().Where(method => method.GetCustomAttributes().Any(a => a.GetType().Name == HandlerAttribute))
            .Should().BeEmpty("the script registry binds handlers as static methods of the facade");
        if (service.Facade is null)
            return;

        FacadeMethods(service.Facade).Where(method => method.GetCustomAttributes().Any(a => a.GetType().Name == HandlerAttribute))
            .Should().OnlyContain(method => (method.ReturnType == typeof(void) || method.ReturnType == typeof(bool)) && method.GetParameters().Length == 0,
                "the script registry binds a handler as an Action, or as a conditional script when it returns bool (Snippet.ConversationAppearsWhen)");
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
    public void The_combat_facade_forwards_to_its_parts_its_service_and_its_rules()
    {
        // Combat is the one service whose facade forwards part members straight to the part and rules straight to
        // CombatRules; the cases above would pass vacuously if the list lost its parts or the rules class its name.
        var combat = ConvertedServiceFixtures.Load().Single(service => service.Name == "Combat");
        combat.Parts.Should().HaveCount(14);
        Rules(combat).Should().NotBeNull();

        var methods = FacadeMethods(combat.Facade!).ToArray();
        var owners = methods.Select(method => Owner(combat, method)).ToArray();
        owners.Count(owner => owner == combat.Contract).Should().Be(ContractMethods(combat.Contract).Count());
        owners.Where(owner => owner != null && owner != combat.Contract).Distinct().Should().HaveCount(14, "every part has members on the facade");
        var ruleForwarders = methods.Where((_, index) => owners[index] is null).Select(Text).ToHashSet(StringComparer.Ordinal);
        RuleMethods(Rules(combat)).Where(rule => rule.IsPublic).Select(Text).Should().OnlyContain(text => ruleForwarders.Contains(text),
            "Combat keeps a forwarder for every public rule, and no interface claims it");
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

    // A value per parameter type that differs from the default where it can, and is never null (only an out parameter
    // starts as null; the proxy writes one back), so a forwarder that drops an argument or passes another fails.
    private static object? SampleValue(Type type)
    {
        if (type.IsByRef)
            return null;
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
        if (type == typeof(TimeSpan))
            return TimeSpan.FromSeconds(3);
        if (Nullable.GetUnderlyingType(type) is { } underlying)
            return SampleValue(underlying);
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(values.Length > 1 ? 1 : 0);
        }

        if (type.IsValueType)
            return Activator.CreateInstance(type);
        if (type == typeof(object))
            return new object();
        if (type == typeof(Type))
            return typeof(FacadeForwardingTests);
        if (type.IsArray)
            return Array.CreateInstance(type.GetElementType()!, 2);
        if (type.IsInterface)
            return RecordingProxy.StandIn(type);
        // A delegate cannot be created uninitialized; a compiled lambda of its type is an instance the proxy can compare by
        // reference.
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            var invoke = type.GetMethod("Invoke")!;
            var parameters = invoke.GetParameters().Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name)).ToArray();
            return Expression.Lambda(type, Expression.Default(invoke.ReturnType), parameters).Compile();
        }

        // An abstract class (a window payload, GuiPayloadBase) gets an instance of its first concrete subclass in the server.
        if (type.IsAbstract)
        {
            type = typeof(CompositionRoot).Assembly.GetTypes()
                       .Where(candidate => candidate.IsClass && !candidate.IsAbstract && !candidate.ContainsGenericParameters && type.IsAssignableFrom(candidate))
                       .OrderBy(candidate => candidate.FullName, StringComparer.Ordinal)
                       .FirstOrDefault()
                   ?? throw new AssertionException($"No concrete class of the server derives from {type.Name}, so it has no sample.");
        }

        // A native wrapper (CNWSCreature) or an engine structure (Location) must never reach its finalizer in a process
        // without the engine.
        var sample = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
        GC.SuppressFinalize(sample);
        return sample;
    }
}
