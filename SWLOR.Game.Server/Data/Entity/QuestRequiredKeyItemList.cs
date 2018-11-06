

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestRequiredKeyItemList]")]
    public class QuestRequiredKeyItemList: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int QuestID { get; set; }
        public int KeyItemID { get; set; }
        public int QuestStateID { get; set; }
    }
}
