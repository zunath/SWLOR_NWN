using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// A content hash of an object graph that is the same in two builds of the same code, so a cache rebuilt by a hot reload can
    /// be compared with the one the boot built: values by content (strings, numbers, enum names, GUIDs), objects of the game
    /// assembly, tuples and key-value pairs by their fields, lists in order, dictionaries and sets in the order of their entries'
    /// hashes, delegates by their method's name. It leaves out what differs between two equal builds or over time: object
    /// identity, times of day (<see cref="DateTime"/>, <see cref="DateTimeOffset"/>), pointers, other services (an interface-typed
    /// field), fields marked <see cref="ReloadStateAttribute"/> other than the kind asked for, and the inside of foreign
    /// objects (a Redis connection, an engine location), which count by type name only. Cycles, depth and size are bounded the
    /// same way every time, so the hash stays comparable. Used only when hot reload is on.
    /// </summary>
    public sealed class ReloadDigestHasher
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;
        private const int MaxDepth = 12;
        private const long NodeBudget = 20_000_000;

        private readonly Assembly _gameAssembly;
        private readonly List<object> _path = new();
        private long _nodes;

        /// <param name="gameAssembly">The assembly whose objects are hashed by their fields.</param>
        public ReloadDigestHasher(Assembly gameAssembly)
        {
            _gameAssembly = gameAssembly ?? throw new ArgumentNullException(nameof(gameAssembly));
        }

        /// <summary>
        /// The digest of the fields of <paramref name="type"/> that <paramref name="include"/> selects: its static fields and,
        /// when <paramref name="instance"/> is given, its instance fields (with those of its base classes).
        /// </summary>
        public ReloadDigestLine Describe(string kind, string name, Type type, object instance, Func<FieldInfo, bool> include)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (include == null)
                throw new ArgumentNullException(nameof(include));

            _nodes = 0;
            _path.Clear();
            var hash = Offset;
            long count = 0;
            foreach (var field in RootFields(type, instance).Where(include))
            {
                hash = Mix(hash, field.DeclaringType?.Name + "." + field.Name);
                if (IsDependency(field.FieldType))
                {
                    hash = Mix(hash, Leaf("dep", field.FieldType.FullName));
                    continue;
                }

                var value = field.GetValue(field.IsStatic ? null : instance);
                hash = Mix(hash, HashValue(value, 0));
                count += CountOf(value);
            }

            return new ReloadDigestLine(kind, name, count, hash.ToString("x16", CultureInfo.InvariantCulture));
        }

        /// <summary>The hash of one value, as <see cref="Describe"/> hashes a field's value.</summary>
        public ulong Hash(object value)
        {
            _nodes = 0;
            _path.Clear();
            return HashValue(value, 0);
        }

        /// <summary>The kind <paramref name="field"/> carries in its <see cref="ReloadStateAttribute"/>, or null when it has none.</summary>
        public static ReloadStateKind? StateOf(FieldInfo field)
        {
            return field.GetCustomAttribute<ReloadStateAttribute>(false)?.Kind;
        }

        /// <summary>
        /// The fields <see cref="Describe"/> reads of <paramref name="type"/>: its own static fields (not constants) and, when
        /// <paramref name="instance"/> is given, the instance fields of its class and base classes, in a fixed order.
        /// </summary>
        public static IEnumerable<FieldInfo> RootFields(Type type, object instance)
        {
            var fields = type
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(field => !field.IsLiteral)
                .ToList();
            if (instance != null)
                fields.AddRange(InstanceFields(instance.GetType()));

            return fields
                .OrderBy(field => field.DeclaringType?.FullName, StringComparer.Ordinal)
                .ThenBy(field => field.IsStatic)
                .ThenBy(field => field.Name, StringComparer.Ordinal);
        }

        private static IEnumerable<FieldInfo> InstanceFields(Type type)
        {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    yield return field;
            }
        }

        private static long CountOf(object value)
        {
            switch (value)
            {
                case null:
                    return 0;
                case string:
                    return 1;
                case ICollection collection:
                    return collection.Count;
            }

            var readOnly = value.GetType().GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>));
            return readOnly != null && readOnly.GetProperty(nameof(IReadOnlyCollection<int>.Count))?.GetValue(value) is int count ? count : 1;
        }

        private ulong HashValue(object value, int depth)
        {
            if (value == null)
                return Leaf("null", string.Empty);
            if (++_nodes > NodeBudget)
                return Leaf("budget", string.Empty);

            var type = value.GetType();
            switch (value)
            {
                case string text:
                    return Leaf("s", text);
                case Enum:
                    return Leaf("e:" + type.FullName, value.ToString());
                case bool or char or sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal:
                    return Leaf("p:" + type.Name, Convert.ToString(value, CultureInfo.InvariantCulture));
                case DateTime or DateTimeOffset:
                    return Leaf("time", string.Empty);
                case TimeSpan span:
                    return Leaf("ts", span.Ticks.ToString(CultureInfo.InvariantCulture));
                case Guid guid:
                    return Leaf("g", guid.ToString("D"));
                case IntPtr or UIntPtr:
                    return Leaf("ptr", string.Empty);
                case Type described:
                    return Leaf("t", described.FullName);
                case Delegate callback:
                    return HashDelegate(callback);
            }

            if (depth >= MaxDepth)
                return Leaf("deep", type.FullName);

            if (!type.IsValueType)
            {
                foreach (var ancestor in _path)
                {
                    if (ReferenceEquals(ancestor, value))
                        return Leaf("cycle", type.FullName);
                }

                _path.Add(value);
            }

            try
            {
                return HashComposite(value, type, depth);
            }
            finally
            {
                if (!type.IsValueType)
                    _path.RemoveAt(_path.Count - 1);
            }
        }

        private ulong HashComposite(object value, Type type, int depth)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Lazy<>))
            {
                var created = (bool)type.GetProperty(nameof(Lazy<int>.IsValueCreated)).GetValue(value);
                return created ? Mix(Leaf("lazy", "1"), HashValue(type.GetProperty(nameof(Lazy<int>.Value)).GetValue(value), depth + 1)) : Leaf("lazy", "0");
            }

            if (value is IDictionary dictionary)
                return HashPairs(Entries(dictionary), depth);

            if (value is IEnumerable sequence && IsCollection(type))
            {
                if (IsKeyValueSequence(type, out var keyProperty, out var valueProperty))
                    return HashPairs(sequence.Cast<object>().Select(pair => (keyProperty.GetValue(pair), valueProperty.GetValue(pair))), depth);

                var elements = sequence.Cast<object>().Select(element => HashValue(element, depth + 1));
                if (IsUnordered(type))
                    elements = elements.OrderBy(hash => hash);

                var hash = Leaf("seq", IsUnordered(type) ? "set" : "list");
                long length = 0;
                foreach (var element in elements.ToList())
                {
                    hash = Mix(hash, element);
                    length++;
                }

                return Mix(hash, (ulong)length);
            }

            if (type.Assembly == _gameAssembly || IsStructural(type))
                return HashFields(value, type, depth);

            return Leaf("x", type.FullName);
        }

        private static IEnumerable<(object Key, object Value)> Entries(IDictionary dictionary)
        {
            var entries = dictionary.GetEnumerator();
            while (entries.MoveNext())
                yield return (entries.Key, entries.Value);
        }

        private ulong HashPairs(IEnumerable<(object Key, object Value)> pairs, int depth)
        {
            var hashed = pairs
                .Select(pair => (Key: HashValue(pair.Key, depth + 1), Value: HashValue(pair.Value, depth + 1)))
                .OrderBy(pair => pair.Key)
                .ThenBy(pair => pair.Value)
                .ToList();

            var hash = Leaf("map", string.Empty);
            foreach (var (key, entry) in hashed)
                hash = Mix(Mix(hash, key), entry);
            return Mix(hash, (ulong)hashed.Count);
        }

        private ulong HashFields(object value, Type type, int depth)
        {
            var hash = Leaf("obj", type.FullName);
            foreach (var field in InstanceFields(type)
                         .Where(field => StateOf(field) == null)
                         .OrderBy(field => field.DeclaringType?.FullName, StringComparer.Ordinal)
                         .ThenBy(field => field.Name, StringComparer.Ordinal))
            {
                hash = Mix(hash, field.Name);
                hash = Mix(hash, IsDependency(field.FieldType)
                    ? Leaf("dep", field.FieldType.FullName)
                    : HashValue(field.GetValue(value), depth + 1));
            }

            return hash;
        }

        private static ulong HashDelegate(Delegate callback)
        {
            var hash = Leaf("fn", string.Empty);
            foreach (var invocation in callback.GetInvocationList())
                hash = Mix(hash, invocation.Method.DeclaringType?.FullName + "." + invocation.Method.Name);
            return hash;
        }

        /// <summary>
        /// A field that holds another service (an interface that is not a collection, or a <see cref="Lazy{T}"/> of one): its
        /// content belongs to that service's own digest, so only its declared type counts here.
        /// </summary>
        public static bool IsDependency(Type declared)
        {
            if (declared.IsGenericType && declared.GetGenericTypeDefinition() == typeof(Lazy<>))
                declared = declared.GetGenericArguments()[0];

            return declared.IsInterface && !typeof(IEnumerable).IsAssignableFrom(declared);
        }

        private static bool IsCollection(Type type)
        {
            return type.IsArray || typeof(ICollection).IsAssignableFrom(type) || type.GetInterfaces()
                .Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                                              i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)));
        }

        private static bool IsUnordered(Type type)
        {
            return type.GetInterfaces().Any(i => i.IsGenericType && (i.GetGenericTypeDefinition() == typeof(ISet<>) ||
                                                                     i.GetGenericTypeDefinition() == typeof(IReadOnlySet<>)));
        }

        private static bool IsKeyValueSequence(Type type, out PropertyInfo key, out PropertyInfo value)
        {
            key = null;
            value = null;
            var element = type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(i => i.GetGenericArguments()[0])
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>));
            if (element == null)
                return false;

            key = element.GetProperty(nameof(KeyValuePair<int, int>.Key));
            value = element.GetProperty(nameof(KeyValuePair<int, int>.Value));
            return true;
        }

        /// <summary>Tuples, key-value pairs and nullable values are hashed by their fields like the game's own objects.</summary>
        private static bool IsStructural(Type type)
        {
            if (!type.IsGenericType)
                return false;

            var definition = type.GetGenericTypeDefinition();
            return definition == typeof(KeyValuePair<,>) || definition == typeof(Nullable<>) ||
                   typeof(ITuple).IsAssignableFrom(type);
        }

        private static ulong Leaf(string tag, string text)
        {
            return Mix(Mix(Offset, tag), text);
        }

        private static ulong Mix(ulong hash, string text)
        {
            if (text != null)
            {
                foreach (var character in text)
                {
                    hash ^= character;
                    hash *= Prime;
                }
            }

            hash ^= 0xFF;
            return hash * Prime;
        }

        private static ulong Mix(ulong hash, ulong value)
        {
            for (var shift = 0; shift < 64; shift += 8)
            {
                hash ^= (value >> shift) & 0xFF;
                hash *= Prime;
            }

            return hash;
        }
    }
}
