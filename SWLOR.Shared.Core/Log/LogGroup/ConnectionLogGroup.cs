using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Log.LogGroup
{
    public class ConnectionLogGroup: ILogGroup
    {
        public string Name => "Connection";
        public ServerEnvironmentType EnvironmentType => ServerEnvironmentType.All;
        public bool AlwaysPrintToConsole => false;
    }
}
