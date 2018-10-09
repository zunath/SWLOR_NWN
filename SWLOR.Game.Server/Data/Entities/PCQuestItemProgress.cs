using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWLOR.Game.Server.Data.Entities
{
    [Table("PCQuestItemProgress")]
    public partial class PCQuestItemProgress
    {
        public int PCQuestItemProgressID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int PCQuestStatusID { get; set; }

        public string Resref { get; set; }

        public int Remaining { get; set; }

        public bool MustBeCraftedByPlayer { get; set; }

        public virtual PCQuestStatus PCQuestStatus { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }
    }
}
