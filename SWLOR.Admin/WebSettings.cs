using SWLOR.Game.Server;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.Admin
{
    public class WebSettings
    {
        private IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        public const string Section = "Web";

        public string RedisIP { get; set; }
        public string LogDirectory { get; set; }
        public string Port { get; set; }

        public WebSettings(IConfiguration configuration)
        {
            var section = configuration.GetSection(Section);
            RedisIP = section["RedisIP"];
            LogDirectory = section["LogDirectory"];
            Port = section["RedisPort"];
        }

        public void Load()
        {
            // DB service expects these configs to be in the environment variables.
            Environment.SetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY", LogDirectory);
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", $"{RedisIP}:{Port}");

            _db.Load();
        }

    }
}
