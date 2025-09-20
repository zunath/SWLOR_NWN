using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Log.LogGroup
{
    public class ErrorLogGroup: ILogGroup
    {
        public string Name => "Error";
        public ServerEnvironmentType EnvironmentType => ServerEnvironmentType.All;
        public bool AlwaysPrintToConsole => true;
    }
}
