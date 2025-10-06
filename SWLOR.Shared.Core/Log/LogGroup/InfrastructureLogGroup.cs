using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;

namespace SWLOR.Shared.Core.Log.LogGroup
{
    public class InfrastructureLogGroup: ILogGroup
    {
        public string Name => "Infrastructure";
        public ServerEnvironmentType EnvironmentType => ServerEnvironmentType.Development | ServerEnvironmentType.Test;
        public bool AlwaysPrintToConsole => true;
    }
}
