using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestRequiredItem]")]
    public class QuestRequiredItem: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int QuestID { get; set; }
        public string Resref { get; set; }
        public int Quantity { get; set; }
        public int QuestStateID { get; set; }
        public bool MustBeCraftedByPlayer { get; set; }
    }
}
