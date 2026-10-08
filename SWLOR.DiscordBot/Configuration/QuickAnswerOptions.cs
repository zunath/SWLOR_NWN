namespace SWLOR.DiscordBot.Configuration;

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
