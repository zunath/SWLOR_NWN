using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Trigger
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
