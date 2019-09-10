

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
            DateOfEvent = DateTime.UtcNow;
            CDKey = string.Empty;
            AccountName = string.Empty;
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
        public string AreaSector { get; set; }
        public string AreaName { get; set; }
        public string AreaTag { get; set; }
        public string AreaResref { get; set; }
        public Enumeration.PCBaseType? PCBaseTypeID { get; set; }
        public DateTime? DateRentDue { get; set; }
        public Guid? AttackerPlayerID { get; set; }

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
                CustomName = CustomName,
                AreaSector = AreaSector,
                AreaName = AreaName,
                AreaTag = AreaTag,
                AreaResref = AreaResref,
                PCBaseTypeID = PCBaseTypeID,
                DateRentDue = DateRentDue,
                AttackerPlayerID = AttackerPlayerID
            };
        }
    }
}
