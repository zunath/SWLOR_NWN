namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class PCBaseStructureItem
    {
        public int PCBaseStructureItemID { get; set; }

        public int PCBaseStructureID { get; set; }

        [Required]
        [StringLength(60)]
        public string ItemGlobalID { get; set; }

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

        public virtual PCBaseStructure PCBaseStructure { get; set; }
    }
}
