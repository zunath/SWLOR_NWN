namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    public partial class BugReport
    {
        public int BugReportID { get; set; }

        [StringLength(60)]
        public string SenderPlayerID { get; set; }

        [Required]
        [StringLength(20)]
        public string CDKey { get; set; }

        [Required]
        [StringLength(1000)]
        public string Text { get; set; }
        
        [StringLength(64)]
        public string TargetName { get; set; }

        [Required]
        [StringLength(16)]
        public string AreaResref { get; set; }

        public double SenderLocationX { get; set; }

        public double SenderLocationY { get; set; }

        public double SenderLocationZ { get; set; }

        public double SenderLocationOrientation { get; set; }
        
        [Column(TypeName = "datetime2")]
        public DateTime DateSubmitted { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
