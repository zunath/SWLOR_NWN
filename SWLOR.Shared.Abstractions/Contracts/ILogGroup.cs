using SWLOR.Shared.Core.Enums;

namespace SWLOR.Shared.Abstractions.Contracts
{
    public interface ILogGroup
    {
        string Name { get; }
        ServerEnvironmentType EnvironmentType { get; }
        bool AlwaysPrintToConsole { get; }
    }
}