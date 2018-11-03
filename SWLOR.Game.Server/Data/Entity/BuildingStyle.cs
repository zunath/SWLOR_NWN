using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("BuildingStyles")]
    public partial class BuildingStyle: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public BuildingStyle()
        {
            this.Name = "";
            this.Resref = "";
            this.DoorRule = "";
            this.PCBases = new HashSet<PCBase>();
            this.ExteriorPCBaseStructures = new HashSet<PCBaseStructure>();
            this.InteriorPCBaseStructures = new HashSet<PCBaseStructure>();
        }

        [ExplicitKey]
        public int BuildingStyleID { get; set; }
        public string Name { get; set; }
        public string Resref { get; set; }
        public Nullable<int> BaseStructureID { get; set; }
        public bool IsDefault { get; set; }
        public string DoorRule { get; set; }
        public bool IsActive { get; set; }
        public int BuildingTypeID { get; set; }
        public int PurchasePrice { get; set; }
        public int DailyUpkeep { get; set; }
        public int FurnitureLimit { get; set; }
    
        public virtual BaseStructure BaseStructure { get; set; }
        public virtual BuildingType BuildingType { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBase> PCBases { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBaseStructure> ExteriorPCBaseStructures { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBaseStructure> InteriorPCBaseStructures { get; set; }
    }
}
