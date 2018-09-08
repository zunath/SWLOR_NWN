using SWLOR.Game.Server.Data.Entities;

namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("SpawnObjectType")]
    public partial class SpawnObjectType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SpawnObjectType()
        {
            Spawns = new HashSet<Spawn>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int SpawnObjectTypeID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Spawn> Spawns { get; set; }
    }
}
