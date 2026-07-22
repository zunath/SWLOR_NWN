using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server
{
    public class ApplicationSettings
    {
        public string LogDirectory { get; }
        public string RedisIPAddress { get; }
        public string BugDiscordWebhookUrl { get; }
        public string HoloNetWebhookUrl { get; }
        public string DMShoutWebhookUrl { get; }
        public string PropertyBroadcastWebhookUrl { get; }
        public string ServerNotificationWebhookUrl { get; }
        public ServerEnvironmentType ServerEnvironment { get; }
        public bool EngineTestsEnabled { get; }
        public string EngineTestResultsDirectory { get; }
        public string EngineTestFilter { get; }
        public float EngineTestStartupDelaySeconds { get; }
        public bool EngineTestShutdownOnCompletion { get; }
        public string EngineTestArenaResref { get; }

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
            RedisIPAddress = Environment.GetEnvironmentVariable("NWNX_REDIS_HOST");
            BugDiscordWebhookUrl = Environment.GetEnvironmentVariable("SWLOR_BUG_DISCORD_WEBHOOK_URL");
            HoloNetWebhookUrl = Environment.GetEnvironmentVariable("SWLOR_HOLONET_WEBHOOK_URL");
            DMShoutWebhookUrl = Environment.GetEnvironmentVariable("SWLOR_DM_SHOUT_WEBHOOK_URL");
            PropertyBroadcastWebhookUrl = Environment.GetEnvironmentVariable("SWLOR_PROPERTY_BROADCAST_WEBHOOK_URL");
            ServerNotificationWebhookUrl = Environment.GetEnvironmentVariable("SWLOR_SERVER_NOTIFICATION_WEBHOOK_URL");

            EngineTestsEnabled = ParseBool(Environment.GetEnvironmentVariable("SWLOR_ENGINE_TESTS_ENABLED"), false);
            EngineTestResultsDirectory = Environment.GetEnvironmentVariable("SWLOR_ENGINE_TEST_RESULTS_DIRECTORY")
                                         ?? (string.IsNullOrWhiteSpace(LogDirectory) ? null : LogDirectory + "engine_tests/");
            EngineTestFilter = Environment.GetEnvironmentVariable("SWLOR_ENGINE_TEST_FILTER");
            EngineTestStartupDelaySeconds = ParseFloat(Environment.GetEnvironmentVariable("SWLOR_ENGINE_TEST_STARTUP_DELAY_SECONDS"), 15f);
            EngineTestShutdownOnCompletion = ParseBool(Environment.GetEnvironmentVariable("SWLOR_ENGINE_TEST_SHUTDOWN"), true);
            EngineTestArenaResref = Environment.GetEnvironmentVariable("SWLOR_ENGINE_TEST_ARENA_RESREF");

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

        private static bool ParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            var normalized = value.Trim().ToLower();
            return normalized == "true" || normalized == "1" || normalized == "yes";
        }

        private static float ParseFloat(string value, float defaultValue)
        {
            return float.TryParse(value, out var parsed) ? parsed : defaultValue;
        }
    }
}
