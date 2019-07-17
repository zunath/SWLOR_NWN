using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PerkLevelSkillRequirement]")]
    public class PerkLevelSkillRequirement: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int PerkLevelID { get; set; }
        public int SkillID { get; set; }
        public int RequiredRank { get; set; }

        public IEntity Clone()
        {
            return new PerkLevelSkillRequirement
            {
                ID = ID,
                PerkLevelID = PerkLevelID,
                SkillID = SkillID,
                RequiredRank = RequiredRank
            };
        }
    }
}
