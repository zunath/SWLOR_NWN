namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("PCBases")]
    public partial class PCBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public PCBase()
        {
            PCBasePermissions = new HashSet<PCBasePermission>();
            PCBaseStructures = new HashSet<PCBaseStructure>();
        }

        [Key]
        public int PCBaseID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        [Required]
        [StringLength(16)]
        public string AreaResref { get; set; }

        [Required]
        [StringLength(2)]
        public string Sector { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateInitialPurchase { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateRentDue { get; set; }

        public int ShieldHP { get; set; }

        public bool IsInReinforcedMode { get; set; }
        
        public int Fuel { get; set; }

        public int ReinforcedFuel { get; set; }

        public int PCBaseTypeID { get; set; }

        public int? ApartmentBuildingID { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateFuelEnds { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBasePermission> PCBasePermissions { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCBaseStructure> PCBaseStructures { get; set; }
    }
}
