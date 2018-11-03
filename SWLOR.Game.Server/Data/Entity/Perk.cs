
using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("Perks")]
    public partial class Perk: IEntity, ICacheable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Perk()
        {
            this.Name = "";
            this.ScriptName = "";
            this.Description = "";
            this.CraftBlueprints = new HashSet<CraftBlueprint>();
            this.PCPerkRefunds = new HashSet<PCPerkRefund>();
            this.PCPerks = new HashSet<PCPerk>();
            this.PerkLevels = new HashSet<PerkLevel>();
        }

        [ExplicitKey]
        public int PerkID { get; set; }
        public string Name { get; set; }
        public Nullable<int> FeatID { get; set; }
        public bool IsActive { get; set; }
        public string ScriptName { get; set; }
        public int BaseFPCost { get; set; }
        public double BaseCastingTime { get; set; }
        public string Description { get; set; }
        public int PerkCategoryID { get; set; }
        public Nullable<int> CooldownCategoryID { get; set; }
        public int ExecutionTypeID { get; set; }
        public string ItemResref { get; set; }
        public bool IsTargetSelfOnly { get; set; }
        public int Enmity { get; set; }
        public int EnmityAdjustmentRuleID { get; set; }
        public Nullable<int> CastAnimationID { get; set; }
    
        public virtual CooldownCategory CooldownCategory { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CraftBlueprint> CraftBlueprints { get; set; }
        public virtual EnmityAdjustmentRule EnmityAdjustmentRule { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCPerkRefund> PCPerkRefunds { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCPerk> PCPerks { get; set; }
        public virtual PerkCategory PerkCategory { get; set; }
        public virtual PerkExecutionType PerkExecutionType { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PerkLevel> PerkLevels { get; set; }
    }
}
