
using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCBases")]
    public partial class PCBase: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCBase()
        {
            this.CustomName = "";
            this.PCBasePermissions = new HashSet<PCBasePermission>();
            this.PCBaseStructures = new HashSet<PCBaseStructure>();
            this.PrimaryResidencePlayerCharacters = new HashSet<PlayerCharacter>();
        }

        [Key]
        public int PCBaseID { get; set; }
        public string PlayerID { get; set; }
        public string AreaResref { get; set; }
        public string Sector { get; set; }
        public System.DateTime DateInitialPurchase { get; set; }
        public System.DateTime DateRentDue { get; set; }
        public int ShieldHP { get; set; }
        public bool IsInReinforcedMode { get; set; }
        public int Fuel { get; set; }
        public int ReinforcedFuel { get; set; }
        public System.DateTime DateFuelEnds { get; set; }
        public int PCBaseTypeID { get; set; }
        public Nullable<int> ApartmentBuildingID { get; set; }
        public string CustomName { get; set; }
        public Nullable<int> BuildingStyleID { get; set; }
    
        public virtual ApartmentBuilding ApartmentBuilding { get; set; }
        public virtual BuildingStyle BuildingStyle { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBasePermission> PCBasePermissions { get; set; }
        public virtual PCBaseType PCBaseType { get; set; }
        public virtual PlayerCharacter PlayerCharacter { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBaseStructure> PCBaseStructures { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PlayerCharacter> PrimaryResidencePlayerCharacters { get; set; }
    }
}
