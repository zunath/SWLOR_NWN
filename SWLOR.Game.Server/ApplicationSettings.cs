using System.Globalization;
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
        public string MasteryStaffWebhookUrl { get; }
        public ServerEnvironmentType ServerEnvironment { get; }

        /// <summary>
        /// True only when SWLOR_ENVIRONMENT contained a recognized spelling. An unset or
        /// mistyped value still resolves to Development, but consumers that must fail
        /// closed (e.g. the engine test runner) can require an explicit environment.
        /// </summary>
        public bool ServerEnvironmentIsExplicit { get; }

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
            MasteryStaffWebhookUrl = Environment.GetEnvironmentVariable("SWLOR_MASTERY_STAFF_WEBHOOK_URL");

            EngineTestsEnabled = ParseBool(Environment.GetEnvironmentVariable("SWLOR_ENGINE_TESTS_ENABLED"), false);
            EngineTestResultsDirectory = Environment.GetEnvironmentVariable("SWLOR_ENGINE_TEST_RESULTS_DIRECTORY")
                                         ?? (string.IsNullOrWhiteSpace(LogDirectory) ? null : LogDirectory + "engine_tests/");
            EngineTestFilter = Environment.GetEnvironmentVariable("SWLOR_ENGINE_TEST_FILTER");
            EngineTestStartupDelaySeconds = ParseFloat(Environment.GetEnvironmentVariable("SWLOR_ENGINE_TEST_STARTUP_DELAY_SECONDS"), 15f);
            EngineTestShutdownOnCompletion = ParseBool(Environment.GetEnvironmentVariable("SWLOR_ENGINE_TEST_SHUTDOWN"), true);
            EngineTestArenaResref = Environment.GetEnvironmentVariable("SWLOR_ENGINE_TEST_ARENA_RESREF");

            var environment = Environment.GetEnvironmentVariable("SWLOR_ENVIRONMENT")?.Trim().ToLower() ?? string.Empty;
            if (environment == "prod" || environment == "production")
            {
                ServerEnvironment = ServerEnvironmentType.Production;
                ServerEnvironmentIsExplicit = true;
            }
            else if (environment == "test" || environment == "testing")
            {
                ServerEnvironment = ServerEnvironmentType.Test;
                ServerEnvironmentIsExplicit = true;
            }
            else
            {
                ServerEnvironment = ServerEnvironmentType.Development;
                // Only a recognized spelling counts as explicit - a typo'd production value
                // falls through to Development, and anything gated on "not production"
                // (like the engine test runner) must be able to fail closed on that.
                ServerEnvironmentIsExplicit = environment == "dev" || environment == "development";
            }
        }

        private static bool ParseBool(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            var normalized = value.Trim().ToLower();
            if (normalized == "true" || normalized == "1" || normalized == "yes")
                return true;
            if (normalized == "false" || normalized == "0" || normalized == "no")
                return false;

            // An unrecognized value (e.g. a typo) keeps the setting's declared default rather
            // than silently flipping it - a mistyped SWLOR_ENGINE_TEST_SHUTDOWN must not leave
            // a headless test server running forever.
            return defaultValue;
        }

        private static float ParseFloat(string value, float defaultValue)
        {
            // Non-finite values (Infinity/NaN) parse successfully but would break consumers
            // like DelayCommand scheduling; only finite, non-negative values are accepted.
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) &&
                   float.IsFinite(parsed) &&
                   parsed >= 0f
                ? parsed
                : defaultValue;
        }
    }
}
