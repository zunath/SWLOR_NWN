using System;
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
