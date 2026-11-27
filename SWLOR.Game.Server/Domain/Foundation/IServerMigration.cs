namespace SWLOR.Game.Server.Domain.Foundation
{
    public interface IServerMigration
    {
        int Version { get; }
        MigrationExecutionType ExecutionType { get; }
        void Migrate();
    }
}
