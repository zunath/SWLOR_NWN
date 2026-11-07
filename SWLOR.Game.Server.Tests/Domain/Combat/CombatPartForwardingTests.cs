using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using CombatFacade = SWLOR.Game.Server.Service.Combat;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// While <c>Combat</c> is split, every member of a converted part stays on the <c>Combat</c> class as a static forwarder
/// of the same name, parameters and defaults, so no caller changes. These tests prove, for each member of each part's
/// interface, that the forwarder exists with that signature and makes exactly one call on the container's instance with
/// the same arguments, returning its result.
/// </summary>
[TestFixture]
public class CombatPartForwardingTests
{
    private const BindingFlags Statics = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static IEnumerable<Type> Parts() => new[]
    {
        typeof(ICombatFeedbackService),
        typeof(ICombatQueriesService),
        typeof(ICombatResourcesService),
    };

    private static MethodInfo Forwarder(MethodInfo member) =>
        typeof(CombatFacade).GetMethod(member.Name, Statics, member.GetParameters().Select(parameter => parameter.ParameterType).ToArray())
        ?? throw new AssertionException($"Combat has no forwarder {member.Name}({string.Join(", ", member.GetParameters().Select(p => p.ParameterType.Name))}).");

    [TestCaseSource(nameof(Parts))]
    public void CombatKeepsAForwarderWithTheSameSignatureForEveryMember(Type part)
    {
        var members = part.GetMethods();
        members.Should().NotBeEmpty();

        foreach (var member in members)
        {
            var forwarder = Forwarder(member);
            forwarder.ReturnType.Should().Be(member.ReturnType, member.Name);
            var parameters = member.GetParameters();
            var forwarded = forwarder.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                forwarded[i].Name.Should().Be(parameters[i].Name, member.Name);
                forwarded[i].IsOut.Should().Be(parameters[i].IsOut, $"{member.Name}({parameters[i].Name})");
                forwarded[i].IsDefined(typeof(ParamArrayAttribute)).Should().Be(parameters[i].IsDefined(typeof(ParamArrayAttribute)), $"{member.Name}({parameters[i].Name})");
                forwarded[i].HasDefaultValue.Should().Be(parameters[i].HasDefaultValue, $"{member.Name}({parameters[i].Name})");
                if (parameters[i].HasDefaultValue)
                    forwarded[i].DefaultValue.Should().Be(parameters[i].DefaultValue, $"{member.Name}({parameters[i].Name})");
            }
        }
    }

    [TestCaseSource(nameof(Parts))]
    public void EveryForwarderMakesOneCallWithTheSameArgumentsAndReturnsItsResult(Type part)
    {
        foreach (var member in part.GetMethods())
        {
            var forwarder = Forwarder(member);
            var proxyInstance = RecordingProxy.Create(part, out var proxy);
            using var provider = ContainerHost.Create(new ServiceCollection().AddSingleton(part, proxyInstance));
            var arguments = forwarder.GetParameters().Select(parameter => SampleValue(parameter.ParameterType)).ToArray();

            object? result;
            using (CompositionRoot.InstallForTests(provider))
            {
                result = forwarder.Invoke(null, arguments);
            }

            var call = proxy.Calls.Should().ContainSingle($"Combat.{member.Name} makes exactly one call").Subject;
            call.Method.Should().BeSameAs(member, $"Combat.{member.Name} calls the member of the same signature");
            call.Arguments.Should().Equal(arguments, $"Combat.{member.Name} passes its arguments through in order");
            if (member.ReturnType == typeof(void))
                continue;

            var returned = proxy.ReturnedBy(call.Method);
            if (member.ReturnType.IsValueType)
                result.Should().Be(returned, $"Combat.{member.Name} returns what the instance returned");
            else
                result.Should().BeSameAs(returned, $"Combat.{member.Name} returns what the instance returned");
        }
    }

    [Test]
    public void TheMovedConstantsKeepTheirValuesOnCombat()
    {
        CombatFacade.BossMinimumHitRate.Should().Be(CombatQueriesService.BossMinimumHitRate).And.Be(35);
        CombatFacade.MaximumActivatedDamageHealingMaxHPPercent.Should().Be(CombatResourcesService.MaximumActivatedDamageHealingMaxHPPercent).And.Be(15);
        CombatFacade.MaximumPassiveDamageHealingMaxHPPercentPerHit.Should().Be(CombatResourcesService.MaximumPassiveDamageHealingMaxHPPercentPerHit).And.Be(6);
        CombatFacade.MaximumCooldownDamageHealingMaxHPPercentPerHit.Should().Be(CombatResourcesService.MaximumCooldownDamageHealingMaxHPPercentPerHit).And.Be(12);
        CombatFacade.MaximumPassiveDamageHealingMaxHPPercentPerWindow.Should().Be(CombatResourcesService.MaximumPassiveDamageHealingMaxHPPercentPerWindow).And.Be(24);
        CombatFacade.MaximumDefeatedEnemyHealingMaxHPPercentPerWindow.Should().Be(CombatResourcesService.MaximumDefeatedEnemyHealingMaxHPPercentPerWindow).And.Be(12);
        CombatFacade.CombatHealingWindowSeconds.Should().Be(CombatResourcesService.CombatHealingWindowSeconds).And.Be(6);
    }

    // A value per parameter type that differs from the default where it can, so a forwarder that drops or swaps an
    // argument fails. An out parameter starts as its default; the proxy writes one back.
    private static object? SampleValue(Type type)
    {
        if (type.IsByRef)
            return null;
        if (type == typeof(uint))
            return 1001u;
        if (type == typeof(int))
            return 7;
        if (type == typeof(string))
            return "sample";
        if (type == typeof(bool))
            return true;
        if (type == typeof(float))
            return 1.5f;
        if (type == typeof(TimeSpan))
            return TimeSpan.FromSeconds(3);
        if (type.IsArray)
            return Array.CreateInstance(type.GetElementType()!, 2);
        if (Nullable.GetUnderlyingType(type) is { IsEnum: true } nullableEnum)
            return Enum.GetValues(nullableEnum).GetValue(1);
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(values.Length > 1 ? 1 : 0);
        }

        if (type == typeof(Type))
            return typeof(CombatPartForwardingTests);
        if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return Array.CreateInstance(type.GetGenericArguments()[0], 1);
        if (type.IsValueType)
            return Activator.CreateInstance(type);
        if (type.IsAbstract || type.IsInterface)
            return null;
        // A native wrapper (CNWSCreature) must never reach its finalizer in a process without the engine.
        var sample = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
        GC.SuppressFinalize(sample);
        return sample;
    }
}
