using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripts.Placeable.PazaakTable
{
    class OnUsed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            NWPlayer player = _.GetLastUsedBy();
            NWPlaceable device = _.OBJECT_SELF;

            DialogService.StartConversation(player, device, "PazaakTable");
        }
    }
}
