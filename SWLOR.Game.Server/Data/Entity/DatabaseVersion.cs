

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[DatabaseVersions]")]
    public class DatabaseVersion: IEntity
    {
        [Key]
        public int DatabaseVersionID { get; set; }
        public string ScriptName { get; set; }
        public System.DateTime DateApplied { get; set; }
        public System.DateTime VersionDate { get; set; }
        public int VersionNumber { get; set; }
    }
}
