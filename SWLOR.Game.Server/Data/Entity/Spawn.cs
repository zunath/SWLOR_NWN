
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("Spawns")]
    public partial class Spawn: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Spawn()
        {
            this.Areas = new HashSet<Area>();
            this.SpawnObjects = new HashSet<SpawnObject>();
        }

        [ExplicitKey]
        public int SpawnID { get; set; }
        public string Name { get; set; }
        public int SpawnObjectTypeID { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Area> Areas { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SpawnObject> SpawnObjects { get; set; }
        public virtual SpawnObjectType SpawnObjectType { get; set; }
    }
}
