using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Legacy.Service;

namespace SWLOR.Game.Server.Legacy.Scripts.Placeable.QuestSystem
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
