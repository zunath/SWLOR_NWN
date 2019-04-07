using NWN;
using SWLOR.Game.Server.Event;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using Object = NWN.Object;

namespace SWLOR.Game.Server.Placeable.PazaakTable
{
    class OnUsed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            NWPlayer player = (_.GetLastUsedBy());
            NWPlaceable device = (Object.OBJECT_SELF);

            DialogService.StartConversation(player, device, "PazaakTable");
            return true;
        }
    }
}
