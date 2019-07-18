

using System;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[ModuleEvent]")]
    public class ModuleEvent: IEntity
    {
        public ModuleEvent()
        {
            ID = Guid.NewGuid();
        }
        [ExplicitKey]
        public Guid ID { get; set; }
        public int ModuleEventTypeID { get; set; }
        public Guid? PlayerID { get; set; }
        public string CDKey { get; set; }
        public string AccountName { get; set; }
        public DateTime DateOfEvent { get; set; }
        public int? BankID { get; set; }
        public Guid? ItemID { get; set; }
        public string ItemName { get; set; }
        public string ItemTag { get; set; }
        public string ItemResref { get; set; }
        public Guid? PCBaseID { get; set; }
        public Guid? PCBaseStructureID { get; set; }
        public int? BaseStructureID { get; set; }
        public string CustomName { get; set; }

        public IEntity Clone()
        {
            return new ModuleEvent
            {
                ID = ID,
                ModuleEventTypeID = ModuleEventTypeID,
                PlayerID = PlayerID,
                CDKey = CDKey,
                AccountName = AccountName,
                DateOfEvent = DateOfEvent,
                BankID = BankID,
                ItemID = ItemID,
                ItemName = ItemName,
                ItemTag = ItemTag,
                ItemResref = ItemResref,
                PCBaseID = PCBaseID,
                PCBaseStructureID = PCBaseStructureID,
                BaseStructureID = BaseStructureID,
                CustomName = CustomName
            };
        }
    }
}
