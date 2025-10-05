namespace SWLOR.Admin
{
    public class DiscordSettings
    {
        public const string Section = "Discord";

        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string BotToken { get; set; } = string.Empty;
        public string AllowedGuildId { get; set; } = string.Empty;
        public List<string> AllowedRoleIds { get; set; } = new();

        public DiscordSettings()
        {
        }

        public DiscordSettings(IConfiguration configuration)
        {
            var section = configuration.GetSection(Section);
            ClientId = section["ClientId"] ?? string.Empty;
            ClientSecret = section["ClientSecret"] ?? string.Empty;
            BotToken = section["BotToken"] ?? string.Empty;
            AllowedGuildId = section["AllowedGuildId"] ?? string.Empty;
            AllowedRoleIds = section.GetSection("AllowedRoleIds").Get<List<string>>() ?? new List<string>();
        }
    }
}