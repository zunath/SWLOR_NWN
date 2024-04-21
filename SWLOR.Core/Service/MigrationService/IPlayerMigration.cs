namespace SWLOR.Core.Service.MigrationService
{
    public interface IPlayerMigration
    {
        int Version { get; }
        void Migrate(uint player);
    }
}
