using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.QuestSystem
{
    public class OnUsed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            QuestService.OnQuestPlaceableUsed(NWScript.OBJECT_SELF);
        }
    }
}
