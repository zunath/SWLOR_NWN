using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class Download
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DownloadID { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(1000)]
        public string Description { get; set; }

        [Required]
        [StringLength(200)]
        public string Url { get; set; }

        public bool IsActive { get; set; }
    }
}
