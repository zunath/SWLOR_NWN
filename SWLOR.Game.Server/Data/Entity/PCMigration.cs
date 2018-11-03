
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCMigrations")]
    public class PCMigration: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCMigration()
        {
            PCMigrationItems = new HashSet<PCMigrationItem>();
        }

        [ExplicitKey]
        public int PCMigrationID { get; set; }
        public string Name { get; set; }
    
        public virtual ICollection<PCMigrationItem> PCMigrationItems { get; set; }
    }
}
