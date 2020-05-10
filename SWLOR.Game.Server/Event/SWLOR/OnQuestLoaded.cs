using SWLOR.Game.Server.Quest;

namespace SWLOR.Game.Server.Scripting
{
    public class OnQuestLoaded
    {
        public AbstractQuest Quest { get; set; }

        public OnQuestLoaded(AbstractQuest quest)
        {
            Quest = quest;
        }
    }
}
