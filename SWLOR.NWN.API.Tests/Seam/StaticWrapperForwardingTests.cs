using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using SWLOR.NWN.API.Tests.Support;
using Wrappers = SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.NWN.API.Tests.Seam;

/// <summary>
/// The behaviour-neutral proof for the NWScript seam: every static wrapper has a matching member on INWScript, and calling
/// the wrapper makes exactly one call on the installed engine with the same arguments, returning what the engine returned.
/// </summary>
[TestFixture]
public class StaticWrapperForwardingTests
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static IReadOnlyList<MethodInfo> StaticMethods { get; } = typeof(Wrappers)
        .GetMethods(Declared)
        .Where(m => !m.IsSpecialName)
        .OrderBy(m => m.Name, StringComparer.Ordinal)
        .ThenBy(m => m.GetParameters().Length)
        .ToList();

    [Test]
    public void StaticWrappers_AreMethodsTheObjectSelfPropertyAndConstants()
    {
        var unexpected = new List<string>();
        unexpected.AddRange(typeof(Wrappers).GetProperties(Declared).Where(p => p.Name != "OBJECT_SELF").Select(p => "property " + p.Name));
        unexpected.AddRange(typeof(Wrappers).GetFields(Declared).Where(f => !f.IsLiteral).Select(f => "field " + f.Name));
        unexpected.AddRange(typeof(Wrappers).GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).Select(t => "nested type " + t.Name));
        unexpected.AddRange(typeof(Wrappers).GetMembers(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m is not ConstructorInfo and not Type && !m.Name.StartsWith('<'))
            .Select(m => "non-public member " + m.Name));

        unexpected.Should().BeEmpty("a static wrapper member the seam does not cover would bypass the engine interface");
    }

    [Test]
    public void EveryStaticMethod_HasAMatchingInterfaceMember()
    {
        var problems = new List<string>();
        foreach (var method in StaticMethods)
        {
            var parameters = method.GetParameters();
            var member = typeof(INWScript).GetMethod(method.Name, parameters.Select(p => p.ParameterType).ToArray());
            if (member == null)
            {
                problems.Add($"{method.Name}: no interface member with the same parameter types");
                continue;
            }

            if (member.ReturnType != method.ReturnType) problems.Add($"{method.Name}: return type {member.ReturnType.Name} != {method.ReturnType.Name}");

            var interfaceParameters = member.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var wrapper = parameters[i];
                var engine = interfaceParameters[i];
                if (wrapper.Name != engine.Name) problems.Add($"{method.Name}: parameter {i} is named {engine.Name}, wrapper has {wrapper.Name}");
                if (wrapper.HasDefaultValue != engine.HasDefaultValue ||
                    (wrapper.HasDefaultValue && !Equals(wrapper.RawDefaultValue, engine.RawDefaultValue)))
                {
                    problems.Add($"{method.Name}: parameter {wrapper.Name} has a different default value");
                }

                if (wrapper.IsDefined(typeof(ParamArrayAttribute)) != engine.IsDefined(typeof(ParamArrayAttribute)))
                {
                    problems.Add($"{method.Name}: parameter {wrapper.Name} differs in `params`");
                }
            }
        }

        problems.Should().BeEmpty();
    }

    [Test]
    public void EveryInterfaceMember_HasAStaticWrapper()
    {
        var staticKeys = StaticMethods.Select(Key).ToHashSet();
        var interfaceMethods = typeof(INWScript).GetMethods().Where(m => !m.IsSpecialName).ToList();
        var orphans = interfaceMethods.Where(m => !staticKeys.Contains(Key(m))).Select(m => m.Name).ToList();
        orphans.Should().BeEmpty();

        typeof(INWScript).GetProperties().Select(p => p.Name).Should().BeEquivalentTo("OBJECT_SELF");
        interfaceMethods.Should().HaveCount(StaticMethods.Count);
    }

    [Test]
    public void EveryStaticMethod_ForwardsOnceToTheEngine_WithTheSameArgumentsAndReturnValue()
    {
        var problems = new List<string>();
        foreach (var method in StaticMethods)
        {
            var fake = new FakeNWScript();
            using var scope = NWScriptRuntime.Override(fake);

            var parameters = method.GetParameters();
            var arguments = parameters.Select((p, i) => SampleValues.For(p.ParameterType, i + 1)).ToArray();
            var returned = SampleValues.For(method.ReturnType, 50);
            if (method.ReturnType != typeof(void)) fake.SetReturn(method.Name, returned);

            object? result;
            try
            {
                result = method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                problems.Add($"{method.Name}: threw {exception.InnerException?.GetType().Name}: {exception.InnerException?.Message}");
                continue;
            }

            var calls = fake.Calls;
            if (calls.Count != 1)
            {
                problems.Add($"{method.Name}: made {calls.Count} engine calls instead of one");
                continue;
            }

            var call = calls[0];
            if (call.Member != method.Name) problems.Add($"{method.Name}: called {call.Member} on the engine");
            if (call.Arguments.Count != arguments.Length)
            {
                problems.Add($"{method.Name}: passed {call.Arguments.Count} arguments instead of {arguments.Length}");
            }
            else
            {
                for (var i = 0; i < arguments.Length; i++)
                {
                    if (!Equals(call.Arguments[i], arguments[i])) problems.Add($"{method.Name}: argument {parameters[i].Name} arrived as {call.Arguments[i]} instead of {arguments[i]}");
                }
            }

            if (method.ReturnType != typeof(void) && !Equals(result, returned))
            {
                problems.Add($"{method.Name}: returned {result} instead of the engine's {returned}");
            }
        }

        problems.Should().BeEmpty();
    }

    [Test]
    public void ObjectSelf_ForwardsToTheEngine()
    {
        var fake = new FakeNWScript();
        fake.SetReturn("OBJECT_SELF", 4242u);
        using var scope = NWScriptRuntime.Override(fake);

        Wrappers.OBJECT_SELF.Should().Be(4242u);

        fake.Calls.Should().ContainSingle().Which.Member.Should().Be("OBJECT_SELF");
    }

    [Test]
    public void OverloadedWrappers_ReachTheirOwnOverload()
    {
        var fake = new FakeNWScript();
        using var scope = NWScriptRuntime.Override(fake);
        var effect = EngineStructures.Create<SWLOR.NWN.API.Engine.Effect>(9);

        Wrappers.RemoveEffect(1000u, effect);
        Wrappers.RemoveEffect(1000u, SWLOR.NWN.API.NWScript.Enum.EffectTypeScript.Regenerate);

        fake.CallsTo("RemoveEffect").Select(c => c.Arguments[1]).Should().HaveCount(2);
        fake.CallsTo("RemoveEffect")[0].Arguments[1].Should().BeSameAs(effect);
        fake.CallsTo("RemoveEffect")[1].Arguments[1].Should().BeOfType<SWLOR.NWN.API.NWScript.Enum.EffectTypeScript[]>();
    }

    private static string Key(MethodInfo method) => method.Name + "(" + string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName)) + ")";
}
