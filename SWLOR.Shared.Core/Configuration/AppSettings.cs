using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;

namespace SWLOR.Shared.Core.Configuration
{
    public class AppSettings : IAppSettings
    {
        public string LogDirectory { get; }
        public string RedisIPAddress { get; }
        public ServerEnvironmentType ServerEnvironment { get; }
        public string BugWebHookUrl { get; }
        public string HolonetWebHookUrl { get; }
        public string DMShoutWebHookUrl { get; }

        public AppSettings()
        {
            LogDirectory = Environment.GetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY");
            RedisIPAddress = Environment.GetEnvironmentVariable("NWNX_REDIS_HOST");
            BugWebHookUrl = Environment.GetEnvironmentVariable("SWLOR_BUG_WEBHOOK_URL");
            HolonetWebHookUrl = Environment.GetEnvironmentVariable("SWLOR_HOLONET_WEBHOOK_URL");
            DMShoutWebHookUrl = Environment.GetEnvironmentVariable("SWLOR_DM_SHOUT_WEBHOOK_URL");

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