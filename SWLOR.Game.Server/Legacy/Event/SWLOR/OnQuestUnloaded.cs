using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
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
