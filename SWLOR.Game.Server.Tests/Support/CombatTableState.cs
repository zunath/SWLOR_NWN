#nullable enable
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Fills and reads the tables a service keeps by creature (the dictionaries and sets of its private fields) so a transcript can
/// show a cleanup's effect on them. Keys are built from the creature ids the transcript gives; a table is named by its field, so
/// a table keeps its name when a split moves it to another service. Values are empty instances: the cleanups remove entries by key.
/// </summary>
public static class CombatTableState
{
    /// <summary>
    /// Adds entries to every creature-keyed dictionary and set of <paramref name="services"/>, naming each creature in
    /// <paramref name="entries"/>: a plain key for each creature, a pair for each neighbouring two (the first one the owner, then reversed).
    /// Returns the names of the fields it filled, in field-name order.
    /// </summary>
    public static IReadOnlyList<string> Populate(IEnumerable<object> services, params uint[] creatures)
    {
        var filled = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var field in Fields(services))
        {
            var collection = field.Value;
            if (!TryKeys(field.Type, creatures, out var keys))
                continue;

            foreach (var key in keys)
            {
                if (collection is IDictionary dictionary)
                {
                    var valueType = field.Type.GetGenericArguments()[1];
                    if (!dictionary.Contains(key) && TryValue(valueType, out var value))
                        dictionary.Add(key, value);
                }
                else
                {
                    field.Type.GetMethod("Add")!.Invoke(collection, new[] { key });
                }
            }

            filled.Add(field.Name);
        }

        return filled.ToList();
    }

    /// <summary>Writes "field: [key, key]" for every non-empty creature-keyed table, in field-name order, one line each.</summary>
    public static IEnumerable<string> Dump(IEnumerable<object> services, Func<object?, string> format)
    {
        foreach (var field in Fields(services).OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            if (!TryKeys(field.Type, new uint[] { 1, 2 }, out _))
                continue;

            var keys = field.Value is IDictionary dictionary
                ? dictionary.Keys.Cast<object?>()
                : ((IEnumerable)field.Value).Cast<object?>();
            var names = keys.Select(format).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            yield return $"table {field.Name}: [{string.Join(", ", names)}]";
        }
    }

    /// <summary>
    /// Writes the named fields of <paramref name="services"/> (a field is found by name on whichever service holds it) as
    /// "name: value" lines in the order of <paramref name="names"/>: dictionaries as sorted "key: value" lists, stacks and lists
    /// in order, tuples item by item, an empty field as "name: empty".
    /// </summary>
    public static IEnumerable<string> DumpFields(IEnumerable<object> services, IReadOnlyList<string> names, Func<object?, string> format)
    {
        var all = services.Distinct().ToList();
        foreach (var name in names)
        {
            object? value = null;
            foreach (var service in all)
            {
                for (var type = service.GetType(); type != null && value == null; type = type.BaseType)
                    value = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)?.GetValue(service);
            }

            yield return value == null ? $"{name}: not held" : $"{name}: {Describe(value, format)}";
        }
    }

    private static string Describe(object? value, Func<object?, string> format)
    {
        switch (value)
        {
            case null:
                return "null";
            case IDictionary dictionary:
                if (dictionary.Count == 0)
                    return "empty";
                var entries = new List<string>();
                foreach (DictionaryEntry entry in dictionary)
                    entries.Add($"{Describe(entry.Key, format)}: {Describe(entry.Value, format)}");
                entries.Sort(StringComparer.Ordinal);
                return "{" + string.Join(", ", entries) + "}";
            case ITuple tuple:
                return "(" + string.Join(", ", Enumerable.Range(0, tuple.Length).Select(index => Describe(tuple[index], format))) + ")";
            case string text:
                return text;
            case DateTime moment:
                return moment.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            case IEnumerable sequence:
                var items = sequence.Cast<object?>().Select(item => Describe(item, format)).ToList();
                return items.Count == 0 ? "empty" : "[" + string.Join(", ", items) + "]";
            default:
                return format(value);
        }
    }

    private static IEnumerable<(string Name, Type Type, object Value)> Fields(IEnumerable<object> services)
    {
        foreach (var service in services.Distinct())
        {
            for (var type = service.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.Name.Contains('<') || field.GetValue(service) is not { } value)
                        continue;
                    var fieldType = field.FieldType;
                    if (!fieldType.IsGenericType)
                        continue;
                    var definition = fieldType.GetGenericTypeDefinition();
                    if (definition == typeof(Dictionary<,>) || definition == typeof(HashSet<>))
                        yield return (field.Name, fieldType, value);
                }
            }
        }
    }

    // The keys a creature-keyed table can hold: a creature, a pair of creatures, or a creature with a value that defaults.
    private static bool TryKeys(Type tableType, uint[] creatures, out List<object> keys)
    {
        keys = new List<object>();
        var keyType = tableType.GetGenericArguments()[0];
        if (keyType == typeof(uint))
        {
            keys.AddRange(creatures.Select(c => (object)c));
            return true;
        }

        if (!keyType.IsGenericType || !typeof(ITuple).IsAssignableFrom(keyType))
            return false;

        var parts = keyType.GetGenericArguments();
        if (parts[0] != typeof(uint))
            return false;
        if (parts.Skip(1).Any(part => part != typeof(uint) && !part.IsEnum && part.IsValueType == false && part.Name != "AbilityDetail"))
            return false;

        for (var index = 0; index + 1 < creatures.Length; index++)
        {
            keys.Add(Tuple(keyType, parts, creatures[index], creatures[index + 1]));
            keys.Add(Tuple(keyType, parts, creatures[index + 1], creatures[index]));
        }

        if (creatures.Length == 1)
            keys.Add(Tuple(keyType, parts, creatures[0], creatures[0]));
        return true;
    }

    private static object Tuple(Type keyType, Type[] parts, uint first, uint second)
    {
        var arguments = new object?[parts.Length];
        arguments[0] = first;
        for (var index = 1; index < parts.Length; index++)
            arguments[index] = parts[index] == typeof(uint) ? second : parts[index].IsValueType ? Activator.CreateInstance(parts[index]) : null;
        return keyType.GetConstructor(parts)!.Invoke(arguments);
    }

    private static bool TryValue(Type type, out object? value)
    {
        value = null;
        if (type.IsValueType)
        {
            value = Activator.CreateInstance(type);
            return true;
        }

        if (type == typeof(string))
        {
            value = string.Empty;
            return true;
        }

        var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
        value = constructor != null ? constructor.Invoke(null) : RuntimeHelpers.GetUninitializedObject(type);
        return true;
    }
}
