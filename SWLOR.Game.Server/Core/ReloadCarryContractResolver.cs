using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// How <see cref="ReloadStateCarrier"/> writes an object of the game assembly: every instance field of the class and its base
    /// classes, public or not, by name, and nothing else (no properties, no type converters, no collection contracts), created
    /// again without running a constructor. The old and new build share the code of the class, so the field-for-field copy is the
    /// object the old build held. A non-null delegate cannot be carried and fails the field. Other types keep Newtonsoft's
    /// defaults.
    /// </summary>
    /// <remarks>
    /// A field that holds a service or content singleton (the <c>isService</c> predicate, from the container's registrations) is
    /// never written or read: an object with such fields takes its services through its constructor, so the content factory
    /// builds it through its values-free constructor with the new build's services, and the carried fields are then written over
    /// what the constructor set.
    /// </remarks>
    public sealed class ReloadCarryContractResolver : DefaultContractResolver
    {
        private readonly Assembly _gameAssembly;
        private readonly Func<Type, bool> _isService;
        private readonly IContentFactory _content;

        public ReloadCarryContractResolver(Assembly gameAssembly)
            : this(gameAssembly, null, null)
        {
        }

        /// <param name="gameAssembly">The build whose objects are written field by field.</param>
        /// <param name="isService">True for a type the container registers (a service or content singleton); null for none.</param>
        /// <param name="content">Builds an object whose fields hold services; null when no carried object holds one.</param>
        public ReloadCarryContractResolver(Assembly gameAssembly, Func<Type, bool> isService, IContentFactory content)
        {
            _gameAssembly = gameAssembly ?? throw new ArgumentNullException(nameof(gameAssembly));
            _isService = isService ?? (_ => false);
            _content = content;
        }

        /// <summary>The instance fields of <paramref name="type"/> that are carried: every one except those that hold a service.</summary>
        public IReadOnlyList<FieldInfo> CarriedFields(Type type)
        {
            return InstanceFields(type).Where(field => !_isService(field.FieldType)).ToArray();
        }

        /// <summary>Whether an object of <paramref name="type"/> holds a service, so it is built through its constructor.</summary>
        public bool TakesServices(Type type)
        {
            return InstanceFields(type).Any(field => _isService(field.FieldType));
        }

        /// <summary>Whether <paramref name="type"/> is a class or struct of the game assembly, written field by field.</summary>
        public bool IsGameObject(Type type)
        {
            return type.Assembly == _gameAssembly && !type.IsEnum && !type.IsInterface && !typeof(Delegate).IsAssignableFrom(type) &&
                   (type.IsClass || type.IsValueType);
        }

        /// <summary>Every instance field of <paramref name="type"/> and its base classes, base class first, in declaration order.</summary>
        public static IReadOnlyList<FieldInfo> InstanceFields(Type type)
        {
            var chain = new List<Type>();
            for (var current = type; current != null && current != typeof(object) && current != typeof(ValueType); current = current.BaseType)
                chain.Insert(0, current);

            return chain
                .SelectMany(current => current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                .ToArray();
        }

        protected override JsonContract CreateContract(Type objectType)
        {
            return IsGameObject(objectType) ? CreateObjectContract(objectType) : base.CreateContract(objectType);
        }

        protected override JsonObjectContract CreateObjectContract(Type objectType)
        {
            var contract = base.CreateObjectContract(objectType);
            if (!IsGameObject(objectType))
                return contract;

            contract.Properties.Clear();
            contract.CreatorParameters.Clear();
            contract.OverrideCreator = null;
            contract.DefaultCreator = TakesServices(objectType)
                ? CreatorWithServices(objectType)
                : () => RuntimeHelpers.GetUninitializedObject(objectType);
            contract.DefaultCreatorNonPublic = false;
            contract.ExtensionDataGetter = null;
            contract.ExtensionDataSetter = null;

            var fields = CarriedFields(objectType);
            var duplicates = fields.GroupBy(field => field.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
            foreach (var field in fields)
            {
                var property = new JsonProperty
                {
                    PropertyName = duplicates.Contains(field.Name) ? field.DeclaringType!.Name + "." + field.Name : field.Name,
                    UnderlyingName = field.Name,
                    PropertyType = field.FieldType,
                    DeclaringType = field.DeclaringType,
                    ValueProvider = new ReflectionValueProvider(field),
                    Readable = true,
                    Writable = true,
                };
                if (typeof(Delegate).IsAssignableFrom(field.FieldType))
                    property.Converter = new DelegateRefusingConverter();
                contract.Properties.Add(property);
            }

            return contract;
        }

        /// <summary>The creator of an object with services: the content factory, with the services of the build that reads it.</summary>
        private Func<object> CreatorWithServices(Type objectType)
        {
            if (_content == null)
            {
                return () => throw new JsonSerializationException(
                    $"{objectType.FullName} takes services through its constructor; the carry needs the content factory to build it.");
            }

            return () => _content.Create<object>(objectType);
        }

        /// <summary>Writes a null delegate as null and refuses any other: a closure of the old build cannot cross a reload.</summary>
        private sealed class DelegateRefusingConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => typeof(Delegate).IsAssignableFrom(objectType);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value != null)
                    throw new JsonSerializationException($"A {value.GetType().Name} delegate cannot be carried across a reload.");
                writer.WriteNull();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                reader.Skip();
                return null;
            }
        }
    }
}
