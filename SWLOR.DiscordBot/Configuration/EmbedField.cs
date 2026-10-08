namespace SWLOR.DiscordBot.Configuration;

public sealed class EmbedField
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public bool Inline { get; set; }
}
