using SWLOR.Game.Server.Quest.Contracts;

namespace SWLOR.Game.Server.Event.SWLOR
{
    public class OnQuestRegistered
    {
        public IQuest Quest { get; set; }

        public OnQuestRegistered(IQuest quest)
        {
            Quest = quest;
        }
    }
}
