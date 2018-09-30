using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class LootTable
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public LootTable()
        {
            LootTableItems = new HashSet<LootTableItem>();
            NorthwestAreaLootTables = new HashSet<Area>();
            NortheastAreaLootTables = new HashSet<Area>();
            SouthwestAreaLootTables = new HashSet<Area>();
            SoutheastAreaLootTables = new HashSet<Area>();
        }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int LootTableID { get; set; }

        [Required]
        [StringLength(64)]
        public string Name { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<LootTableItem> LootTableItems { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Area> NorthwestAreaLootTables { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Area> NortheastAreaLootTables { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Area> SouthwestAreaLootTables { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Area> SoutheastAreaLootTables { get; set; }

    }
}
