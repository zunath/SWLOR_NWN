using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Log.LogGroup
{
    public class SpaceLogGroup: ILogGroup
    {
        public string Name => "Space";
        public ServerEnvironmentType EnvironmentType => ServerEnvironmentType.All;
        public bool AlwaysPrintToConsole => false;
    }
}
