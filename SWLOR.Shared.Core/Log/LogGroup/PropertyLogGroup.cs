using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Log.LogGroup
{
    public class PropertyLogGroup: ILogGroup
    {
        public string Name => "Property";
        public ServerEnvironmentType EnvironmentType => ServerEnvironmentType.All;
        public bool AlwaysPrintToConsole => false;
    }
}
