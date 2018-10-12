namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("PCMapProgression")]
    public partial class PCMapProgression
    {
        public int PCMapProgressionID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        [Required]
        [StringLength(16)]
        public string AreaResref { get; set; }

        [Required]
        [StringLength(1024)]
        public string Progression { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
