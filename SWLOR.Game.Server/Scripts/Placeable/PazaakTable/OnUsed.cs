using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.GameObject;
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
            NWPlayer player = NWScript.GetLastUsedBy();
            NWPlaceable device = NWScript.OBJECT_SELF;

            DialogService.StartConversation(player, device, "PazaakTable");
        }
    }
}
