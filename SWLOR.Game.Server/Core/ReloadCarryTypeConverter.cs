using System.Reflection;
using Newtonsoft.Json;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Writes a <see cref="Type"/> held as data (the status effect an aura applies, say) by its full name and reads it back as the
    /// reloaded build's type of that name. Only types of the game assembly can be carried. Used by <see cref="ReloadStateCarrier"/>.
    /// </summary>
    public sealed class ReloadCarryTypeConverter : JsonConverter
    {
        private readonly Assembly _gameAssembly;

        public ReloadCarryTypeConverter(Assembly gameAssembly)
        {
            _gameAssembly = gameAssembly ?? throw new ArgumentNullException(nameof(gameAssembly));
        }

        public override bool CanConvert(Type objectType) => typeof(Type).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var type = (Type)value;
            if (type.Assembly != _gameAssembly || type.IsGenericType)
                throw new JsonSerializationException($"Only types of the game build can be carried, not {type.FullName}.");
            writer.WriteValue(type.FullName);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var name = (string)reader.Value;
            return _gameAssembly.GetType(name!, throwOnError: false)
                   ?? throw new JsonSerializationException($"The reloaded build has no type {name}.");
        }
    }
}
