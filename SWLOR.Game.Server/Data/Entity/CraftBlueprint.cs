
using System;
using System.Collections.Generic;

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[CraftBlueprints]")]
    public class CraftBlueprint: IEntity
    {
        [ExplicitKey]
        public long CraftBlueprintID { get; set; }
        public long CraftCategoryID { get; set; }
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
    
    }
}
