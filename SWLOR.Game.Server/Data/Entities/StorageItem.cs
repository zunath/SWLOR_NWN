using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class StorageItem
    {
        public int StorageItemID { get; set; }

        public int StorageContainerID { get; set; }

        [Required]
        public string ItemName { get; set; }

        [Required]
        [StringLength(64)]
        public string ItemTag { get; set; }

        [Required]
        [StringLength(16)]
        public string ItemResref { get; set; }

        [Required]
        public string ItemObject { get; set; }

        [Required]
        [StringLength(60)]
        public string GlobalID { get; set; }

        public virtual StorageContainer StorageContainer { get; set; }
    }
}
