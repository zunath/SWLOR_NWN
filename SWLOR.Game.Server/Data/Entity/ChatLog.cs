

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ChatLog]")]
    public class ChatLog: IEntity
    {
        public ChatLog()
        {
            ID = Guid.NewGuid();
            SenderAccountName = "";
            SenderCDKey = "";
            Message = "";
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public int ChatChannelID { get; set; }
        public Guid? SenderPlayerID { get; set; }
        public string SenderAccountName { get; set; }
        public string SenderCDKey { get; set; }
        public Guid? ReceiverPlayerID { get; set; }
        public string ReceiverAccountName { get; set; }
        public string ReceiverCDKey { get; set; }
        public string Message { get; set; }
        public DateTime DateSent { get; set; }
        public string SenderDMName { get; set; }
        public string ReceiverDMName { get; set; }

        public IEntity Clone()
        {
            return new ChatLog
            {
                ID = ID,
                ChatChannelID = ChatChannelID,
                SenderPlayerID = SenderPlayerID,
                SenderAccountName = SenderAccountName,
                SenderCDKey = SenderCDKey,
                ReceiverPlayerID = ReceiverPlayerID,
                ReceiverAccountName = ReceiverAccountName,
                ReceiverCDKey = ReceiverCDKey,
                Message = Message,
                DateSent = DateSent,
                SenderDMName = SenderDMName,
                ReceiverDMName = ReceiverDMName
            };
        }
    }
}
