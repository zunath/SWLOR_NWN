namespace SWLOR.DiscordBot.Configuration;

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
