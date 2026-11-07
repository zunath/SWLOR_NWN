using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
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
        typeof(ICombatAbilityBonusService),
        typeof(ICombatActivityService),
        typeof(ICombatAttackTimingService),
        typeof(ICombatDamageModifiersService),
        typeof(ICombatFeedbackService),
        typeof(ICombatQueriesService),
        typeof(ICombatResourcesService),
        typeof(ICombatSuppressionService),
    };

    // The methods of a part; a property (the activity part's shared table) is checked by its own test below.
    private static MethodInfo[] Members(Type part) => part.GetMethods().Where(method => !method.IsSpecialName).ToArray();

    private static MethodInfo Forwarder(MethodInfo member) =>
        typeof(CombatFacade).GetMethod(member.Name, Statics, member.GetParameters().Select(parameter => parameter.ParameterType).ToArray())
        ?? throw new AssertionException($"Combat has no forwarder {member.Name}({string.Join(", ", member.GetParameters().Select(p => p.ParameterType.Name))}).");

    [Test]
    public void OnlyTheActivityPartHasAProperty()
    {
        Parts().SelectMany(part => part.GetProperties().Select(property => $"{part.Name}.{property.Name}"))
            .Should().Equal("ICombatActivityService.RecentDamageTargets");
        Parts().SelectMany(part => part.GetMethods().Where(method => method.IsSpecialName).Select(method => method.Name))
            .Should().Equal("get_RecentDamageTargets");
    }

    [TestCaseSource(nameof(Parts))]
    public void CombatKeepsAForwarderWithTheSameSignatureForEveryMember(Type part)
    {
        var members = Members(part);
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
        foreach (var member in Members(part))
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

    // The instance class keeps its interface's defaults, so a call made on the instance binds the values a call through
    // Combat binds.
    [TestCaseSource(nameof(Parts))]
    public void ThePartServiceKeepsTheDefaultsOfItsInterface(Type part)
    {
        var service = part.Assembly.GetTypes().Single(type => type.IsClass && !type.IsAbstract && part.IsAssignableFrom(type));
        var map = service.GetInterfaceMap(part);
        for (var i = 0; i < map.InterfaceMethods.Length; i++)
        {
            var declared = map.InterfaceMethods[i].GetParameters();
            var implemented = map.TargetMethods[i].GetParameters();
            for (var p = 0; p < declared.Length; p++)
            {
                var label = $"{service.Name}.{map.TargetMethods[i].Name}({declared[p].Name})";
                implemented[p].HasDefaultValue.Should().Be(declared[p].HasDefaultValue, label);
                if (declared[p].HasDefaultValue)
                    implemented[p].DefaultValue.Should().Be(declared[p].DefaultValue, label);
            }
        }
    }

    // The members of the ability bonus and damage modifier parts that have default arguments, with the defaults they had
    // as static members of Combat, on the forwarder, the interface and the instance class alike.
    [TestCase(typeof(ICombatAbilityBonusService), "GetNextSkillAbilityStaminaCostAdjustment(isHostileAbility=False)")]
    [TestCase(typeof(ICombatAbilityBonusService), "ConsumeNextSkillAbilityStaminaCostAdjustment(isHostileAbility=False)")]
    [TestCase(typeof(ICombatAbilityBonusService), "ConsumeNextSkillAbilityBonuses(abilityStatusCategories=None)")]
    [TestCase(typeof(ICombatAbilityBonusService), "GetQueuedWeaponAbilityActivationHitChanceAdjustment(ability=null)")]
    [TestCase(typeof(ICombatDamageModifiersService), "CalculateDamageWithCriticalMitigation(deltaCap=0)")]
    [TestCase(typeof(ICombatDamageModifiersService), "ApplyCriticalDamageModifier(skillType=Invalid, defender=2130706432, criticalDamagePercentAdjustment=0)")]
    [TestCase(typeof(ICombatDamageModifiersService), "ApplyDamageDealtModifiers(abilityDamagePercentAdjustment=0, lowHPAbilityDamagePercentAdjustment=0, isOrdinaryWeaponAttack=False)")]
    [TestCase(typeof(ICombatDamageModifiersService), "ApplyDamageDealtModifiers(skillType=Invalid, damageType=Physical, isAbilityDamage=False, canApplyRandomFlatBonuses=True, isLandedAttack=True, ability=null)")]
    public void TheMovedDefaultsKeepTheirValues(Type part, string expected)
    {
        static string Defaults(MethodInfo method) => $"{method.Name}(" + string.Join(", ", method.GetParameters()
            .Where(parameter => parameter.HasDefaultValue)
            .Select(parameter => $"{parameter.Name}={parameter.DefaultValue ?? "null"}")) + ")";

        var service = part.Assembly.GetTypes().Single(type => type.IsClass && !type.IsAbstract && part.IsAssignableFrom(type));
        var members = part.GetMethods().Where(method => Defaults(method) == expected).ToArray();
        members.Should().ContainSingle(expected);
        var member = members[0];
        Defaults(Forwarder(member)).Should().Be(expected, "Combat's forwarder");
        var map = service.GetInterfaceMap(part);
        Defaults(map.TargetMethods[Array.IndexOf(map.InterfaceMethods, member)]).Should().Be(expected, service.Name);
        NWScript.OBJECT_INVALID.Should().Be(2130706432u);

        var all = Parts().Where(type => type == typeof(ICombatAbilityBonusService) || type == typeof(ICombatDamageModifiersService))
            .SelectMany(type => type.GetMethods())
            .Where(method => method.GetParameters().Any(parameter => parameter.HasDefaultValue))
            .Select(Defaults);
        all.Should().HaveCount(8, "the PR lists these eight members");
    }

    [Test]
    public void TheRecentDamageTargetsTheUnmovedPartsReadAreTheActivityPartsOwnTable()
    {
        using var facades = new FacadeContainer();
        using var installed = facades.Install();
        var activity = facades.Get<ICombatActivityService>();
        var forwarder = typeof(CombatFacade).GetProperty("_recentDamageTargets", Statics)
                        ?? throw new AssertionException("Combat has no _recentDamageTargets forwarder.");

        forwarder.GetMethod!.IsPrivate.Should().BeTrue();
        forwarder.SetMethod.Should().BeNull("the hit effects and damage taken parts only read and prune the table");
        var table = forwarder.GetValue(null);
        table.Should().BeSameAs(activity.RecentDamageTargets);
        table.Should().BeSameAs(typeof(CombatActivityService)
            .GetField("_recentDamageTargets", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(activity));
        forwarder.GetValue(null).Should().BeSameAs(table, "every read returns the same table, so a prune is seen by the next read");
    }

    [TestCase("PurgeInvalidOffensiveActivity", ScriptName.OnModuleHeartbeat)]
    [TestCase("RestoreIdleReadinessOnPlayerEnter", ScriptName.OnModuleEnter)]
    public void TheActivityHandlersStayOnCombatUnderTheirNames(string name, string script)
    {
        var handler = typeof(CombatFacade).GetMethod(name, BindingFlags.Public | BindingFlags.Static, Type.EmptyTypes)!;
        handler.GetCustomAttributes<NWNEventHandler>().Select(attribute => attribute.Script).Should().Equal(script);
        typeof(CombatActivityService).GetMethod(name)!.GetCustomAttributes<NWNEventHandler>().Should()
            .BeEmpty("handlers stay on the facade");
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
        // A delegate (the status factory of GetAbilityStatusDurationPercentAdjustment) cannot be created uninitialized; a
        // compiled lambda of its type is an instance the proxy can compare by reference.
        if (typeof(Delegate).IsAssignableFrom(type))
        {
            var invoke = type.GetMethod("Invoke")!;
            var parameters = invoke.GetParameters().Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name)).ToArray();
            return Expression.Lambda(type, Expression.Default(invoke.ReturnType), parameters).Compile();
        }

        // A native wrapper (CNWSCreature) must never reach its finalizer in a process without the engine.
        var sample = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
        GC.SuppressFinalize(sample);
        return sample;
    }
}
