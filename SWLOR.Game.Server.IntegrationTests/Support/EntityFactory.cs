using System.Collections;
using System.Reflection;
using Newtonsoft.Json;
using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.IntegrationTests.Support;

/// <summary>
/// Builds a populated instance of any entity type by reflection: every settable property gets a value that is not its
/// default (text, numbers, enums, dates, lists, dictionaries and nested objects, down to a depth of four), so a round trip
/// through Redis shows a property the serializer drops or changes. The values are small and fixed, so searches on the
/// indexed properties can find them again.
/// </summary>
public static class EntityFactory
{
    private const int MaxDepth = 4;

    /// <summary>Every concrete entity type the database stores, in name order.</summary>
    public static IReadOnlyList<Type> EntityTypes { get; } = typeof(EntityBase).Assembly.GetTypes()
        .Where(type => type.IsSubclassOf(typeof(EntityBase)) && !type.IsAbstract && !type.IsGenericType)
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .ToList();

    public static EntityBase Build(Type type, string id, int seed = 1)
    {
        var entity = (EntityBase)Activator.CreateInstance(type)!;
        Fill(entity, seed, 0);
        entity.Id = id;
        entity.DateCreated = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        entity.EntityType = type.Name;
        return entity;
    }

    public static T Build<T>(string id, int seed = 1)
        where T : EntityBase => (T)Build(typeof(T), id, seed);

    /// <summary>The stored form: what <c>JSON.SET</c> receives.</summary>
    public static string Json(EntityBase entity) => JsonConvert.SerializeObject(entity);

    private static void Fill(object target, int seed, int depth)
    {
        foreach (var property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0 || property.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                continue;

            // A collection the constructor filled keeps its defaults (a dictionary with a key per stat, say); the
            // factory adds to it, because replacing it would hide how the serializer merges stored entries into those defaults.
            if (property.CanRead && IsCollection(property.PropertyType) && property.GetValue(target) is IEnumerable current &&
                current.GetEnumerator().MoveNext())
            {
                AddElements(current, property.PropertyType, property.Name, seed, depth);
            }
            else if (property.CanWrite && property.SetMethod!.IsPublic)
            {
                var value = MakeValue(property.PropertyType, property.Name, seed, depth);
                if (value != null)
                    property.SetValue(target, value);
            }
            else if (property.CanRead && typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
            {
                // A getter-only collection the constructor created: add to it.
                var existing = property.GetValue(target);
                if (existing != null)
                    AddElements(existing, property.PropertyType, property.Name, seed, depth);
            }
        }
    }

    private static bool IsCollection(Type type) =>
        type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);

    private static object? MakeValue(Type type, string name, int seed, int depth)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying != null)
            return MakeValue(underlying, name, seed, depth);

        if (type == typeof(string)) return $"{name}-{seed}";
        if (type == typeof(bool)) return true;
        if (type == typeof(int)) return 7 + seed;
        if (type == typeof(long)) return 7L + seed;
        if (type == typeof(uint)) return (uint)(7 + seed);
        if (type == typeof(ulong)) return (ulong)(7 + seed);
        if (type == typeof(short)) return (short)(7 + seed);
        if (type == typeof(ushort)) return (ushort)(7 + seed);
        if (type == typeof(byte)) return (byte)(7 + seed);
        if (type == typeof(float)) return 1.5f + seed;
        if (type == typeof(double)) return 2.25d + seed;
        if (type == typeof(decimal)) return 3.5m + seed;
        if (type == typeof(DateTime)) return new DateTime(2026, 3, 4, 5, 6, 7, DateTimeKind.Utc).AddDays(seed);
        if (type == typeof(DateTimeOffset)) return new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero).AddDays(seed);
        if (type == typeof(TimeSpan)) return TimeSpan.FromSeconds(90 + seed);
        if (type == typeof(Guid)) return new Guid(seed, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11);
        if (type.IsEnum)
        {
            var values = Enum.GetValues(type).Cast<object>().Distinct().ToList();
            return values.Count == 0 ? null : values.FirstOrDefault(v => Convert.ToInt64(v) != 0) ?? values[0];
        }

        if (depth >= MaxDepth)
            return null;

        if (type.IsArray)
        {
            var element = type.GetElementType()!;
            var array = Array.CreateInstance(element, 2);
            for (var i = 0; i < 2; i++)
                array.SetValue(MakeValue(element, name, seed + i, depth + 1) ?? Default(element), i);
            return array;
        }

        if (type.IsInterface && type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();
            if (definition == typeof(IList<>) || definition == typeof(ICollection<>) || definition == typeof(IEnumerable<>) ||
                definition == typeof(IReadOnlyList<>))
                type = typeof(List<>).MakeGenericType(arguments);
            else if (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
                type = typeof(Dictionary<,>).MakeGenericType(arguments);
            else if (definition == typeof(ISet<>))
                type = typeof(HashSet<>).MakeGenericType(arguments);
        }

        if (type.IsAbstract || type.IsInterface || type.IsValueType || type == typeof(object))
            return null;

        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            return null;

        var instance = constructor.Invoke(null);
        if (instance is IEnumerable and not string)
            AddElements(instance, type, name, seed, depth);
        else
            Fill(instance, seed, depth + 1);
        return instance;
    }

    private static void AddElements(object collection, Type type, string name, int seed, int depth)
    {
        if (depth + 1 > MaxDepth)
            return;

        var dictionary = type.GetInterfaces().Concat(new[] { type })
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictionary != null && collection is IDictionary map)
        {
            var arguments = dictionary.GetGenericArguments();
            var keys = MakeKeys(arguments[0]);
            for (var i = 0; i < keys.Count; i++)
            {
                var value = MakeValue(arguments[1], name, seed + i, depth + 1);
                if (value != null || !arguments[1].IsValueType)
                    map[keys[i]] = value;
            }

            return;
        }

        var element = type.GetInterfaces().Concat(new[] { type })
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>))
            .Select(i => i.GetGenericArguments()[0])
            .FirstOrDefault();
        if (element == null)
            return;

        var add = typeof(ICollection<>).MakeGenericType(element).GetMethod("Add")!;
        for (var i = 0; i < 2; i++)
        {
            var value = MakeValue(element, name, seed + i, depth + 1);
            if (value != null)
                add.Invoke(collection, new[] { value });
        }
    }

    private static List<object> MakeKeys(Type keyType)
    {
        if (keyType == typeof(string)) return new List<object> { "key-a", "key-b" };
        if (keyType == typeof(int)) return new List<object> { 11, 12 };
        if (keyType == typeof(long)) return new List<object> { 11L, 12L };
        if (keyType.IsEnum)
            return Enum.GetValues(keyType).Cast<object>().Distinct().Where(v => Convert.ToInt64(v) != 0).Take(2).ToList();
        return new List<object>();
    }

    private static object? Default(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
}
