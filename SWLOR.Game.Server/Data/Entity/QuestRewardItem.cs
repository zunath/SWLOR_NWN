using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestRewardItem]")]
    public class QuestRewardItem: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int QuestID { get; set; }
        public string Resref { get; set; }
        public int Quantity { get; set; }

        public IEntity Clone()
        {
            return new QuestRewardItem
            {
                ID = ID,
                QuestID = QuestID,
                Resref = Resref,
                Quantity = Quantity
            };
        }
    }
}
