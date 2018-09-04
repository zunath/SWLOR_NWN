namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class BaseStructure
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BaseStructure()
        {
            CraftBlueprints = new HashSet<CraftBlueprint>();
            PCBaseStructures = new HashSet<PCBaseStructure>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BaseStructureID { get; set; }

        public int BaseStructureTypeID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        [Required]
        [StringLength(16)]
        public string PlaceableResref { get; set; }

        [Required]
        [StringLength(16)]
        public string ItemResref { get; set; }

        public bool IsActive { get; set; }

        public double Power { get; set; }

        public double CPU { get; set; }

        public int HitPoints { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CraftBlueprint> CraftBlueprints { get; set; }

        public virtual BaseStructureType BaseStructureType { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBaseStructure> PCBaseStructures { get; set; }
    }
}
