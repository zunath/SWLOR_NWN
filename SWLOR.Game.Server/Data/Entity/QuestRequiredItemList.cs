
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestRequiredItemList]")]
    public class QuestRequiredItemList: IEntity
    {
        [Key]
        public int QuestRequiredItemListID { get; set; }
        public int QuestID { get; set; }
        public string Resref { get; set; }
        public int Quantity { get; set; }
        public int QuestStateID { get; set; }
        public bool MustBeCraftedByPlayer { get; set; }
    }
}
