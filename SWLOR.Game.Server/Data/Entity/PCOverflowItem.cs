
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PCOverflowItems]")]
    public class PCOverflowItem: IEntity
    {
        [Key]
        public long PCOverflowItemID { get; set; }
        public string PlayerID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }
    }
}
