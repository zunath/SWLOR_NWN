namespace SWLOR.Game.Server.Data.Contracts
{
    public interface IDataMigration
    {
        int Version { get; }
        void Up();
        void Down();
    }
}
