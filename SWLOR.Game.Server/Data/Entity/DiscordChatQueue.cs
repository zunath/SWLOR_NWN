
using System;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("DiscordChatQueue")]
    public partial class DiscordChatQueue: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DiscordChatQueue()
        {
            this.Message = "";
            this.SenderAccountName = "";
            this.SenderCDKey = "";
        }

        [Key]
        public int DiscordChatQueueID { get; set; }
        public string SenderName { get; set; }
        public string Message { get; set; }
        public System.DateTime DateSent { get; set; }
        public DateTime? DatePosted { get; set; }
        public DateTime? DateForRetry { get; set; }
        public string ResponseContent { get; set; }
        public int RetryAttempts { get; set; }
        public string SenderAccountName { get; set; }
        public string SenderCDKey { get; set; }
    }
}
