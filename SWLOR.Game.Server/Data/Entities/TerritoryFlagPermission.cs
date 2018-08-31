using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class TerritoryFlagPermission
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public TerritoryFlagPermission()
        {
            PCTerritoryFlagsPermissions = new HashSet<PCTerritoryFlagsPermission>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int TerritoryFlagPermissionID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        public bool IsActive { get; set; }

        public bool IsSelectable { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCTerritoryFlagsPermission> PCTerritoryFlagsPermissions { get; set; }
    }
}
