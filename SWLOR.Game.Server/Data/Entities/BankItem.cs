namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class BankItem
    {
        public int BankItemID { get; set; }

        public int BankID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        [Required]
        [StringLength(60)]
        public string ItemID { get; set; }

        [Required]
        public string ItemName { get; set; }

        [Required]
        [StringLength(64)]
        public string ItemTag { get; set; }

        [Required]
        [StringLength(16)]
        public string ItemResref { get; set; }

        [Required]
        public string ItemObject { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateStored { get; set; }

        public virtual Bank Bank { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
