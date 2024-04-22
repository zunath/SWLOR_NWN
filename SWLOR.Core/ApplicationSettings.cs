using SWLOR.Core.Enumeration;

namespace SWLOR.Core
{
    public class ApplicationSettings
    {
        public string LogDirectory { get; }
        public string PluginDirectory { get; }
        public string RedisIPAddress { get; }
        public bool IsHotReloadEnabled { get; }
        public ServerEnvironmentType ServerEnvironment { get; }

        private static ApplicationSettings _settings;
        public static ApplicationSettings Get()
        {
            if (_settings == null)
                _settings = new ApplicationSettings();

            return _settings;
        }

        private ApplicationSettings()
        {
            LogDirectory = Environment.GetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY");
            PluginDirectory = Environment.GetEnvironmentVariable("SWLOR_PLUGIN_DIRECTORY");
            RedisIPAddress = Environment.GetEnvironmentVariable("NWNX_REDIS_HOST");
            IsHotReloadEnabled = Environment.GetEnvironmentVariable("SWLOR_ENABLE_HOT_RELOAD") == "true";

            var environment = Environment.GetEnvironmentVariable("SWLOR_ENVIRONMENT");
            if (!string.IsNullOrWhiteSpace(environment) && 
                (environment.ToLower() == "prod" || environment.ToLower() == "production"))
            {
                ServerEnvironment = ServerEnvironmentType.Production;
            }
            else if (!string.IsNullOrWhiteSpace(environment) && 
                     (environment.ToLower() == "test" || environment.ToLower() == "testing"))
            {
                ServerEnvironment = ServerEnvironmentType.Test;
            }
            else
            {
                ServerEnvironment = ServerEnvironmentType.Development;
            }
        }

    }
}