using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable.QuestSystem
{
    public class OnUsed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            QuestService.OnQuestPlaceableUsed(NWGameObject.OBJECT_SELF);
            return true;
        }
    }
}
