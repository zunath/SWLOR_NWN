using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Trigger
{
    public class QuestTrigger: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            QuestService.OnQuestTriggerEntered(NWScript.OBJECT_SELF);
        }
    }
}
