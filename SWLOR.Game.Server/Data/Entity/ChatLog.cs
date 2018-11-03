
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ChatLog")]
    public partial class ChatLog: IEntity
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ChatLog()
        {
            this.SenderAccountName = "";
            this.SenderCDKey = "";
            this.Message = "";
        }

        [Key]
        public long ChatLogID { get; set; }
        public int ChatChannelID { get; set; }
        public string SenderPlayerID { get; set; }
        public string SenderAccountName { get; set; }
        public string SenderCDKey { get; set; }
        public string ReceiverPlayerID { get; set; }
        public string ReceiverAccountName { get; set; }
        public string ReceiverCDKey { get; set; }
        public string Message { get; set; }
        public System.DateTime DateSent { get; set; }
        public string SenderDMName { get; set; }
        public string ReceiverDMName { get; set; }
    
        public virtual ChatChannelsDomain ChatChannelsDomain { get; set; }
        public virtual PlayerCharacter ReceiverPlayerCharacter { get; set; }
        public virtual PlayerCharacter SenderPlayerCharacter { get; set; }
    }
}
