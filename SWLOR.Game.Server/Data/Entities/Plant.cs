using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class Plant
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Plant()
        {
            GrowingPlants = new HashSet<GrowingPlant>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PlantID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        public int BaseTicks { get; set; }

        [Required]
        [StringLength(16)]
        public string Resref { get; set; }

        public int WaterTicks { get; set; }

        public int Level { get; set; }

        [Required]
        [StringLength(16)]
        public string SeedResref { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<GrowingPlant> GrowingPlants { get; set; }
    }
}
