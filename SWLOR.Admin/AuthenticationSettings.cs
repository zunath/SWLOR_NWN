namespace SWLOR.Admin
{
    public class AuthenticationSettings
    {
        public const string Section = "Authentication";

        public bool RequireDiscordAuth { get; set; } = true;

        public AuthenticationSettings()
        {
        }

        public AuthenticationSettings(IConfiguration configuration)
        {
            var section = configuration.GetSection(Section);
            RequireDiscordAuth = section.GetValue<bool>("RequireDiscordAuth", true);
        }
    }
}