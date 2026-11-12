using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Extension;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Hands the runtime state marked <see cref="ReloadStateKind.Carried"/> from the build a hot reload retires to the build it
    /// activates (MaintainabilityRefactorHotReload.md, P4-15). While the old build quiesces, each class's carried fields are written
    /// as one JSON object (its static fields, and the fields of the container's singleton of the class) and left on the module
    /// (<see cref="ReloadCarry"/>); after the new build's rerun and adoption steps, each class's object is read back into the same
    /// fields of the new singleton. Objects of the game assembly are copied field by field (<see cref="ReloadCarryContractResolver"/>),
    /// subclasses keep their type (<see cref="ReloadCarryBinder"/>), shared objects stay shared, engine object ids stay valid (the
    /// engine keeps every object across a reload). A class whose state cannot be written or read is logged and left as the new build
    /// made it; the other classes still carry. Nothing of it runs unless a reload quiesces or activates a build.
    /// </summary>
    public sealed class ReloadStateCarrier
    {
        /// <summary>The prefix of a class's key on the module (<see cref="ReloadCarry"/>); the class's full name follows.</summary>
        public const string KeyPrefix = "state:";

        private readonly Assembly _gameAssembly;
        private readonly IReadOnlyList<JsonConverter> _converters;

        /// <param name="gameAssembly">The build whose classes carry state.</param>
        /// <param name="converters">
        /// Converters for references to definitions the rerun boot handlers rebuild (an ability, say), written as their key and read
        /// back as the new build's definition.
        /// </param>
        public ReloadStateCarrier(Assembly gameAssembly, IEnumerable<JsonConverter> converters = null)
        {
            _gameAssembly = gameAssembly ?? throw new ArgumentNullException(nameof(gameAssembly));
            _converters = (converters ?? Enumerable.Empty<JsonConverter>()).ToArray();
        }

        /// <summary>
        /// Every class of <paramref name="assembly"/> that declares a <see cref="ReloadStateKind.Carried"/> field, in the order of the
        /// class names, with those fields in the order of their names.
        /// </summary>
        public static IReadOnlyList<(Type Type, IReadOnlyList<FieldInfo> Fields)> CarriedClasses(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            return assembly.GetTypes()
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .Select(type => (Type: type, Fields: (IReadOnlyList<FieldInfo>)type.GetFields(flags)
                    .Where(field => ReloadDigestHasher.StateOf(field) == ReloadStateKind.Carried)
                    .OrderBy(field => field.Name, StringComparer.Ordinal)
                    .ToArray()))
                .Where(entry => entry.Fields.Count > 0)
                .ToArray();
        }

        /// <summary>
        /// Writes every carried class's fields and hands each class's text to <paramref name="put"/> under
        /// <see cref="KeyPrefix"/> and the class's full name (the server passes <see cref="ReloadCarry.Put"/>). An instance field is
        /// read from <paramref name="resolveOwner"/>'s instance of the class (the container's singleton). Every class is tried; when
        /// any fails, this throws after the last one, naming them.
        /// </summary>
        public void CarryAll(Func<Type, object> resolveOwner, Action<string, string> put)
        {
            if (resolveOwner == null)
                throw new ArgumentNullException(nameof(resolveOwner));
            if (put == null)
                throw new ArgumentNullException(nameof(put));

            var failed = new List<string>();
            foreach (var (type, fields) in CarriedClasses(_gameAssembly))
            {
                try
                {
                    put(KeyPrefix + type.FullName, Write(fields, OwnerOf(type, fields, resolveOwner)));
                }
                catch (Exception ex)
                {
                    failed.Add(type.FullName);
                    Log.Write(LogGroup.Error, $"Reload could not carry the state of {type.FullName}; the next build starts it empty. " +
                                              $"Details: {Environment.NewLine}{Environment.NewLine}{ex.ToMessageAndCompleteStacktrace()}", true);
                }
            }

            if (failed.Count > 0)
                throw new InvalidOperationException("Reload could not carry the state of " + string.Join(", ", failed) + ".");
        }

        /// <summary>
        /// Reads every carried class's text from <paramref name="take"/> (the server passes <see cref="ReloadCarry.Take"/>, which also
        /// removes it) back into the class's fields, then lets an <see cref="IReloadStateAdopter"/> owner start its timers again. A
        /// class the old build left nothing for keeps what the new build made and is logged. Every class is tried; when any fails,
        /// this throws after the last one, naming them.
        /// </summary>
        public void AdoptAll(Func<Type, object> resolveOwner, Func<string, string> take)
        {
            if (resolveOwner == null)
                throw new ArgumentNullException(nameof(resolveOwner));
            if (take == null)
                throw new ArgumentNullException(nameof(take));

            var failed = new List<string>();
            foreach (var (type, fields) in CarriedClasses(_gameAssembly))
            {
                try
                {
                    var text = take(KeyPrefix + type.FullName);
                    if (text == null)
                    {
                        Log.Write(LogGroup.Error, $"Reload found no carried state for {type.FullName}; it starts empty.", true);
                        continue;
                    }

                    var owner = OwnerOf(type, fields, resolveOwner);
                    Read(fields, owner, text);
                    (owner as IReloadStateAdopter)?.OnCarriedStateAdopted();
                }
                catch (Exception ex)
                {
                    failed.Add(type.FullName);
                    Log.Write(LogGroup.Error, $"Reload could not put back the carried state of {type.FullName}; it starts empty. " +
                                              $"Details: {Environment.NewLine}{Environment.NewLine}{ex.ToMessageAndCompleteStacktrace()}", true);
                }
            }

            if (failed.Count > 0)
                throw new InvalidOperationException("Reload could not put back the carried state of " + string.Join(", ", failed) + ".");
        }

        /// <summary>Writes <paramref name="fields"/> (static, or of <paramref name="instance"/>) as one JSON object keyed by field name.</summary>
        public string Write(IReadOnlyList<FieldInfo> fields, object instance)
        {
            if (fields == null)
                throw new ArgumentNullException(nameof(fields));

            var serializer = CreateSerializer();
            using var text = new StringWriter(CultureInfo.InvariantCulture);
            using (var writer = new JsonTextWriter(text))
            {
                writer.WriteStartObject();
                foreach (var field in fields)
                {
                    writer.WritePropertyName(field.Name);
                    serializer.Serialize(writer, field.GetValue(field.IsStatic ? null : instance), field.FieldType);
                }

                writer.WriteEndObject();
            }

            return text.ToString();
        }

        /// <summary>
        /// Reads what <see cref="Write"/> wrote into <paramref name="fields"/>. A collection the field already holds is refilled in
        /// place (so it keeps its comparer and whoever else holds it sees the carried content); anything else is replaced. A field
        /// the text does not name keeps its value; a name the build no longer has is skipped.
        /// </summary>
        public void Read(IReadOnlyList<FieldInfo> fields, object instance, string json)
        {
            if (fields == null)
                throw new ArgumentNullException(nameof(fields));
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            var serializer = CreateSerializer();
            var byName = fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
            using var reader = new JsonTextReader(new StringReader(json));
            reader.Read();
            if (reader.TokenType != JsonToken.StartObject)
                throw new JsonSerializationException("Carried state must be a JSON object.");

            while (reader.Read() && reader.TokenType == JsonToken.PropertyName)
            {
                var name = (string)reader.Value;
                reader.Read();
                if (!byName.TryGetValue(name!, out var field))
                {
                    reader.Skip();
                    continue;
                }

                Assign(field, field.IsStatic ? null : instance, serializer.Deserialize(reader, field.FieldType));
            }
        }

        /// <summary>
        /// Why <paramref name="type"/> cannot be carried, one line per problem (empty when it can): a delegate, a member typed
        /// <see cref="object"/>, or a type that is neither of the game assembly nor a value, a <see cref="Type"/>, a standard collection,
        /// a tuple or a vector; <paramref name="converted"/> names the types a converter writes (definitions). An interface or abstract
        /// class of the game assembly is checked through every implementation.
        /// </summary>
        public static IReadOnlyList<string> Problems(Type type, Assembly gameAssembly, Func<Type, bool> converted = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (gameAssembly == null)
                throw new ArgumentNullException(nameof(gameAssembly));

            var problems = new List<string>();
            CheckType(type, type.Name, gameAssembly, converted ?? (_ => false), new HashSet<Type>(), problems);
            return problems;
        }

        private static void CheckType(Type type, string path, Assembly gameAssembly, Func<Type, bool> converted, HashSet<Type> visited, List<string> problems)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (converted(type) || IsValue(type) || typeof(Type).IsAssignableFrom(type) || type == typeof(Vector3) || type == typeof(Vector2))
                return;

            if (typeof(Delegate).IsAssignableFrom(type))
            {
                problems.Add($"{path}: a delegate ({type.Name}) cannot be carried");
                return;
            }

            if (type == typeof(object))
            {
                problems.Add($"{path}: a member typed object cannot be carried");
                return;
            }

            if (type.IsArray)
            {
                CheckType(type.GetElementType()!, path + "[]", gameAssembly, converted, visited, problems);
                return;
            }

            if (type.IsGenericType && IsStandardGeneric(type.GetGenericTypeDefinition()))
            {
                foreach (var argument in type.GetGenericArguments())
                    CheckType(argument, $"{path}<{argument.Name}>", gameAssembly, converted, visited, problems);
                return;
            }

            if (type.Assembly != gameAssembly || type.IsGenericType)
            {
                problems.Add($"{path}: {type.FullName} is neither of the game build nor a value or standard collection");
                return;
            }

            if (!visited.Add(type))
                return;

            if (type.IsInterface || type.IsAbstract)
            {
                foreach (var implementation in gameAssembly.GetTypes().Where(candidate =>
                             !candidate.IsInterface && !candidate.IsAbstract && type.IsAssignableFrom(candidate)))
                    CheckType(implementation, $"{path}({implementation.Name})", gameAssembly, converted, visited, problems);
                if (!type.IsAbstract || type.IsInterface)
                    return;
            }

            foreach (var field in ReloadCarryContractResolver.InstanceFields(type))
                CheckType(field.FieldType, path + "." + field.Name, gameAssembly, converted, visited, problems);
        }

        private static bool IsValue(Type type)
        {
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid);
        }

        private static bool IsStandardGeneric(Type definition)
        {
            return definition == typeof(List<>) || definition == typeof(HashSet<>) || definition == typeof(Dictionary<,>) ||
                   definition == typeof(Queue<>) || definition == typeof(SortedDictionary<,>) || definition == typeof(SortedSet<>) ||
                   definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>) || definition == typeof(IList<>) ||
                   definition == typeof(ICollection<>) || definition == typeof(IEnumerable<>) || definition == typeof(ISet<>) ||
                   definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>) ||
                   definition == typeof(KeyValuePair<,>) || definition == typeof(Nullable<>) ||
                   definition.FullName!.StartsWith("System.ValueTuple`", StringComparison.Ordinal);
        }

        private static object OwnerOf(Type type, IReadOnlyList<FieldInfo> fields, Func<Type, object> resolveOwner)
        {
            if (fields.All(field => field.IsStatic))
                return null;

            return resolveOwner(type) ?? throw new InvalidOperationException($"The service container has no instance of {type.FullName}.");
        }

        private static void Assign(FieldInfo field, object owner, object value)
        {
            var current = field.GetValue(owner);
            if (current != null && value != null && TryRefill(current, value))
                return;

            if (field.IsStatic && field.IsInitOnly)
                throw new NotSupportedException($"{field.DeclaringType?.FullName}.{field.Name} is static readonly and not a collection; it cannot be carried.");

            field.SetValue(owner, value);
        }

        /// <summary>Replaces the content of the collection <paramref name="target"/> with that of <paramref name="source"/>.</summary>
        private static bool TryRefill(object target, object source)
        {
            if (target is IDictionary dictionary && source is IDictionary entries)
            {
                dictionary.Clear();
                foreach (DictionaryEntry entry in entries)
                    dictionary[entry.Key] = entry.Value;
                return true;
            }

            var collection = target.GetType().GetInterfaces()
                .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(ICollection<>));
            if (collection == null || source is not IEnumerable items || target.GetType() != source.GetType())
                return false;

            collection.GetMethod(nameof(ICollection<object>.Clear))!.Invoke(target, null);
            var add = collection.GetMethod(nameof(ICollection<object>.Add))!;
            foreach (var item in items)
                add.Invoke(target, new[] { item });
            return true;
        }

        private JsonSerializer CreateSerializer()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new ReloadCarryContractResolver(_gameAssembly),
                SerializationBinder = new ReloadCarryBinder(_gameAssembly),
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                DateParseHandling = DateParseHandling.None,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                FloatParseHandling = FloatParseHandling.Double,
                MaxDepth = 256,
            };
            settings.Converters.Add(new StringEnumConverter());
            settings.Converters.Add(new ReloadCarryPairDictionaryConverter());
            settings.Converters.Add(new ReloadCarryTypeConverter(_gameAssembly));
            foreach (var converter in _converters)
                settings.Converters.Add(converter);

            var serializer = JsonSerializer.Create(settings);
            serializer.ReferenceResolver = new ReloadCarryReferenceResolver();
            return serializer;
        }
    }
}
