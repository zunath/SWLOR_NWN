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
