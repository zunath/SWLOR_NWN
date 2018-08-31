using System.ComponentModel.DataAnnotations;

namespace SWLOR.Game.Server.Data.Entities
{
    public partial class PCSkill
    {
        public int PCSkillID { get; set; }

        [Required]
        [StringLength(60)]
        public string PlayerID { get; set; }

        public int SkillID { get; set; }

        public int XP { get; set; }

        public int Rank { get; set; }

        public bool IsLocked { get; set; }

        public virtual PlayerCharacter PlayerCharacter { get; set; }

        public virtual Skill Skill { get; set; }
    }
}
