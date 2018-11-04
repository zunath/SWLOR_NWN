
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[PerkLevelQuestRequirements]")]
    public class PerkLevelQuestRequirement: IEntity
    {
        [Key]
        public int PerkLevelQuestRequirementID { get; set; }
        public int PerkLevelID { get; set; }
        public int RequiredQuestID { get; set; }
    }
}
