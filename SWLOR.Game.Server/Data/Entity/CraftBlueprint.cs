using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;

namespace SWLOR.Game.Server.Data.Entity
{
    public class CraftBlueprint: IEntity
    {
        [Key]
        public int ID { get; set; }
        public CraftBlueprintCategory CraftCategoryID { get; set; }
        public int BaseLevel { get; set; }
        public string ItemName { get; set; }
        public string ItemResref { get; set; }
        public int Quantity { get; set; }
        public int SkillID { get; set; }
        public CraftDeviceType CraftDeviceID { get; set; }
        public PerkType? PerkID { get; set; }
        public int RequiredPerkLevel { get; set; }
        public bool IsActive { get; set; }
        public ComponentType MainComponentTypeID { get; set; }
        public int MainMinimum { get; set; }
        public ComponentType SecondaryComponentTypeID { get; set; }
        public int SecondaryMinimum { get; set; }
        public ComponentType TertiaryComponentTypeID { get; set; }
        public int TertiaryMinimum { get; set; }
        public int EnhancementSlots { get; set; }
        public int MainMaximum { get; set; }
        public int SecondaryMaximum { get; set; }
        public int TertiaryMaximum { get; set; }
        public int? BaseStructureID { get; set; }
    }
}
