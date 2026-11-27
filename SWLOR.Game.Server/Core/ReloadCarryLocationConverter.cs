using System.Numerics;
using Newtonsoft.Json;
using SWLOR.NWN.API.Engine;

namespace SWLOR.Game.Server.Core
{
    /// <summary>
    /// Writes an engine <see cref="Location"/> held in carried state (where a pilot stood before a launch, say) as its area, position and
    /// facing, and reads it back as a new location in the same area: the area object and the engine survive a hot reload, the
    /// location handle the old build held does not. Used by <see cref="ReloadStateCarrier"/>; it calls the engine, so only on the
    /// engine thread.
    /// </summary>
    public sealed class ReloadCarryLocationConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(Location).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var location = (Location)value;
            var position = GetPositionFromLocation(location);
            writer.WriteStartArray();
            writer.WriteValue(GetAreaFromLocation(location));
            writer.WriteValue(position.X);
            writer.WriteValue(position.Y);
            writer.WriteValue(position.Z);
            writer.WriteValue(GetFacingFromLocation(location));
            writer.WriteEndArray();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var parts = serializer.Deserialize<double[]>(reader);
            if (parts == null || parts.Length != 5)
                throw new JsonSerializationException("A carried location is [area, x, y, z, facing].");

            return Location((uint)parts[0], new Vector3((float)parts[1], (float)parts[2], (float)parts[3]), (float)parts[4]);
        }
    }
}
