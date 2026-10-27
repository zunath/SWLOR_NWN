using System.Numerics;
using System.Reflection;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.Testing;

namespace SWLOR.NWN.API.Tests.Support;

/// <summary>
/// Builds a distinct, non-default sample value for any type the NWScript wrappers use in a signature, so a test can call
/// every wrapper and tell apart swapped or dropped arguments.
/// </summary>
internal static class SampleValues
{
    /// <summary>A sample of <paramref name="type"/>. Different seeds give different values where the type allows it.</summary>
    public static object? For(Type type, int seed)
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

        if (type.IsEnum)
        {
            var values = Enum.GetValues(type);
            return values.GetValue(seed % values.Length);
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            var array = Array.CreateInstance(elementType, 2);
            array.SetValue(For(elementType, seed), 0);
            array.SetValue(For(elementType, seed + 1), 1);
            return array;
        }

        if (type.IsGenericType && type.FullName!.StartsWith("System.ValueTuple", StringComparison.Ordinal))
        {
            var items = type.GenericTypeArguments.Select((t, i) => For(t, seed + i)).ToArray();
            return Activator.CreateInstance(type, items);
        }

        if (typeof(EngineStructureBase).IsAssignableFrom(type))
        {
            var create = typeof(EngineStructures).GetMethod(nameof(EngineStructures.Create))!.MakeGenericMethod(type);
            return create.Invoke(null, new object[] { (nint)(500 + seed) });
        }

        throw new NotSupportedException($"No sample value is defined for {type}. Add one to {nameof(SampleValues)}.");
    }
}
