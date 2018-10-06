namespace SWLOR.Game.Server.Data.Entities
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    [Table("DiscordChatQueue")]
    public partial class DiscordChatQueue
    {
        public int DiscordChatQueueID { get; set; }

        [Required]
        [StringLength(255)]
        public string SenderName { get; set; }

        [Required]
        public string Message { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime DateSent { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? DatePosted { get; set; }

        [Column(TypeName = "datetime2")]
        public DateTime? DateForRetry { get; set; }

        public string ResponseContent { get; set; }

        public int RetryAttempts { get; set; }

        [StringLength(1024)]
        public string SenderAccountName { get; set; }

        [StringLength(20)]
        public string SenderCDKey { get; set; }
    }
}
