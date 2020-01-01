using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    public class PerkLevelQuestRequirement: IEntity
    {
        [Key]
        public int ID { get; set; }
        public int PerkLevelID { get; set; }
        public int RequiredQuestID { get; set; }
    }
}
