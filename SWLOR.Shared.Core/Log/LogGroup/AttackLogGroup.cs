using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Core.Log.LogGroup
{
    public class AttackLogGroup: ILogGroup
    {
        public string Name => "Attack";
        public ServerEnvironmentType EnvironmentType => ServerEnvironmentType.Development | ServerEnvironmentType.Test;
        public bool AlwaysPrintToConsole => false;
    }
}
