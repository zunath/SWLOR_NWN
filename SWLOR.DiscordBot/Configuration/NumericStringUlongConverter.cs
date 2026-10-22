using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWLOR.DiscordBot.Configuration;

internal sealed class NumericStringUlongConverter : JsonConverter<ulong>
{
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt64(out var numeric)) return numeric;
        if (reader.TokenType == JsonTokenType.String && ulong.TryParse(reader.GetString(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var text)) return text;
        throw new JsonException("Expected an unsigned integer ID or a string containing one.");
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
}
