using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting
{
    public class OnQuestUnloaded
    {
        public AbstractQuest Quest { get; set; }

        public OnQuestUnloaded(AbstractQuest quest)
        {
            Quest = quest;
        }
    }
}
