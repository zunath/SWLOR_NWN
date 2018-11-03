
using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("CraftBlueprints")]
    public partial class CraftBlueprint: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public CraftBlueprint()
        {
            this.PCCraftedBlueprints = new HashSet<PCCraftedBlueprint>();
        }

        [ExplicitKey]
        public long CraftBlueprintID { get; set; }
        public long CraftCategoryID { get; set; }
        public int BaseLevel { get; set; }
        public string ItemName { get; set; }
        public string ItemResref { get; set; }
        public int Quantity { get; set; }
        public int SkillID { get; set; }
        public int CraftDeviceID { get; set; }
        public Nullable<int> PerkID { get; set; }
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
        public Nullable<int> BaseStructureID { get; set; }
    
        public virtual BaseStructure BaseStructure { get; set; }
        public virtual ComponentType MainComponentType { get; set; }
        public virtual ComponentType SecondaryComponentType { get; set; }
        public virtual ComponentType TertiaryComponentType { get; set; }
        public virtual CraftBlueprintCategory CraftBlueprintCategory { get; set; }
        public virtual CraftDevice CraftDevice { get; set; }
        public virtual Perk Perk { get; set; }
        public virtual Skill Skill { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCCraftedBlueprint> PCCraftedBlueprints { get; set; }
    }
}
