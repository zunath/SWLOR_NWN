using SWLOR.Shared.Abstractions;

namespace SWLOR.Component.Migration.Entity
{
    public class ServerMigrationStatus: EntityBase
    {
        public const string DefaultId = "SWLOR_CONFIG";

        public ServerMigrationStatus()
        {
            Id = DefaultId;
            MigrationVersion = 0;
        }

        [Indexed]
        public int MigrationVersion { get; set; }
    }
}
