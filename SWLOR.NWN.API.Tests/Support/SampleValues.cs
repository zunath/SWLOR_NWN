using System.Numerics;
using System.Reflection;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.Testing;

namespace SWLOR.NWN.API.Tests.Support;

/// <summary>
/// Builds a distinct, non-default sample value for any type the NWScript and NWNX plugin wrappers use in a signature, so a test can call
/// every wrapper and tell apart swapped or dropped arguments.
/// </summary>
internal static class SampleValues
{
    /// <summary>A sample of <paramref name="type"/>. Different seeds give different values where the type allows it.</summary>
    public static object? For(Type type, int seed) => For(type, seed, 0);

    private static object? For(Type type, int seed, int depth)
    {
        if (type == typeof(void)) return null;
        if (type == typeof(uint)) return 1000u + (uint)seed;
        if (type == typeof(int)) return 10 + seed;
        if (type == typeof(float)) return 1.5f + seed;
        if (type == typeof(bool)) return seed % 2 == 1;
        if (type == typeof(string)) return $"sample{seed}";
        if (type == typeof(nint)) return (nint)(100 + seed);
        if (type == typeof(Vector3)) return new Vector3(seed + 1, seed + 2, seed + 3);
        if (type == typeof(Action)) return new Action(() => { });

        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null) return For(underlying, seed, depth);

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(seed % values.Length);
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            var array = Array.CreateInstance(elementType, 2);
            array.SetValue(For(elementType, seed, depth), 0);
            array.SetValue(For(elementType, seed + 1, depth), 1);
            return array;
        }

        if (type.IsGenericType && type.FullName!.StartsWith("System.ValueTuple", StringComparison.Ordinal))
        {
            var items = type.GenericTypeArguments.Select((t, i) => For(t, seed + i, depth)).ToArray();
            return Activator.CreateInstance(type, items);
        }

        if (typeof(EngineStructureBase).IsAssignableFrom(type))
        {
            var create = typeof(EngineStructures).GetMethod(nameof(EngineStructures.Create))!.MakeGenericMethod(type);
            return create.Invoke(null, new object[] { (nint)(500 + seed) });
        }

        // Plain data: a struct, or a class with a public parameterless constructor, with every settable public field and
        // property filled with a distinct sample (the NWNX plugins pass and return such types).
        if (depth < 3 && (type.IsValueType || (type.IsClass && type.GetConstructor(Type.EmptyTypes) != null)))
        {
            var instance = Activator.CreateInstance(type)!;
            var index = 0;
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(f => !f.IsInitOnly))
            {
                field.SetValue(instance, For(field.FieldType, seed + ++index, depth + 1));
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.SetMethod?.IsPublic == true && p.GetIndexParameters().Length == 0))
            {
                property.SetValue(instance, For(property.PropertyType, seed + ++index, depth + 1));
            }

            return instance;
        }

        throw new NotSupportedException($"No sample value is defined for {type}. Add one to {nameof(SampleValues)}.");
    }
}
