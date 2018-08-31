namespace SWLOR.Game.Server.Data.Entities
{
    public partial class QuestPrerequisite
    {
        public int QuestPrerequisiteID { get; set; }

        public int QuestID { get; set; }

        public int RequiredQuestID { get; set; }

        public virtual Quest Quest { get; set; }

        public virtual Quest RequiredQuest { get; set; }
    }
}
