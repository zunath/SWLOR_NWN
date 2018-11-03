
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("ClientLogEvents")]
    public partial class ClientLogEvent: IEntity
    {

        [Key]
        public int ClientLogEventID { get; set; }
        public int ClientLogEventTypeID { get; set; }
        public string PlayerID { get; set; }
        public string CDKey { get; set; }
        public string AccountName { get; set; }
        public System.DateTime DateOfEvent { get; set; }
    
        public virtual ClientLogEventTypesDomain ClientLogEventTypesDomain { get; set; }
        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
