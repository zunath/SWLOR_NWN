using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCMigrationItem
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int PCMigrationItemID { get; set; }

        public int PCMigrationID { get; set; }

        [Required]
        [StringLength(16)]
        public string CurrentResref { get; set; }

        [Required]
        [StringLength(16)]
        public string NewResref { get; set; }

        public bool StripItemProperties { get; set; }

        public int BaseItemTypeID { get; set; }

        public virtual BaseItemType BaseItemType { get; set; }

        public virtual PCMigration PCMigration { get; set; }
    }
}
