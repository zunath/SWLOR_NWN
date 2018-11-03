
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCMigrationItems")]
    public class PCMigrationItem: IEntity
    {
        [Key]
        public int PCMigrationItemID { get; set; }
        public int PCMigrationID { get; set; }
        public string CurrentResref { get; set; }
        public string NewResref { get; set; }
        public bool StripItemProperties { get; set; }
        public int BaseItemTypeID { get; set; }
    }
}
