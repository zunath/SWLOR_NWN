using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Trigger
{
    public class QuestTrigger: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            QuestService.OnQuestTriggerEntered(Object.OBJECT_SELF);

            return true;
        }
    }
}
