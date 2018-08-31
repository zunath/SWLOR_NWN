using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public class ComponentType
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ComponentType()
        {
            MainCraftBlueprints = new HashSet<CraftBlueprint>();
            SecondaryCraftBlueprints = new HashSet<CraftBlueprint>();
            TertiaryCraftBlueprints = new HashSet<CraftBlueprint>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int ComponentTypeID { get; set; }

        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CraftBlueprint> MainCraftBlueprints { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CraftBlueprint> SecondaryCraftBlueprints { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CraftBlueprint> TertiaryCraftBlueprints { get; set; }
    }
}
