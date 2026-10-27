using System.Reflection;
using SWLOR.NWN.API.Testing;

namespace SWLOR.NWN.API.Tests.Support;

/// <summary>
/// The behaviour-neutral checks for one static wrapper class and its engine interface: each static method has a matching
/// interface member, and calling it makes exactly one call on the installed fake with the same arguments, returning what
/// the fake returned. Each check returns the problems it found; an empty list means it passed.
/// </summary>
internal static class ForwardingProof
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static IReadOnlyList<MethodInfo> StaticMethods(Type wrapper)
    {
        return wrapper.GetMethods(Declared)
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name, StringComparer.Ordinal)
            .ThenBy(m => string.Join(",", m.GetParameters().Select(p => p.ParameterType.FullName)), StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Anything on the wrapper other than methods and data-only nested types would bypass the seam.</summary>
    public static List<string> UncoveredMembers(Type wrapper)
    {
        var unexpected = new List<string>();
        unexpected.AddRange(wrapper.GetProperties(Declared).Select(p => "property " + p.Name));
        unexpected.AddRange(wrapper.GetFields(Declared).Where(f => !f.IsLiteral).Select(f => "field " + f.Name));
        unexpected.AddRange(wrapper.GetMembers(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m is not ConstructorInfo and not Type && !m.Name.StartsWith('<'))
            .Select(m => "non-public member " + m.Name));
        unexpected.AddRange(wrapper.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(m => "instance method " + m.Name));

        foreach (var nested in wrapper.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!nested.IsNestedPublic) unexpected.Add("non-public nested type " + nested.Name);

            // A nested type stays in the static class, so it must not be able to run engine code.
            var code = nested.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName)
                .Select(m => $"{nested.Name}.{m.Name}");
            unexpected.AddRange(code.Select(name => "method on nested type " + name));
        }

        return unexpected;
    }

    public static List<string> SignatureProblems(Type wrapper, Type engineInterface)
    {
        var problems = new List<string>();
        foreach (var method in StaticMethods(wrapper))
        {
            var parameters = method.GetParameters();
            var member = engineInterface.GetMethod(method.Name, parameters.Select(p => p.ParameterType).ToArray());
            if (member == null)
            {
                problems.Add($"{method.Name}: no interface member with the same parameter types");
                continue;
            }

            if (member.ReturnType != method.ReturnType) problems.Add($"{method.Name}: return type {member.ReturnType.Name} != {method.ReturnType.Name}");

            var interfaceParameters = member.GetParameters();
            for (var i = 0; i < parameters.Length; i++)
            {
                var staticParameter = parameters[i];
                var engineParameter = interfaceParameters[i];
                if (staticParameter.Name != engineParameter.Name) problems.Add($"{method.Name}: parameter {i} is named {engineParameter.Name}, wrapper has {staticParameter.Name}");
                if (staticParameter.HasDefaultValue != engineParameter.HasDefaultValue ||
                    (staticParameter.HasDefaultValue && !Equals(staticParameter.RawDefaultValue, engineParameter.RawDefaultValue)))
                {
                    problems.Add($"{method.Name}: parameter {staticParameter.Name} has a different default value");
                }

                if (staticParameter.IsDefined(typeof(ParamArrayAttribute)) != engineParameter.IsDefined(typeof(ParamArrayAttribute)))
                {
                    problems.Add($"{method.Name}: parameter {staticParameter.Name} differs in `params`");
                }
            }
        }

        return problems;
    }

    /// <summary>Interface members without a static wrapper, and any interface property (the plugins have none).</summary>
    public static List<string> OrphanInterfaceMembers(Type wrapper, Type engineInterface)
    {
        var staticKeys = StaticMethods(wrapper).Select(Key).ToHashSet();
        var orphans = engineInterface.GetMethods().Where(m => !m.IsSpecialName && !staticKeys.Contains(Key(m))).Select(m => m.Name).ToList();
        orphans.AddRange(engineInterface.GetProperties().Select(p => "property " + p.Name));

        var methodCount = engineInterface.GetMethods().Count(m => !m.IsSpecialName);
        if (methodCount != StaticMethods(wrapper).Count) orphans.Add($"{methodCount} interface methods but {StaticMethods(wrapper).Count} static methods");

        return orphans;
    }

    /// <summary>Calls every static method once against a fresh fake and checks the one call that reached it.</summary>
    public static List<string> ForwardingProblems(Type wrapper, Func<(FakeEngine Fake, IDisposable Scope)> install)
    {
        var problems = new List<string>();
        foreach (var method in StaticMethods(wrapper))
        {
            var (fake, scope) = install();
            using (scope)
            {
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
                        if (!Equals(call.Arguments[i], arguments[i]))
                        {
                            problems.Add($"{method.Name}: argument {parameters[i].Name} arrived as {call.Arguments[i]} instead of {arguments[i]}");
                        }
                    }
                }

                if (method.ReturnType != typeof(void) && !Equals(result, returned))
                {
                    problems.Add($"{method.Name}: returned {result} instead of the engine's {returned}");
                }
            }
        }

        return problems;
    }

    private static string Key(MethodInfo method) => method.Name + "(" + string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName)) + ")";
}
