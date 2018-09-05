namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class PCBaseStructure
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCBaseStructure()
        {
            PCBaseStructureItems = new HashSet<PCBaseStructureItem>();
        }

        public int PCBaseStructureID { get; set; }

        public int PCBaseID { get; set; }

        public int BaseStructureID { get; set; }

        public double LocationX { get; set; }

        public double LocationY { get; set; }

        public double LocationZ { get; set; }

        public double LocationOrientation { get; set; }

        public double Durability { get; set; }

        public int? InteriorStyleID { get; set; }

        public int? ExteriorStyleID { get; set; }

        public virtual BaseStructure BaseStructure { get; set; }

        public virtual PCBase PCBase { get; set; }

        public virtual BuildingStyle InteriorStyle { get; set; }

        public virtual BuildingStyle ExteriorStyle { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBaseStructureItem> PCBaseStructureItems { get; set; }
    }
}
