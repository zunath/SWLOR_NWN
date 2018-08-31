using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class GameTopic
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int GameTopicID { get; set; }

        [Required]
        [StringLength(32)]
        public string Name { get; set; }

        [Required]
        public string Text { get; set; }

        public int GameTopicCategoryID { get; set; }

        public bool IsActive { get; set; }

        public int Sequence { get; set; }

        [Required]
        [StringLength(32)]
        public string Icon { get; set; }

        public virtual GameTopicCategory GameTopicCategory { get; set; }
    }
}
