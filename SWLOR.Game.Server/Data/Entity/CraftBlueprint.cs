using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[CraftBlueprint]")]
    public class CraftBlueprint: IEntity
    {
        [ExplicitKey]
        public int ID { get; set; }
        public int CraftCategoryID { get; set; }
        public int BaseLevel { get; set; }
        public string ItemName { get; set; }
        public string ItemResref { get; set; }
        public int Quantity { get; set; }
        public int SkillID { get; set; }
        public int CraftDeviceID { get; set; }
        public int? PerkID { get; set; }
        public int RequiredPerkLevel { get; set; }
        public bool IsActive { get; set; }
        public int MainComponentTypeID { get; set; }
        public int MainMinimum { get; set; }
        public int SecondaryComponentTypeID { get; set; }
        public int SecondaryMinimum { get; set; }
        public int TertiaryComponentTypeID { get; set; }
        public int TertiaryMinimum { get; set; }
        public int EnhancementSlots { get; set; }
        public int MainMaximum { get; set; }
        public int SecondaryMaximum { get; set; }
        public int TertiaryMaximum { get; set; }
        public int? BaseStructureID { get; set; }

        public IEntity Clone()
        {
            return new CraftBlueprint
            {
                ID = ID,
                CraftCategoryID = CraftCategoryID,
                BaseLevel = BaseLevel,
                ItemName = ItemName,
                ItemResref = ItemResref,
                Quantity = Quantity,
                SkillID = SkillID,
                CraftDeviceID = CraftDeviceID,
                PerkID = PerkID,
                RequiredPerkLevel = RequiredPerkLevel,
                IsActive = IsActive,
                MainComponentTypeID = MainComponentTypeID,
                MainMinimum = MainMinimum,
                SecondaryComponentTypeID = SecondaryComponentTypeID,
                SecondaryMinimum = SecondaryMinimum,
                TertiaryComponentTypeID = TertiaryComponentTypeID,
                TertiaryMinimum = TertiaryMinimum,
                EnhancementSlots = EnhancementSlots,
                MainMaximum = MainMaximum,
                SecondaryMaximum = SecondaryMaximum,
                TertiaryMaximum = TertiaryMaximum,
                BaseStructureID = BaseStructureID
            };
        }
    }
}
