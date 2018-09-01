using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class Perk
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Perk()
        {
            CraftBlueprints = new HashSet<CraftBlueprint>();
            PCPerks = new HashSet<PCPerk>();
            PerkLevels = new HashSet<PerkLevel>();
            StructureBlueprints = new HashSet<StructureBlueprint>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PerkID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        public int? FeatID { get; set; }

        public bool IsActive { get; set; }

        [Required]
        [StringLength(64)]
        public string JavaScriptName { get; set; }

        public int BaseFPCost { get; set; }

        public double BaseCastingTime { get; set; }

        [Required]
        [StringLength(256)]
        public string Description { get; set; }

        public int PerkCategoryID { get; set; }

        public int? CooldownCategoryID { get; set; }

        public int ExecutionTypeID { get; set; }

        [StringLength(16)]
        public string ItemResref { get; set; }

        public bool IsTargetSelfOnly { get; set; }

        public int Enmity { get; set; }

        public int EnmityAdjustmentRuleID { get; set; }

        public virtual CooldownCategory CooldownCategory { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CraftBlueprint> CraftBlueprints { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PCPerk> PCPerks { get; set; }

        public virtual PerkCategory PerkCategory { get; set; }

        public virtual PerkExecutionType PerkExecutionType { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PerkLevel> PerkLevels { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<StructureBlueprint> StructureBlueprints { get; set; }

        public virtual EnmityAdjustmentRule EnmityAdjustmentRule { get; set; }
    }
}
