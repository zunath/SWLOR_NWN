using System.Text.Json.Serialization;
using System.Text.Json;

namespace SWLOR.DiscordBot.Configuration;

public static class ConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    public static BotConfiguration Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<BotConfiguration>(stream, JsonOptions)
               ?? throw new InvalidDataException("Bot configuration is empty or null.");
    }

    public static BotConfiguration Parse(string json) =>
        JsonSerializer.Deserialize<BotConfiguration>(json, JsonOptions)
        ?? throw new InvalidDataException("Bot configuration is empty or null.");

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        options.Converters.Add(new NumericStringUlongConverter());
        options.Converters.Add(new NullableNumericStringUlongConverter());
        return options;
    }
}
