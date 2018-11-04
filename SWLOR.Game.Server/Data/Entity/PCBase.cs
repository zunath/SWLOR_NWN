
using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("PCBases")]
    public class PCBase: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCBase()
        {
            CustomName = "";
            PCBasePermissions = new HashSet<PCBasePermission>();
            PCBaseStructures = new HashSet<PCBaseStructure>();
        }

        [Key]
        public int PCBaseID { get; set; }
        public string PlayerID { get; set; }
        public string AreaResref { get; set; }
        public string Sector { get; set; }
        public DateTime DateInitialPurchase { get; set; }
        public DateTime DateRentDue { get; set; }
        public int ShieldHP { get; set; }
        public bool IsInReinforcedMode { get; set; }
        public int Fuel { get; set; }
        public int ReinforcedFuel { get; set; }
        public DateTime DateFuelEnds { get; set; }
        public int PCBaseTypeID { get; set; }
        public int? ApartmentBuildingID { get; set; }
        public string CustomName { get; set; }
        public int? BuildingStyleID { get; set; }
    
        public virtual ICollection<PCBasePermission> PCBasePermissions { get; set; }
        public virtual ICollection<PCBaseStructure> PCBaseStructures { get; set; }
    }
}
