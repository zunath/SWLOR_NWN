using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Trigger
{
    public class QuestTrigger: IRegisteredEvent
    {
        private readonly IQuestService _quest;

        public QuestTrigger(IQuestService quest)
        {
            _quest = quest;
        }

        public bool Run(params object[] args)
        {
            _quest.OnQuestTriggerEntered(Object.OBJECT_SELF);

            return true;
        }
    }
}
