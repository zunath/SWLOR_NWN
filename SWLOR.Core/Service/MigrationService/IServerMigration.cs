namespace SWLOR.Core.Service.MigrationService
{
    public interface IServerMigration
    {
        int Version { get; }
        void Migrate();
    }
}
