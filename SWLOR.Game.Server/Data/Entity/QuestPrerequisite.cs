

using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("[QuestPrerequisites]")]
    public class QuestPrerequisite: IEntity
    {
        [Key]
        public int QuestPrerequisiteID { get; set; }
        public int QuestID { get; set; }
        public int RequiredQuestID { get; set; }
    }
}
