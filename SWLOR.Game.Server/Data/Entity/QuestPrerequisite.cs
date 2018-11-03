
using Dapper.Contrib.Extensions;
using SWLOR.Game.Server.Data.Contracts;

namespace SWLOR.Game.Server.Data.Entity
{
    [Table("QuestPrerequisites")]
    public partial class QuestPrerequisite: IEntity
    {
        [Key]
        public int QuestPrerequisiteID { get; set; }
        public int QuestID { get; set; }
        public int RequiredQuestID { get; set; }
    
        public virtual Quest Quest { get; set; }
        public virtual Quest RequiredQuest { get; set; }
    }
}
