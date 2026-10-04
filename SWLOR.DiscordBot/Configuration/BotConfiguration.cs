using System.Text.Json;
using System.Text.Json.Serialization;

namespace SWLOR.DiscordBot.Configuration;

public sealed class BotConfiguration
{
    public ulong GuildId { get; set; }
    public string Prefix { get; set; } = "?";
    public ulong[] AdministratorRoleIds { get; set; } = [];
    public TicketOptions Tickets { get; set; } = new();
    public WelcomeOptions Welcome { get; set; } = new();
    public FactionOptions Factions { get; set; } = new();
    public QuickAnswerOptions[] Answers { get; set; } = [];
}

public sealed class TicketOptions
{
    public bool Enabled { get; set; }
    public TicketPanelOptions[] Panels { get; set; } = [];
    public ulong[] SupportRoleIds { get; set; } = [];
    public ulong[] BypassRoleIds { get; set; } = [];
    public bool? BypassMemberLimit { get; set; }
    public bool? BypassPanelLimit { get; set; }
    public bool? BypassGuildLimit { get; set; }
    public int MemberLimit { get; set; } = 1;
    public int GuildLimit { get; set; } = 100;
    public ulong ClosedCategoryId { get; set; }
    public ulong LogChannelId { get; set; }
    public bool CloseConfirmation { get; set; } = true;
    public bool ClosedRequesterCanRead { get; set; } = true;
    public TimeSpan CleanupDelay { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int ArchiveRetentionDays { get; set; } = 90;
    public string ArchiveDirectory { get; set; } = "/data/archives";
    public bool CopyAttachments { get; set; } = true;
    public long MaxAttachmentBytes { get; set; } = 104857600;
    public long MaxTicketAttachmentBytes { get; set; } = 1073741824;
    public long MaxTranscriptContentBytes { get; set; } = 33554432;
}

public sealed class TicketPanelOptions
{
    public string Id { get; set; } = "support";
    public ulong ChannelId { get; set; }
    public ulong[] OpenCategoryIds { get; set; } = [];
    public int OpenLimit { get; set; } = 500;
    public string Label { get; set; } = "Open ticket";
    public string OpeningMessage { get; set; } = "";
    public string PanelMessage { get; set; } = "";
}

public sealed class WelcomeOptions
{
    private Dictionary<string, ulong>? _channelMentions = new(StringComparer.OrdinalIgnoreCase);

    public bool Enabled { get; set; }
    public ulong ChannelId { get; set; }
    public string Template { get; set; } = "";
    public bool DirectMessage { get; set; }
    public Dictionary<string, ulong> ChannelMentions
    {
        get => _channelMentions!;
        set => _channelMentions = value is null ? null : new Dictionary<string, ulong>(value, StringComparer.OrdinalIgnoreCase);
    }
}

public sealed class FactionOptions
{
    public bool Enabled { get; set; }
    public FactionRole[] Roles { get; set; } = [];
    public bool? Exclusive { get; set; }
    public string? Behavior { get; set; }
    public bool DeleteCommand { get; set; }
    public bool DeleteResponse { get; set; }
}

public sealed class FactionRole
{
    public string Name { get; set; } = "";
    public ulong RoleId { get; set; }
}

public sealed class QuickAnswerOptions
{
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string[] Responses { get; set; } = [];
    public AnswerEmbed[] Embeds { get; set; } = [];
    public ulong[] AllowedRoleIds { get; set; } = [];
    public ulong[] AllowedChannelIds { get; set; } = [];
    public TimeSpan Cooldown { get; set; }
    public bool DeleteCommand { get; set; }
    public TimeSpan? DeleteResponseAfter { get; set; }
}

public sealed class AnswerEmbed
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public uint Color { get; set; }
    public EmbedField[] Fields { get; set; } = [];
}

public sealed class EmbedField
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public bool Inline { get; set; }
}

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

    private sealed class NumericStringUlongConverter : JsonConverter<ulong>
    {
        public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetUInt64(out var numeric)) return numeric;
            if (reader.TokenType == JsonTokenType.String && ulong.TryParse(reader.GetString(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var text)) return text;
            throw new JsonException("Expected an unsigned integer ID or a string containing one.");
        }
        public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
    }

    private sealed class NullableNumericStringUlongConverter : JsonConverter<ulong?>
    {
        public override ulong? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null ? null : new NumericStringUlongConverter().Read(ref reader, typeof(ulong), options);
        public override void Write(Utf8JsonWriter writer, ulong? value, JsonSerializerOptions options)
        {
            if (value.HasValue) writer.WriteNumberValue(value.Value); else writer.WriteNullValue();
        }
    }
}
