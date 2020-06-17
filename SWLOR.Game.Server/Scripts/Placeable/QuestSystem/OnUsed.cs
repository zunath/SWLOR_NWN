using SWLOR.Game.Server.NWN;
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
            QuestService.OnQuestPlaceableUsed(_.OBJECT_SELF);
        }
    }
}
