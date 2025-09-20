using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Log.LogGroup
{
    public class IncubationLogGroup: ILogGroup
    {
        public string Name => "Incubation";
        public ServerEnvironmentType EnvironmentType => ServerEnvironmentType.All;
        public bool AlwaysPrintToConsole => false;
    }
}
