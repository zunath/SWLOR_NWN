

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ClientLogEvents]")]
    public class ClientLogEvent: IEntity
    {

        [ExplicitKey]
        public Guid ID { get; set; }
        public int ClientLogEventTypeID { get; set; }
        public Guid PlayerID { get; set; }
        public string CDKey { get; set; }
        public string AccountName { get; set; }
        public DateTime DateOfEvent { get; set; }
    }
}
