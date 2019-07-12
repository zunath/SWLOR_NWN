using SWLOR.Game.Server.Event;
using NWN;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Placeable.SearchContainer
{
    public class OnClosed: IRegisteredEvent
    {
        public bool Run(params object[] args)
        {
            SearchService.OnChestClose((NWGameObject.OBJECT_SELF));
            return true;
        }
    }
}
