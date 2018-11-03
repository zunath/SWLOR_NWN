
using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCBaseStructures")]
    public partial class PCBaseStructure: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCBaseStructure()
        {
            this.CustomName = "";
            this.PCBaseStructureItems = new HashSet<PCBaseStructureItem>();
            this.PCBaseStructurePermissions = new HashSet<PCBaseStructurePermission>();
            this.ChildStructures = new HashSet<PCBaseStructure>();
            this.PlayerCharacters = new HashSet<PlayerCharacter>();
        }

        [Key]
        public int PCBaseStructureID { get; set; }
        public int PCBaseID { get; set; }
        public int BaseStructureID { get; set; }
        public double LocationX { get; set; }
        public double LocationY { get; set; }
        public double LocationZ { get; set; }
        public double LocationOrientation { get; set; }
        public double Durability { get; set; }
        public Nullable<int> InteriorStyleID { get; set; }
        public Nullable<int> ExteriorStyleID { get; set; }
        public Nullable<int> ParentPCBaseStructureID { get; set; }
        public string CustomName { get; set; }
        public int StructureBonus { get; set; }
        public Nullable<System.DateTime> DateNextActivity { get; set; }
    
        public virtual BaseStructure BaseStructure { get; set; }
        public virtual BuildingStyle ExteriorStyle { get; set; }
        public virtual BuildingStyle InteriorStyle { get; set; }
        public virtual PCBase PCBase { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBaseStructureItem> PCBaseStructureItems { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBaseStructurePermission> PCBaseStructurePermissions { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBaseStructure> ChildStructures { get; set; }
        public virtual PCBaseStructure ParentPCBaseStructure { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PlayerCharacter> PlayerCharacters { get; set; }
    }
}
