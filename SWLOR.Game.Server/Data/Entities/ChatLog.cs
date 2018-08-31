using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("ChatLog")]
    public partial class ChatLog
    {
        public long ChatLogID { get; set; }

        public int ChatChannelID { get; set; }

        [StringLength(60)]
        public string SenderPlayerID { get; set; }

        [Required]
        [StringLength(1024)]
        public string SenderAccountName { get; set; }

        [Required]
        [StringLength(20)]
        public string SenderCDKey { get; set; }

        [StringLength(60)]
        public string ReceiverPlayerID { get; set; }

        [StringLength(1024)]
        public string ReceiverAccountName { get; set; }

        [StringLength(20)]
        public string ReceiverCDKey { get; set; }

        [Required]
        public string Message { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateSent { get; set; }

        [StringLength(60)]
        public string SenderDMName { get; set; }

        [StringLength(60)]
        public string ReceiverDMName { get; set; }

        public virtual ChatChannelsDomain ChatChannelsDomain { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }

        public virtual PlayerCharacter PlayerCharacter1 { get; set; }
    }
}
