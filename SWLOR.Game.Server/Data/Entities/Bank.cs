namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class Bank
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Bank()
        {
            BankItems = new HashSet<BankItem>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int BankID { get; set; }

        [Required]
        [StringLength(255)]
        public string AreaName { get; set; }

        [Required]
        [StringLength(64)]
        public string AreaTag { get; set; }

        [Required]
        [StringLength(16)]
        public string AreaResref { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<BankItem> BankItems { get; set; }
    }
}
