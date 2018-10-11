namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class ApartmentBuilding
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ApartmentBuilding()
        {
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ApartmentBuildingID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }
    }
}
