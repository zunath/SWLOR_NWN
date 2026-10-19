namespace SWLOR.Game.Server.Entity
{
    public class ServerConfiguration: EntityBase
    {
        public ServerConfiguration()
        {
            Id = "SWLOR_CONFIG";
            MigrationVersion = 0;
        }

        [Indexed]
        public int MigrationVersion { get; set; }

        /// <summary>The character migration version every saved character file was last upgraded to at boot.</summary>
        public int CharacterFileVersion { get; set; }
    }
}
