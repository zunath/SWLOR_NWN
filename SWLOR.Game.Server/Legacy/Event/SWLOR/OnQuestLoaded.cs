using SWLOR.Game.Server.Legacy.Quest;

namespace SWLOR.Game.Server.Legacy.Event.SWLOR
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
