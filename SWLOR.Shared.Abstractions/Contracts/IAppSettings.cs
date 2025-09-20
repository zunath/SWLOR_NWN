using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Configuration;

public interface IAppSettings
{
    string LogDirectory { get; }
    string RedisIPAddress { get; }
    ServerEnvironmentType ServerEnvironment { get; }
}