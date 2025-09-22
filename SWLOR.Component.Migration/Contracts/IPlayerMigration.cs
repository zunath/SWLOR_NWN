namespace SWLOR.Component.Migration.Contracts
{
    public interface IPlayerMigration
    {
        int Version { get; }
        void Migrate(uint player);
    }
}
