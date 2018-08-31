using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class ClientLogEvent
    {
        public int ClientLogEventID { get; set; }

        public int ClientLogEventTypeID { get; set; }

        [StringLength(60)]
        public string PlayerID { get; set; }

        [Required]
        [StringLength(20)]
        public string CDKey { get; set; }

        [Required]
        [StringLength(1024)]
        public string AccountName { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateOfEvent { get; set; }

        public virtual ClientLogEventTypesDomain ClientLogEventTypesDomain { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
