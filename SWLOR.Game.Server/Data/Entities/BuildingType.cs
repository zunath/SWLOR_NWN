using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class BuildingType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BuildingType()
        {
            BuildingStyles = new HashSet<BuildingStyle>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BuildingTypeID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BuildingStyle> BuildingStyles { get; set; }
    }
}
