using SWLOR.Game.Server.Data.Entities;

namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class SpawnObject
    {
        public int SpawnObjectID { get; set; }

        public int SpawnID { get; set; }

        [Required]
        [StringLength(16)]
        public string Resref { get; set; }

        public virtual Spawn Spawn { get; set; }
    }
}
