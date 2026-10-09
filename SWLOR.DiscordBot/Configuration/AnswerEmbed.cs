namespace SWLOR.DiscordBot.Configuration;

public sealed class AnswerEmbed
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public uint Color { get; set; }
    public EmbedField[] Fields { get; set; } = [];
}
