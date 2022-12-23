namespace SWLOR.Admin
{
    public class WebSettings
    {
        public const string Section = "Web";

        public string RedisIP { get; set; }
        public string LogDirectory { get; set; }

        public WebSettings(IConfiguration configuration)
        {
            var section = configuration.GetSection(Section);
            RedisIP = section["RedisIP"];
            LogDirectory = section["LogDirectory"];
        }

        public void Load()
        {
            // DB service expects these configs to be in the environment variables.
            Environment.SetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY", LogDirectory);
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", RedisIP);

            DB.Load();
        }

    }
}
