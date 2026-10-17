using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWLOR.DiscordBot.Discord;

// SDK payload interfaces expose only a component/emote discriminator. Preserve the concrete payload,
// including nested components, without serializing unrelated message, channel, or client objects.
internal sealed class DiscordPayloadConverter<T> : JsonConverter<T>
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new NotSupportedException("Transcript payloads are written only.");

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, value!.GetType(), options);
}
