
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("QuestRewardItems")]
    public class QuestRewardItem: IEntity
    {
        public QuestRewardItem()
        {
        }

        [Key]
        public int QuestRewardItemID { get; set; }
        public int QuestID { get; set; }
        public string Resref { get; set; }
        public int Quantity { get; set; }
    }
}
