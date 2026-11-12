using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;

namespace SWLOR.Game.Server.Tests.Core;

/// <summary>
/// Builds a sample value of any carriable type (<see cref="ReloadStateCarrier.Problems"/>): one entry per collection, a fixed value
/// per scalar, an object of the game assembly with every field filled (an interface or abstract type as its first implementation by
/// name), to a bounded depth. The carry tests fill every carried field with it, so a new carried field is covered without a test of
/// its own.
/// </summary>
internal static class ReloadCarrySamples
{
    private const int MaxDepth = 5;

    public static readonly DateTime SampleTime = new(2026, 11, 12, 10, 30, 15, DateTimeKind.Utc);

    public static object Create(Type type, int depth = 0)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
            return Create(underlying, depth);

        if (type == typeof(bool)) return true;
        if (type == typeof(string)) return "sample";
        if (type == typeof(char)) return 'c';
        if (type == typeof(float)) return 1.5f;
        if (type == typeof(double)) return 2.5d;
        if (type == typeof(decimal)) return 3.5m;
        if (type.IsPrimitive) return Convert.ChangeType(7, type);
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.Length > 1 ? values.GetValue(1) : values.GetValue(0);
        }

        if (type == typeof(DateTime)) return SampleTime;
        if (type == typeof(DateTimeOffset)) return new DateTimeOffset(SampleTime);
        if (type == typeof(TimeSpan)) return TimeSpan.FromSeconds(5);
        if (type == typeof(Guid)) return new Guid("6f1e2d3c-4b5a-4968-8776-5a4b3c2d1e0f");
        if (type == typeof(Vector3)) return new Vector3(1, 2, 3);
        if (type == typeof(Vector2)) return new Vector2(1, 2);
        if (typeof(Type).IsAssignableFrom(type)) return typeof(ReloadCarry);
        if (typeof(Delegate).IsAssignableFrom(type) || type == typeof(AbilityDetail)) return null;

        if (type.IsArray)
        {
            var array = Array.CreateInstance(type.GetElementType()!, 1);
            array.SetValue(Create(type.GetElementType()!, depth + 1), 0);
            return array;
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();
            if (definition.FullName!.StartsWith("System.ValueTuple`", StringComparison.Ordinal) || definition == typeof(KeyValuePair<,>))
                return Activator.CreateInstance(type, arguments.Select(argument => Create(argument, depth + 1)).ToArray());

            var concrete = ConcreteCollection(definition, arguments);
            if (concrete != null)
            {
                var collection = Activator.CreateInstance(concrete)!;
                if (depth >= MaxDepth)
                    return collection;
                if (collection is IDictionary dictionary)
                    dictionary.Add(Create(arguments[0], depth + 1)!, Create(arguments[1], depth + 1));
                else
                    (concrete.GetMethod("Add") ?? concrete.GetMethod("Enqueue"))!.Invoke(collection, new[] { Create(arguments[0], depth + 1) });
                return collection;
            }
        }

        if (type.Assembly != typeof(ReloadStateCarrier).Assembly || depth >= MaxDepth)
            return null;

        if (type.IsInterface || type.IsAbstract)
        {
            var implementation = type.Assembly.GetTypes()
                .Where(candidate => !candidate.IsInterface && !candidate.IsAbstract && !candidate.IsGenericTypeDefinition &&
                                    type.IsAssignableFrom(candidate))
                .OrderBy(candidate => candidate.FullName, StringComparer.Ordinal)
                .FirstOrDefault();
            return implementation == null ? null : Create(implementation, depth);
        }

        var instance = RuntimeHelpers.GetUninitializedObject(type);
        foreach (var field in ReloadCarryContractResolver.InstanceFields(type))
            field.SetValue(instance, Create(field.FieldType, depth + 1));
        return instance;
    }

    /// <summary>An empty instance of the field's own collection type, or null when it is not a concrete collection.</summary>
    public static object EmptyCollection(Type type)
    {
        if (!type.IsGenericType || type.IsInterface || type.IsAbstract)
            return null;

        var concrete = ConcreteCollection(type.GetGenericTypeDefinition(), type.GetGenericArguments());
        return concrete == type ? Activator.CreateInstance(type) : null;
    }

    private static Type ConcreteCollection(Type definition, Type[] arguments)
    {
        if (definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
            return typeof(Dictionary<,>).MakeGenericType(arguments);
        if (definition == typeof(SortedDictionary<,>))
            return typeof(SortedDictionary<,>).MakeGenericType(arguments);
        if (definition == typeof(HashSet<>) || definition == typeof(ISet<>))
            return typeof(HashSet<>).MakeGenericType(arguments);
        if (definition == typeof(SortedSet<>))
            return typeof(SortedSet<>).MakeGenericType(arguments);
        if (definition == typeof(Queue<>))
            return typeof(Queue<>).MakeGenericType(arguments);
        if (definition == typeof(List<>) || definition == typeof(IList<>) || definition == typeof(IReadOnlyList<>) ||
            definition == typeof(ICollection<>) || definition == typeof(IReadOnlyCollection<>) || definition == typeof(IEnumerable<>))
            return typeof(List<>).MakeGenericType(arguments);
        return null;
    }

    /// <summary>The instance a carried class's instance fields live on in a test: the class without its constructor.</summary>
    public static object Owner(Type type, IReadOnlyList<FieldInfo> fields)
    {
        return fields.All(field => field.IsStatic) ? null : RuntimeHelpers.GetUninitializedObject(type);
    }
}
