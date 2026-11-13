using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Writes a <see cref="Dictionary{TKey,TValue}"/> whose key is not a string, number, enum or GUID (a tuple of two creatures, say)
    /// as a list of <c>[key, value]</c> pairs, because a JSON property name cannot hold such a key. Used by
    /// <see cref="ReloadStateCarrier"/>.
    /// </summary>
    public sealed class ReloadCarryPairDictionaryConverter : JsonConverter
    {
        /// <summary>Whether Newtonsoft can write <paramref name="keyType"/> as a property name and read it back.</summary>
        public static bool IsSimpleKey(Type keyType)
        {
            var type = Nullable.GetUnderlyingType(keyType) ?? keyType;
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(Guid) || type == typeof(decimal);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsGenericType &&
                   objectType.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                   !IsSimpleKey(objectType.GetGenericArguments()[0]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var arguments = value.GetType().GetGenericArguments();
            writer.WriteStartArray();
            foreach (DictionaryEntry entry in (IDictionary)value)
            {
                writer.WriteStartArray();
                serializer.Serialize(writer, entry.Key, arguments[0]);
                serializer.Serialize(writer, entry.Value, arguments[1]);
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var arguments = objectType.GetGenericArguments();
            var dictionary = (IDictionary)Activator.CreateInstance(objectType);
            Expect(reader, JsonToken.StartArray);
            while (reader.Read() && reader.TokenType != JsonToken.EndArray)
            {
                Expect(reader, JsonToken.StartArray);
                reader.Read();
                var key = serializer.Deserialize(reader, arguments[0]);
                reader.Read();
                var entryValue = serializer.Deserialize(reader, arguments[1]);
                reader.Read();
                Expect(reader, JsonToken.EndArray);
                dictionary.Add(key!, entryValue);
            }

            return dictionary;
        }

        private static void Expect(JsonReader reader, JsonToken token)
        {
            if (reader.TokenType != token)
                throw new JsonSerializationException($"Expected {token} in a carried dictionary, found {reader.TokenType}.");
        }
    }
}
