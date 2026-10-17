using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWLOR.DiscordBot.Configuration;

internal sealed class NullableNumericStringUlongConverter : JsonConverter<ulong?>
{
    public override ulong? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : new NumericStringUlongConverter().Read(ref reader, typeof(ulong), options);

    public override void Write(Utf8JsonWriter writer, ulong? value, JsonSerializerOptions options)
    {
        if (value.HasValue) writer.WriteNumberValue(value.Value); else writer.WriteNullValue();
    }
}
