using SWLOR.Shared.Abstractions.Enums;

namespace SWLOR.Shared.Abstractions.Contracts
{
    public interface IAppSettings
    {
        string LogDirectory { get; }
        string RedisIPAddress { get; }
        ServerEnvironmentType ServerEnvironment { get; }
        string BugWebHookUrl { get; }
        string HolonetWebHookUrl { get; }
        string DMShoutWebHookUrl { get; }
    }
}