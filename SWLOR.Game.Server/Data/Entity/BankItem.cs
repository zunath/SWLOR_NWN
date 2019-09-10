
using System;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[BankItem]")]
    public class BankItem: IEntity
    {
        public BankItem()
        {
            ID = Guid.NewGuid();
        }

        [ExplicitKey]
        public Guid ID { get; set; }
        public int BankID { get; set; }
        public Guid PlayerID { get; set; }
        public string ItemID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public string ItemObject { get; set; }
        public DateTime DateStored { get; set; }

        public IEntity Clone()
        {
            return new BankItem
            {
                ID = ID,
                BankID = BankID,
                PlayerID = PlayerID,
                ItemID = ItemID,
                ItemName = ItemName,
                ItemTag = ItemTag,
                ItemResref = ItemResref,
                ItemObject = ItemObject, 
                DateStored = DateStored
            };
        }
    }
}
