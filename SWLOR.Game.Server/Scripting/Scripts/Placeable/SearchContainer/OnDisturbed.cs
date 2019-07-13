using NWN;
using SWLOR.Game.Server.Scripting.Contracts;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Scripting.Scripts.Placeable.SearchContainer
{
    public class OnDisturbed: IScript
    {
        public void SubscribeEvents()
        {
        }

        public void UnsubscribeEvents()
        {
        }

        public void Main()
        {
            SearchService.OnChestDisturbed((NWGameObject.OBJECT_SELF));
        }
    }
}
