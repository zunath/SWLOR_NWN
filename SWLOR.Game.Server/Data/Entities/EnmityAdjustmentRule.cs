using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class EnmityAdjustmentRule
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public EnmityAdjustmentRule()
        {
            Perks = new HashSet<Perk>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int EnmityAdjustmentRuleID { get; set; }

        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Perk> Perks { get; set; }
    }
}
