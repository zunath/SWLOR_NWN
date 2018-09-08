namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Spawn
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Spawn()
        {
            SpawnObjects = new HashSet<SpawnObject>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SpawnID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        public int SpawnObjectTypeID { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SpawnObject> SpawnObjects { get; set; }

        public virtual SpawnObjectType SpawnObjectType { get; set; }
    }
}
