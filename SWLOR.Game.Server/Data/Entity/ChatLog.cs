

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ChatLog]")]
    public class ChatLog: IEntity
    {
        public ChatLog()
        {
            SenderAccountName = "";
            SenderCDKey = "";
            Message = "";
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
    }
}
