using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Reads the constructor graph of a service collection for <see cref="LazyDependencyCycleTests"/>: a service whose
/// constructor takes <c>Lazy&lt;IXService&gt;</c> must need it, because taking <c>IXService</c> directly would make a
/// constructor cycle (decision D3 of <c>Readmes/MaintainabilityRefactorFacadeRemoval.md</c>).
/// </summary>
public static class LazyDependencyCycles
{
    /// <summary>
    /// One message per <c>Lazy&lt;T&gt;</c> constructor parameter whose <c>T</c> would make no cycle if it were direct: the
    /// class registered for <c>T</c> does not reach the class that takes it through direct constructor parameters.
    /// </summary>
    public static IReadOnlyList<string> NeedlessLazies(IServiceCollection services)
    {
        var classes = services
            .Where(descriptor => !descriptor.ServiceType.IsGenericTypeDefinition && descriptor.ImplementationType != null)
            .GroupBy(descriptor => descriptor.ServiceType)
            .ToDictionary(group => group.Key, group => group.Last().ImplementationType!);

        var problems = new List<string>();
        foreach (var owner in classes.Values.Distinct())
        {
            foreach (var parameter in ParametersOf(owner))
            {
                if (!parameter.ParameterType.IsGenericType || parameter.ParameterType.GetGenericTypeDefinition() != typeof(Lazy<>))
                    continue;

                var target = parameter.ParameterType.GetGenericArguments()[0];
                if (!classes.TryGetValue(target, out var targetClass))
                {
                    problems.Add($"{owner.Name} takes Lazy<{target.Name}>, which the container does not register.");
                    continue;
                }

                if (!Reaches(targetClass, owner, classes, new HashSet<Type>()))
                {
                    problems.Add($"{owner.Name} takes Lazy<{target.Name}>, but {targetClass.Name} never reaches {owner.Name} through its constructor, " +
                                 $"so a direct {target.Name} parameter makes no cycle: take {target.Name} directly.");
                }
            }
        }

        return problems.OrderBy(problem => problem, StringComparer.Ordinal).ToList();
    }

    /// <summary>The direct (not lazy) constructor dependencies of a class that the collection registers.</summary>
    public static IEnumerable<Type> DirectDependencies(Type type, IReadOnlyDictionary<Type, Type> classes) =>
        ParametersOf(type)
            .Select(parameter => classes.TryGetValue(parameter.ParameterType, out var dependency) ? dependency : null)
            .OfType<Type>();

    private static bool Reaches(Type from, Type to, IReadOnlyDictionary<Type, Type> classes, HashSet<Type> seen)
    {
        if (from == to)
            return true;
        if (!seen.Add(from))
            return false;

        return DirectDependencies(from, classes).Any(next => Reaches(next, to, classes, seen));
    }

    // The container uses the public constructor with the most parameters it can satisfy; the services have one.
    private static ParameterInfo[] ParametersOf(Type type) =>
        type.GetConstructors().OrderByDescending(constructor => constructor.GetParameters().Length).FirstOrDefault()?.GetParameters() ?? [];
}
