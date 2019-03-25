using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[SkillXPRequirement]")]
    public class SkillXPRequirement: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int SkillID { get; set; }
        public int Rank { get; set; }
        public int XP { get; set; }
    }
}
