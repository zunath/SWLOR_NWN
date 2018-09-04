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

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
