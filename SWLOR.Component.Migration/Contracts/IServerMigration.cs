using SWLOR.Component.Migration.Enums;

namespace SWLOR.Component.Migration.Contracts
{
    public interface IServerMigration
    {
        int Version { get; }
        MigrationExecutionType ExecutionType { get; }
        void Migrate();
    }
}
